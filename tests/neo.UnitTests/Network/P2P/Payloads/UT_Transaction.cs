using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Transaction
    {
        Transaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            uut = new Transaction();
        }

        [TestMethod]
        public void Script_Get()
        {
            uut.Script.Should().BeNull();
        }

        [TestMethod]
        public void FromStackItem()
        {
            Assert.ThrowsException<NotSupportedException>(() => ((IInteroperable)uut).FromStackItem(VM.Types.StackItem.Null));
        }

        [TestMethod]
        public void TestEquals()
        {
            Assert.IsTrue(uut.Equals(uut));
            Assert.IsFalse(uut.Equals(null));
        }

        [TestMethod]
        public void InventoryType_Get()
        {
            ((IInventory)uut).InventoryType.Should().Be(InventoryType.TX);
        }

        [TestMethod]
        public void Script_Set()
        {
            byte[] val = TestUtils.GetByteArray(32, 0x42);
            uut.Script = val;
            uut.Script.Length.Should().Be(32);
            for (int i = 0; i < val.Length; i++)
            {
                uut.Script[i].Should().Be(val[i]);
            }
        }

        [TestMethod]
        public void Gas_Get()
        {
            uut.SystemFee.Should().Be(0);
        }

        [TestMethod]
        public void Gas_Set()
        {
            long val = 4200000000;
            uut.SystemFee = val;
            uut.SystemFee.Should().Be(val);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            uut.Signers = Array.Empty<Signer>();
            uut.Attributes = Array.Empty<TransactionAttribute>();
            uut.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                }
            };

            uut.Version.Should().Be(0);
            uut.Script.Length.Should().Be(32);
            uut.Script.GetVarSize().Should().Be(33);
            uut.Size.Should().Be(63);
        }

        [TestMethod]
        public void FeeIsMultiSigContract()
        {
            var walletA = TestUtils.GenerateTestWallet();
            var walletB = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            using (var unlockA = walletA.Unlock("123"))
            using (var unlockB = walletB.Unlock("123"))
            {
                var a = walletA.CreateAccount();
                var b = walletB.CreateAccount();

                var multiSignContract = Contract.CreateMultiSigContract(2,
                    new ECPoint[]
                    {
                        a.GetKey().PublicKey,
                        b.GetKey().PublicKey
                    });

                walletA.CreateAccount(multiSignContract, a.GetKey());
                var acc = walletB.CreateAccount(multiSignContract, b.GetKey());

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);
                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction

                var tx = walletA.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(BigInteger.One,8)
                    }
                }, acc.ScriptHash);

                Assert.IsNotNull(tx);

                // Sign

                var data = new ContractParametersContext(tx);
                Assert.IsTrue(walletA.Sign(data));
                Assert.IsTrue(walletB.Sign(data));
                Assert.IsTrue(data.Completed);

                tx.Witnesses = data.GetWitnesses();

                // Fast check

                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check

                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, tx.NetworkFee))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }

                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
                Assert.AreEqual(1967100, verificationGas);
                Assert.AreEqual(348000, sizeGas);
                Assert.AreEqual(2315100, tx.NetworkFee);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContractDetailed()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            using (var unlock = wallet.Unlock("123"))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction

                // self-transfer of 1e-8 GAS
                var tx = wallet.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(BigInteger.One,8)
                    }
                }, acc.ScriptHash);

                Assert.IsNotNull(tx);
                Assert.IsNull(tx.Witnesses);

                // check pre-computed network fee (already guessing signature sizes)
                tx.NetworkFee.Should().Be(1228520L);

                // ----
                // Sign
                // ----

                var data = new ContractParametersContext(tx);
                // 'from' is always required as witness
                // if not included on cosigner with a scope, its scope should be considered 'CalledByEntry'
                data.ScriptHashes.Count.Should().Be(1);
                data.ScriptHashes[0].Should().BeEquivalentTo(acc.ScriptHash);
                // will sign tx
                bool signed = wallet.Sign(data);
                Assert.IsTrue(signed);
                // get witnesses from signed 'data'
                tx.Witnesses = data.GetWitnesses();
                tx.Witnesses.Length.Should().Be(1);

                // Fast check

                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check

                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, tx.NetworkFee))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }

                // ------------------
                // check tx_size cost
                // ------------------
                Assert.AreEqual(245, tx.Size);

                // will verify tx size, step by step

                // Part I
                Assert.AreEqual(25, Transaction.HeaderSize);
                // Part II
                Assert.AreEqual(1, tx.Attributes.GetVarSize());
                Assert.AreEqual(0, tx.Attributes.Length);
                Assert.AreEqual(1, tx.Signers.Length);
                // Note that Data size and Usage size are different (because of first byte on GetVarSize())
                Assert.AreEqual(22, tx.Signers.GetVarSize());
                // Part III
                Assert.AreEqual(88, tx.Script.GetVarSize());
                // Part IV
                Assert.AreEqual(109, tx.Witnesses.GetVarSize());
                // I + II + III + IV
                Assert.AreEqual(25 + 22 + 1 + 88 + 109, tx.Size);

                Assert.AreEqual(1000, NativeContract.Policy.GetFeePerByte(snapshot));
                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);

                // final check: verification_cost and tx_size
                Assert.AreEqual(245000, sizeGas);
                Assert.AreEqual(983520, verificationGas);

                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_Global()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    BigInteger value = (new BigDecimal(BigInteger.One, 8)).Value;
                    sb.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value, null);
                    sb.Emit(OpCode.ASSERT);
                    script = sb.ToArray();
                }

                // trying global scope
                var signers = new Signer[]{ new Signer
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.Global
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, signers);

                Assert.IsNotNull(tx);
                Assert.IsNull(tx.Witnesses);

                // ----
                // Sign
                // ----

                var data = new ContractParametersContext(tx);
                bool signed = wallet.Sign(data);
                Assert.IsTrue(signed);

                // get witnesses from signed 'data'
                tx.Witnesses = data.GetWitnesses();
                tx.Witnesses.Length.Should().Be(1);

                // Fast check
                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check
                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, tx.NetworkFee))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }
                // get sizeGas
                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
                // final check on sum: verification_cost + tx_size
                Assert.AreEqual(1228520, verificationGas + sizeGas);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CurrentHash_GAS()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    BigInteger value = (new BigDecimal(BigInteger.One, 8)).Value;
                    sb.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value, null);
                    sb.Emit(OpCode.ASSERT);
                    script = sb.ToArray();
                }

                // trying global scope
                var signers = new Signer[]{ new Signer
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.CustomContracts,
                    AllowedContracts = new[] { NativeContract.GAS.Hash }
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, signers);

                Assert.IsNotNull(tx);
                Assert.IsNull(tx.Witnesses);

                // ----
                // Sign
                // ----

                var data = new ContractParametersContext(tx);
                bool signed = wallet.Sign(data);
                Assert.IsTrue(signed);

                // get witnesses from signed 'data'
                tx.Witnesses = data.GetWitnesses();
                tx.Witnesses.Length.Should().Be(1);

                // Fast check
                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check
                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, tx.NetworkFee))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }
                // get sizeGas
                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
                // final check on sum: verification_cost + tx_size
                Assert.AreEqual(1249520, verificationGas + sizeGas);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CalledByEntry_Plus_GAS()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(BigInteger.One, 8)).Value;
                    sb.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value, null);
                    sb.Emit(OpCode.ASSERT);
                    script = sb.ToArray();
                }

                // trying CalledByEntry together with GAS
                var signers = new Signer[]{ new Signer
                {
                    Account = acc.ScriptHash,
                    // This combination is supposed to actually be an OR,
                    // where it's valid in both Entry and also for Custom hash provided (in any execution level)
                    // it would be better to test this in the future including situations where a deeper call level uses this custom witness successfully
                    Scopes = WitnessScope.CustomContracts | WitnessScope.CalledByEntry,
                    AllowedContracts = new[] { NativeContract.GAS.Hash }
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, signers);

                Assert.IsNotNull(tx);
                Assert.IsNull(tx.Witnesses);

                // ----
                // Sign
                // ----

                var data = new ContractParametersContext(tx);
                bool signed = wallet.Sign(data);
                Assert.IsTrue(signed);

                // get witnesses from signed 'data'
                tx.Witnesses = data.GetWitnesses();
                tx.Witnesses.Length.Should().Be(1);

                // Fast check
                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check
                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, tx.NetworkFee))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }
                // get sizeGas
                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
                // final check on sum: verification_cost + tx_size
                Assert.AreEqual(1249520, verificationGas + sizeGas);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CurrentHash_NEO_FAULT()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(BigInteger.One, 8)).Value;
                    sb.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.ASSERT);
                    script = sb.ToArray();
                }

                // trying global scope
                var signers = new Signer[]{ new Signer
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.CustomContracts,
                    AllowedContracts = new[] { NativeContract.NEO.Hash }
                } };

                // using this...

                // expects FAULT on execution of 'transfer' Application script
                // due to lack of a valid witness validation
                Transaction tx = null;
                Assert.ThrowsException<InvalidOperationException>(() => tx = wallet.MakeTransaction(script, acc.ScriptHash, signers));
                Assert.IsNull(tx);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CurrentHash_NEO_GAS()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    BigInteger value = (new BigDecimal(BigInteger.One, 8)).Value;
                    sb.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value, null);
                    sb.Emit(OpCode.ASSERT);
                    script = sb.ToArray();
                }

                // trying two custom hashes, for same target account
                var signers = new Signer[]{ new Signer
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.CustomContracts,
                    AllowedContracts = new[] { NativeContract.NEO.Hash, NativeContract.GAS.Hash }
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, signers);

                Assert.IsNotNull(tx);
                Assert.IsNull(tx.Witnesses);

                // ----
                // Sign
                // ----

                var data = new ContractParametersContext(tx);
                bool signed = wallet.Sign(data);
                Assert.IsTrue(signed);

                // get witnesses from signed 'data'
                tx.Witnesses = data.GetWitnesses();
                // only a single witness should exist
                tx.Witnesses.Length.Should().Be(1);
                // no attributes must exist
                tx.Attributes.Length.Should().Be(0);
                // one cosigner must exist
                tx.Signers.Length.Should().Be(1);

                // Fast check
                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check
                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, tx.NetworkFee))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }
                // get sizeGas
                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
                // final check on sum: verification_cost + tx_size
                Assert.AreEqual(1269520, verificationGas + sizeGas);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_NoScopeFAULT()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    BigInteger value = (new BigDecimal(BigInteger.One, 8)).Value;
                    sb.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.ASSERT);
                    script = sb.ToArray();
                }

                // trying with no scope
                var attributes = new TransactionAttribute[] { };

                var signers = new Signer[]{ new Signer
                {
                    Account = acc.ScriptHash,
                    Scopes = (WitnessScope) 0xFF,
                    AllowedContracts = new[] { NativeContract.NEO.Hash, NativeContract.GAS.Hash }
                } };

                // using this...

                // expects FAULT on execution of 'transfer' Application script
                // due to lack of a valid witness validation
                Transaction tx = null;
                Assert.ThrowsException<InvalidOperationException>(() => tx = wallet.MakeTransaction(script, acc.ScriptHash, signers, attributes));
                Assert.IsNull(tx);
            }
        }

        [TestMethod]
        public void Transaction_Reverify_Hashes_Length_Unequal_To_Witnesses_Length()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            Transaction txSimple = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = new[]{
                    new Signer
                    {
                        Account = UInt160.Parse("0x0001020304050607080900010203040506070809"),
                        Scopes = WitnessScope.Global
                    }
                },
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[0] { }
            };
            UInt160[] hashes = txSimple.GetScriptHashesForVerifying(snapshot);
            Assert.AreEqual(1, hashes.Length);
            Assert.AreNotEqual(VerifyResult.Succeed, txSimple.VerifyStateDependent(snapshot, new TransactionVerificationContext()));
        }

        [TestMethod]
        public void Transaction_Serialize_Deserialize_Simple()
        {
            // good and simple transaction
            Transaction txSimple = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[] { new Witness() { InvocationScript = new byte[0], VerificationScript = Array.Empty<byte>() } }
            };

            byte[] sTx = txSimple.ToArray();

            // detailed hexstring info (basic checking)
            sTx.ToHexString().Should().Be(
                "00" + // version
                "04030201" + // nonce
                "00e1f50500000000" + // system fee (1 GAS)
                "0100000000000000" + // network fee (1 satoshi)
                "04030201" + // timelimit 
                "01000000000000000000000000000000000000000000" + // empty signer
                "00" + // no attributes
                "0111" + // push1 script
                "010000"); // empty witnesses

            // try to deserialize
            Transaction tx2 = Neo.IO.Helper.AsSerializable<Transaction>(sTx);

            tx2.Version.Should().Be(0x00);
            tx2.Nonce.Should().Be(0x01020304);
            tx2.Sender.Should().Be(UInt160.Zero);
            tx2.SystemFee.Should().Be(0x0000000005f5e100); // 1 GAS (long)BigInteger.Pow(10, 8)
            tx2.NetworkFee.Should().Be(0x0000000000000001);
            tx2.ValidUntilBlock.Should().Be(0x01020304);
            tx2.Attributes.Should().BeEquivalentTo(new TransactionAttribute[0] { });
            tx2.Signers.Should().BeEquivalentTo(new Signer[] {
                new Signer()
                {
                    Account = UInt160.Zero,
                    AllowedContracts = Array.Empty<UInt160>(),
                    AllowedGroups = Array.Empty<ECPoint>() }
                }
            );
            tx2.Script.Should().BeEquivalentTo(new byte[] { (byte)OpCode.PUSH1 });
            tx2.Witnesses.Should().BeEquivalentTo(new Witness[] { new Witness() { InvocationScript = new byte[0], VerificationScript = Array.Empty<byte>() } });
        }

        [TestMethod]
        public void Transaction_Serialize_Deserialize_DistinctCosigners()
        {
            // cosigners must be distinct (regarding account)

            Transaction txDoubleCosigners = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = new Signer[]
                {
                    new Signer()
                    {
                        Account = UInt160.Parse("0x0001020304050607080900010203040506070809"),
                        Scopes = WitnessScope.Global
                    },
                    new Signer()
                    {
                        Account = UInt160.Parse("0x0001020304050607080900010203040506070809"), // same account as above
                        Scopes = WitnessScope.CalledByEntry // different scope, but still, same account (cannot do that)
                    }
                },
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[] { new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() } }
            };

            byte[] sTx = txDoubleCosigners.ToArray();

            // no need for detailed hexstring here (see basic tests for it)
            sTx.ToHexString().Should().Be("000403020100e1f5050000000001000000000000000403020102090807060504030201000908070605040302010080090807060504030201000908070605040302010001000111010000");

            // back to transaction (should fail, due to non-distinct cosigners)
            Transaction tx2 = null;
            Assert.ThrowsException<FormatException>(() =>
                tx2 = Neo.IO.Helper.AsSerializable<Transaction>(sTx)
            );
            Assert.IsNull(tx2);
        }


        [TestMethod]
        public void Transaction_Serialize_Deserialize_MaxSizeCosigners()
        {
            // cosigners must respect count

            int maxCosigners = 16;

            // --------------------------------------
            // this should pass (respecting max size)

            var cosigners1 = new Signer[maxCosigners];
            for (int i = 0; i < cosigners1.Length; i++)
            {
                string hex = i.ToString("X4");
                while (hex.Length < 40)
                    hex = hex.Insert(0, "0");
                cosigners1[i] = new Signer
                {
                    Account = UInt160.Parse(hex),
                    Scopes = WitnessScope.CalledByEntry
                };
            }

            Transaction txCosigners1 = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = cosigners1, // max + 1 (should fail)
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[] { new Witness() { InvocationScript = new byte[0], VerificationScript = Array.Empty<byte>() } }
            };

            byte[] sTx1 = txCosigners1.ToArray();

            // back to transaction (should fail, due to non-distinct cosigners)
            Assert.ThrowsException<FormatException>(() => Neo.IO.Helper.AsSerializable<Transaction>(sTx1));

            // ----------------------------
            // this should fail (max + 1)

            var cosigners = new Signer[maxCosigners + 1];
            for (var i = 0; i < maxCosigners + 1; i++)
            {
                string hex = i.ToString("X4");
                while (hex.Length < 40)
                    hex = hex.Insert(0, "0");
                cosigners[i] = new Signer
                {
                    Account = UInt160.Parse(hex)
                };
            }

            Transaction txCosigners = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = cosigners, // max + 1 (should fail)
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[] { new Witness() { InvocationScript = new byte[0], VerificationScript = Array.Empty<byte>() } }
            };

            byte[] sTx2 = txCosigners.ToArray();

            // back to transaction (should fail, due to non-distinct cosigners)
            Transaction tx2 = null;
            Assert.ThrowsException<FormatException>(() =>
                tx2 = Neo.IO.Helper.AsSerializable<Transaction>(sTx2)
            );
            Assert.IsNull(tx2);
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_FeeOnly_Default()
        {
            // Global is supposed to be default

            Signer cosigner = new Signer();
            cosigner.Scopes.Should().Be(WitnessScope.None);

            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    BigInteger value = (new BigDecimal(BigInteger.One, 8)).Value;
                    sb.EmitDynamicCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value, null);
                    sb.Emit(OpCode.ASSERT);
                    script = sb.ToArray();
                }

                // try to use fee only inside the smart contract
                var signers = new Signer[]{ new Signer
                {
                    Account = acc.ScriptHash,
                    Scopes =  WitnessScope.None
                } };

                Assert.ThrowsException<InvalidOperationException>(() => wallet.MakeTransaction(script, acc.ScriptHash, signers));

                // change to global scope
                signers[0].Scopes = WitnessScope.Global;

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, signers);

                Assert.IsNotNull(tx);
                Assert.IsNull(tx.Witnesses);

                // ----
                // Sign
                // ----

                var data = new ContractParametersContext(tx);
                bool signed = wallet.Sign(data);
                Assert.IsTrue(signed);

                // get witnesses from signed 'data'
                tx.Witnesses = data.GetWitnesses();
                tx.Witnesses.Length.Should().Be(1);

                // Fast check
                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check
                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, tx.NetworkFee))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }
                // get sizeGas
                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
                // final check on sum: verification_cost + tx_size
                Assert.AreEqual(1228520, verificationGas + sizeGas);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void ToJson()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            uut.SystemFee = 4200000000;
            uut.Signers = new Signer[] { new Signer() { Account = UInt160.Zero } };
            uut.Attributes = Array.Empty<TransactionAttribute>();
            uut.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                }
            };

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0xe17382d26702bde77b00a9f23ea156b77c418764cbc45b2692088b5fde0336e3");
            jObj["size"].AsNumber().Should().Be(84);
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            jObj["netfee"].AsString().Should().Be("0");
            jObj["script"].AsString().Should().Be("QiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA=");
            jObj["sysfee"].AsString().Should().Be("4200000000");
        }

        [TestMethod]
        public void Test_GetAttribute()
        {
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 0,
                Nonce = (uint)Environment.TickCount,
                Script = new byte[Transaction.MaxTransactionSize],
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
                Witnesses = new Witness[0],
            };

            Assert.IsNull(tx.GetAttribute<OracleResponse>());
            Assert.IsNull(tx.GetAttribute<HighPriorityAttribute>());

            tx.Attributes = new TransactionAttribute[] { new HighPriorityAttribute() };

            Assert.IsNull(tx.GetAttribute<OracleResponse>());
            Assert.IsNotNull(tx.GetAttribute<HighPriorityAttribute>());
        }

        [TestMethod]
        public void Test_VerifyStateIndependent()
        {
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 0,
                Nonce = (uint)Environment.TickCount,
                Script = new byte[Transaction.MaxTransactionSize],
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
                Witnesses = new[]
                {
                    new Witness
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = Array.Empty<byte>()
                    }
                }
            };
            tx.VerifyStateIndependent().Should().Be(VerifyResult.Invalid);
            tx.Script = new byte[0];
            tx.VerifyStateIndependent().Should().Be(VerifyResult.Succeed);

            var walletA = TestUtils.GenerateTestWallet();
            var walletB = TestUtils.GenerateTestWallet();
            var snapshot = Blockchain.Singleton.GetSnapshot();

            using (var unlockA = walletA.Unlock("123"))
            using (var unlockB = walletB.Unlock("123"))
            {
                var a = walletA.CreateAccount();
                var b = walletB.CreateAccount();

                var multiSignContract = Contract.CreateMultiSigContract(2,
                    new ECPoint[]
                    {
                        a.GetKey().PublicKey,
                        b.GetKey().PublicKey
                    });

                walletA.CreateAccount(multiSignContract, a.GetKey());
                var acc = walletB.CreateAccount(multiSignContract, b.GetKey());

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);
                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction

                tx = walletA.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(BigInteger.One,8)
                    }
                }, acc.ScriptHash);

                // Sign

                var data = new ContractParametersContext(tx);
                Assert.IsTrue(walletA.Sign(data));
                Assert.IsTrue(walletB.Sign(data));
                Assert.IsTrue(data.Completed);

                tx.Witnesses = data.GetWitnesses();
                tx.VerifyStateIndependent().Should().Be(VerifyResult.Succeed);
            }
        }

        [TestMethod]
        public void Test_VerifyStateDependent()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var height = NativeContract.Ledger.CurrentIndex(snapshot);
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 55000,
                Nonce = (uint)Environment.TickCount,
                Script = Array.Empty<byte>(),
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                SystemFee = 0,
                ValidUntilBlock = height + 1,
                Version = 0,
                Witnesses = new Witness[] {
                    new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = new byte[0] },
                    new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = new byte[1] }
                }
            };

            // Fake balance

            var key = NativeContract.GAS.CreateStorageKey(20, tx.Sender);
            var balance = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            balance.GetInteroperable<AccountState>().Balance = tx.NetworkFee;

            tx.VerifyStateDependent(snapshot, new TransactionVerificationContext()).Should().Be(VerifyResult.Invalid);
            balance.GetInteroperable<AccountState>().Balance = 0;
            tx.SystemFee = 10;
            tx.VerifyStateDependent(snapshot, new TransactionVerificationContext()).Should().Be(VerifyResult.InsufficientFunds);

            var walletA = TestUtils.GenerateTestWallet();
            var walletB = TestUtils.GenerateTestWallet();

            using (var unlockA = walletA.Unlock("123"))
            using (var unlockB = walletB.Unlock("123"))
            {
                var a = walletA.CreateAccount();
                var b = walletB.CreateAccount();

                var multiSignContract = Contract.CreateMultiSigContract(2,
                    new ECPoint[]
                    {
                        a.GetKey().PublicKey,
                        b.GetKey().PublicKey
                    });

                walletA.CreateAccount(multiSignContract, a.GetKey());
                var acc = walletB.CreateAccount(multiSignContract, b.GetKey());

                // Fake balance

                snapshot = Blockchain.Singleton.GetSnapshot();
                key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);
                balance = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
                balance.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                // Make transaction

                snapshot.Commit();
                tx = walletA.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(BigInteger.One,8)
                    }
                }, acc.ScriptHash);

                // Sign

                var data = new ContractParametersContext(tx);
                Assert.IsTrue(walletA.Sign(data));
                Assert.IsTrue(walletB.Sign(data));
                Assert.IsTrue(data.Completed);

                tx.Witnesses = data.GetWitnesses();
                tx.VerifyStateDependent(snapshot, new TransactionVerificationContext()).Should().Be(VerifyResult.Succeed);
            }
        }

        [TestMethod]
        public void Test_Verify()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                NetworkFee = 0,
                Nonce = (uint)Environment.TickCount,
                Script = new byte[Transaction.MaxTransactionSize],
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
                Witnesses = new Witness[0],
            };
            tx.Verify(snapshot, new TransactionVerificationContext()).Should().Be(VerifyResult.Invalid);

            var walletA = TestUtils.GenerateTestWallet();
            var walletB = TestUtils.GenerateTestWallet();

            using (var unlockA = walletA.Unlock("123"))
            using (var unlockB = walletB.Unlock("123"))
            {
                var a = walletA.CreateAccount();
                var b = walletB.CreateAccount();

                var multiSignContract = Contract.CreateMultiSigContract(2,
                    new ECPoint[]
                    {
                        a.GetKey().PublicKey,
                        b.GetKey().PublicKey
                    });

                walletA.CreateAccount(multiSignContract, a.GetKey());
                var acc = walletB.CreateAccount(multiSignContract, b.GetKey());

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);
                var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));

                entry.GetInteroperable<AccountState>().Balance = 10000 * NativeContract.GAS.Factor;

                snapshot.Commit();

                // Make transaction

                tx = walletA.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(BigInteger.One,8)
                    }
                }, acc.ScriptHash);

                // Sign

                var data = new ContractParametersContext(tx);
                Assert.IsTrue(walletA.Sign(data));
                Assert.IsTrue(walletB.Sign(data));
                Assert.IsTrue(data.Completed);

                tx.Witnesses = data.GetWitnesses();
                tx.Verify(snapshot, new TransactionVerificationContext()).Should().Be(VerifyResult.Succeed);
            }
        }
    }
}
