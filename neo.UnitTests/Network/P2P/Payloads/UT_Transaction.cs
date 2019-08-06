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

        private NEP6Wallet GenerateTestWallet()
        {
            JObject wallet = new JObject();
            wallet["name"] = "noname";
            wallet["version"] = new System.Version().ToString();
            wallet["scrypt"] = new ScryptParameters(0, 0, 0).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = null;
            wallet.ToString().Should().Be("{\"name\":\"noname\",\"version\":\"0.0\",\"scrypt\":{\"n\":0,\"r\":0,\"p\":0},\"accounts\":[],\"extra\":null}");
            return new NEP6Wallet(wallet);
        }

        [TestMethod]
        public void FeeIsMultiSigContract()
        {
            var store = TestBlockchain.GetStore();
            var walletA = GenerateTestWallet();
            var walletB = GenerateTestWallet();
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
            var wallet = GenerateTestWallet();
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
                //tx.Size  =>  HeaderSize +
                //Attributes.GetVarSize() +   //Attributes
                //Script.GetVarSize() +       //Script
                //Witnesses.GetVarSize();     //Witnesses

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
            var wallet = GenerateTestWallet();
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

                var tx = wallet.MakeTransaction(new TransactionAttribute[0], script, cosigners, acc.ScriptHash);

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

                //Assert.IsNotNull(tx.Witnesses);
                //tx.Witnesses = new Witness[0]{};

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
            var wallet = GenerateTestWallet();
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

                var tx = wallet.MakeTransaction(new TransactionAttribute[0], script, cosigners, acc.ScriptHash);

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

                //Assert.IsNotNull(tx.Witnesses);
                //tx.Witnesses = new Witness[0]{};

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
            var wallet = GenerateTestWallet();
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
                    tx = wallet.MakeTransaction(new TransactionAttribute[0], script, cosigners, acc.ScriptHash));
                Assert.IsNull(tx);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract_TestScope_CurrentHash_NEO_GAS()
        {
            var wallet = GenerateTestWallet();
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

                // could use this...
                // public Transaction MakeTransaction(TransferOutput[] outputs, UInt160 from = null)

                // ------------------------
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
                // public Transaction MakeTransaction(TransactionAttribute[] attributes, byte[] script, UInt160 sender = null)

                var tx = wallet.MakeTransaction(new TransactionAttribute[0], script, cosigners, acc.ScriptHash);

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

                //Assert.IsNotNull(tx.Witnesses);
                //tx.Witnesses = new Witness[0]{};

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
            var wallet = GenerateTestWallet();
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

                // could use this...
                // public Transaction MakeTransaction(TransferOutput[] outputs, UInt160 from = null)

                // ------------------------
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
                Assert.ThrowsException<InvalidOperationException>(() => tx = wallet.MakeTransaction(attributes, script, cosigners, acc.ScriptHash));
                Assert.IsNull(tx);
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
