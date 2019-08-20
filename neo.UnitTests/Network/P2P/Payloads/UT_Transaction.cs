using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Numerics;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Transaction
    {
        Transaction uut;
        Store store;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Transaction();
            store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void Script_Get()
        {
            uut.Script.Should().BeNull();
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
            uut.Sender = UInt160.Zero;
            uut.Attributes = new TransactionAttribute[0];
            uut.Cosigners = new Cosigner[0];
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
            uut.Size.Should().Be(83);
        }

        [TestMethod]
        public void FeeIsMultiSigContract()
        {
            var store = TestBlockchain.GetStore();
            var walletA = TestUtils.GenerateTestWallet();
            var walletB = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

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
                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction

                var tx = walletA.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(1,8)
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
                    using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
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
                Assert.AreEqual(verificationGas, 2000540);
                Assert.AreEqual(sizeGas, 358000);
                Assert.AreEqual(verificationGas + sizeGas, 2358540);
                Assert.AreEqual(tx.NetworkFee, 2358540);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContractDetailed()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            using (var unlock = wallet.Unlock("123"))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction

                // self-transfer of 1e-8 GAS
                var tx = wallet.MakeTransaction(new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(1,8)
                    }
                }, acc.ScriptHash);

                Assert.IsNotNull(tx);
                Assert.IsNull(tx.Witnesses);

                // check pre-computed network fee (already guessing signature sizes)
                tx.NetworkFee.Should().Be(1257240);

                // ----
                // Sign
                // ----

                var data = new ContractParametersContext(tx);
                // 'from' is always required as witness
                // if not included on cosigner with a scope, its scope should be considered 'CalledByEntry'
                data.ScriptHashes.Count.Should().Be(1);
                data.ScriptHashes[0].ShouldBeEquivalentTo(acc.ScriptHash);
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
                    using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
                    {
                        engine.LoadScript(witness.VerificationScript);
                        engine.LoadScript(witness.InvocationScript);
                        Assert.AreEqual(VMState.HALT, engine.Execute());
                        Assert.AreEqual(1, engine.ResultStack.Count);
                        Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                        verificationGas += engine.GasConsumed;
                    }
                }
                Assert.AreEqual(verificationGas, 1000240);

                // ------------------
                // check tx_size cost
                // ------------------
                Assert.AreEqual(tx.Size, 257);

                // will verify tx size, step by step

                // Part I
                Assert.AreEqual(Transaction.HeaderSize, 45);
                // Part II
                Assert.AreEqual(tx.Attributes.GetVarSize(), 1);
                Assert.AreEqual(tx.Attributes.Length, 0);
                Assert.AreEqual(tx.Cosigners.Length, 1);
                Assert.AreEqual(tx.Cosigners.GetVarSize(), 22);
                // Note that Data size and Usage size are different (because of first byte on GetVarSize())
                Assert.AreEqual(tx.Cosigners[0].Size, 21);
                // Part III
                Assert.AreEqual(tx.Script.GetVarSize(), 82);
                // Part IV
                Assert.AreEqual(tx.Witnesses.GetVarSize(), 107);
                // I + II + III + IV
                Assert.AreEqual(tx.Size, 45 + 23 + 82 + 107);

                Assert.AreEqual(NativeContract.Policy.GetFeePerByte(snapshot), 1000);
                var sizeGas = tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
                Assert.AreEqual(sizeGas, 257000);

                // final check on sum: verification_cost + tx_size
                Assert.AreEqual(verificationGas + sizeGas, 1257240);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_Global()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(1, 8)).Value;
                    sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.THROWIFNOT);
                    script = sb.ToArray();
                }

                // trying global scope
                var cosigners = new Cosigner[]{ new Cosigner
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.Global
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], cosigners);

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
                    using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
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
                Assert.AreEqual(verificationGas + sizeGas, 1257240);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CurrentHash_GAS()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(1, 8)).Value;
                    sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.THROWIFNOT);
                    script = sb.ToArray();
                }

                // trying global scope
                var cosigners = new Cosigner[]{ new Cosigner
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.CustomContracts,
                    AllowedContracts = new[] { NativeContract.GAS.Hash }
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], cosigners);

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
                    using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
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
                Assert.AreEqual(verificationGas + sizeGas, 1278240);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CalledByEntry_Plus_GAS()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(1, 8)).Value;
                    sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.THROWIFNOT);
                    script = sb.ToArray();
                }

                // trying CalledByEntry together with GAS
                var cosigners = new Cosigner[]{ new Cosigner
                {
                    Account = acc.ScriptHash,
                    // This combination is supposed to actually be an OR,
                    // where it's valid in both Entry and also for Custom hash provided (in any execution level)
                    // it would be better to test this in the future including situations where a deeper call level uses this custom witness successfully
                    Scopes = WitnessScope.CustomContracts | WitnessScope.CalledByEntry,
                    AllowedContracts = new[] { NativeContract.GAS.Hash }
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], cosigners);

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
                    using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
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
                Assert.AreEqual(verificationGas + sizeGas, 1278240);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CurrentHash_NEO_FAULT()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(1, 8)).Value;
                    sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.THROWIFNOT);
                    script = sb.ToArray();
                }

                // trying global scope
                var cosigners = new Cosigner[]{ new Cosigner
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.CustomContracts,
                    AllowedContracts = new[] { NativeContract.NEO.Hash }
                } };

                // using this...

                // expects FAULT on execution of 'transfer' Application script
                // due to lack of a valid witness validation
                Transaction tx = null;
                Assert.ThrowsException<InvalidOperationException>(() =>
                    tx = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], cosigners));
                Assert.IsNull(tx);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CurrentHash_NEO_GAS()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(1, 8)).Value;
                    sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.THROWIFNOT);
                    script = sb.ToArray();
                }

                // trying two custom hashes, for same target account
                var cosigners = new Cosigner[]{ new Cosigner
                {
                    Account = acc.ScriptHash,
                    Scopes = WitnessScope.CustomContracts,
                    AllowedContracts = new[] { NativeContract.NEO.Hash, NativeContract.GAS.Hash }
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], cosigners);

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
                tx.Cosigners.Length.Should().Be(1);

                // Fast check
                Assert.IsTrue(tx.VerifyWitnesses(snapshot, tx.NetworkFee));

                // Check
                long verificationGas = 0;
                foreach (var witness in tx.Witnesses)
                {
                    using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
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
                Assert.AreEqual(verificationGas + sizeGas, 1298240);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_NoScopeFAULT()
        {
            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(1, 8)).Value;
                    sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.THROWIFNOT);
                    script = sb.ToArray();
                }

                // trying with no scope
                var attributes = new TransactionAttribute[] { };
                var cosigners = new Cosigner[] { };

                // using this...

                // expects FAULT on execution of 'transfer' Application script
                // due to lack of a valid witness validation
                Transaction tx = null;
                Assert.ThrowsException<InvalidOperationException>(() => tx = wallet.MakeTransaction(script, acc.ScriptHash, attributes, cosigners));
                Assert.IsNull(tx);
            }
        }

        [TestMethod]
        public void Transaction_Serialize_Deserialize_Simple()
        {
            // good and simple transaction
            Transaction txSimple = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                Sender = UInt160.Zero,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = new TransactionAttribute[0] { },
                Cosigners = new Cosigner[0] { },
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[0] { }
            };

            byte[] sTx = txSimple.ToArray();

            // detailed hexstring info (basic checking)
            sTx.ToHexString().Should().Be("00" + // version
            "04030201" + // nonce
            "0000000000000000000000000000000000000000" + // sender
            "00e1f50500000000" + // system fee (1 GAS)
            "0100000000000000" + // network fee (1 satoshi)
            "04030201" + // timelimit 
            "00" + // no attributes
            "00" + // no cosigners
            "0151" + // push1 script
            "00"); // no witnesses

            // try to deserialize
            Transaction tx2 = Neo.IO.Helper.AsSerializable<Transaction>(sTx);

            tx2.Version.Should().Be(0x00);
            tx2.Nonce.Should().Be(0x01020304);
            tx2.Sender.Should().Be(UInt160.Zero);
            tx2.SystemFee.Should().Be(0x0000000005f5e100); // 1 GAS (long)BigInteger.Pow(10, 8)
            tx2.NetworkFee.Should().Be(0x0000000000000001);
            tx2.ValidUntilBlock.Should().Be(0x01020304);
            tx2.Attributes.Should().BeEquivalentTo(new TransactionAttribute[0] { });
            tx2.Cosigners.Should().BeEquivalentTo(new Cosigner[0] { });
            tx2.Script.Should().BeEquivalentTo(new byte[] { (byte)OpCode.PUSH1 });
            tx2.Witnesses.Should().BeEquivalentTo(new Witness[0] { });
        }

        [TestMethod]
        public void Transaction_Serialize_Deserialize_DistinctCosigners()
        {
            // cosigners must be distinct (regarding account)

            Transaction txDoubleCosigners = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                Sender = UInt160.Zero,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = new TransactionAttribute[0] { },
                Cosigners = new Cosigner[] {
                    new Cosigner
                    {
                        Account = UInt160.Parse("0x0001020304050607080900010203040506070809"),
                        Scopes = WitnessScope.Global
                    },
                    new Cosigner
                    {
                        Account = UInt160.Parse("0x0001020304050607080900010203040506070809"), // same account as above
                        Scopes = WitnessScope.CalledByEntry // different scope, but still, same account (cannot do that)
                    }
                },
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[0] { }
            };

            byte[] sTx = txDoubleCosigners.ToArray();

            // no need for detailed hexstring here (see basic tests for it)
            sTx.ToHexString().Should().Be("0004030201000000000000000000000000000000000000000000e1f505000000000100000000000000040302010002090807060504030201000908070605040302010000090807060504030201000908070605040302010001015100");

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

            var cosigners1 = new Cosigner[maxCosigners];
            for (int i = 0; i < cosigners1.Length; i++)
            {
                string hex = i.ToString("X4");
                while (hex.Length < 40)
                    hex = hex.Insert(0, "0");
                cosigners1[i] = new Cosigner
                {
                    Account = UInt160.Parse(hex)
                };
            }

            Transaction txCosigners1 = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                Sender = UInt160.Zero,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = new TransactionAttribute[0] { },
                Cosigners = cosigners1, // max + 1 (should fail)
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[0] { }
            };

            byte[] sTx1 = txCosigners1.ToArray();

            // back to transaction (should fail, due to non-distinct cosigners)
            Transaction tx1 = Neo.IO.Helper.AsSerializable<Transaction>(sTx1);
            Assert.IsNotNull(tx1);

            // ----------------------------
            // this should fail (max + 1)

            var cosigners = new Cosigner[maxCosigners + 1];
            for (var i = 0; i < maxCosigners + 1; i++)
            {
                string hex = i.ToString("X4");
                while (hex.Length < 40)
                    hex = hex.Insert(0, "0");
                cosigners[i] = new Cosigner
                {
                    Account = UInt160.Parse(hex)
                };
            }

            Transaction txCosigners = new Transaction
            {
                Version = 0x00,
                Nonce = 0x01020304,
                Sender = UInt160.Zero,
                SystemFee = (long)BigInteger.Pow(10, 8), // 1 GAS 
                NetworkFee = 0x0000000000000001,
                ValidUntilBlock = 0x01020304,
                Attributes = new TransactionAttribute[0] { },
                Cosigners = cosigners, // max + 1 (should fail)
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Witnesses = new Witness[0] { }
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
        public void FeeIsSignatureContract_TestScope_Global_Default()
        {
            // Global is supposed to be default

            Cosigner cosigner = new Cosigner();
            cosigner.Scopes.Should().Be(WitnessScope.Global);

            var wallet = TestUtils.GenerateTestWallet();
            var snapshot = store.GetSnapshot();

            // no password on this wallet
            using (var unlock = wallet.Unlock(""))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);

                var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState().ToByteArray()
                });

                entry.Value = new Nep5AccountState()
                {
                    Balance = 10000 * NativeContract.GAS.Factor
                }
                .ToByteArray();

                // Make transaction
                // Manually creating script

                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    // self-transfer of 1e-8 GAS
                    System.Numerics.BigInteger value = (new BigDecimal(1, 8)).Value;
                    sb.EmitAppCall(NativeContract.GAS.Hash, "transfer", acc.ScriptHash, acc.ScriptHash, value);
                    sb.Emit(OpCode.THROWIFNOT);
                    script = sb.ToArray();
                }

                // default to global scope
                var cosigners = new Cosigner[]{ new Cosigner
                {
                    Account = acc.ScriptHash
                } };

                // using this...

                var tx = wallet.MakeTransaction(script, acc.ScriptHash, new TransactionAttribute[0], cosigners);

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
                    using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
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
                Assert.AreEqual(verificationGas + sizeGas, 1257240);
                // final assert
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void ToJson()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            uut.Sender = UInt160.Zero;
            uut.SystemFee = 4200000000;
            uut.Attributes = new TransactionAttribute[] { };
            uut.Cosigners = new Cosigner[] { };
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
            jObj["hash"].AsString().Should().Be("0x11e3ee692015f0cd3cb8b6db7a4fc37568540f020cb9ca497a9917c81f20b62f");
            jObj["size"].AsNumber().Should().Be(83);
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            ((JArray)jObj["cosigners"]).Count.Should().Be(0);
            jObj["net_fee"].AsString().Should().Be("0");
            jObj["script"].AsString().Should().Be("4220202020202020202020202020202020202020202020202020202020202020");
            jObj["sys_fee"].AsNumber().Should().Be(42);
        }
    }
}
