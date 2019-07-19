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

namespace Neo.UnitTests
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
            uut.Size.Should().Be(82);
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
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void FeeIsSignatureContract()
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

                // Sign

                var data = new ContractParametersContext(tx);
                Assert.IsTrue(wallet.Sign(data));
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
                Assert.AreEqual(tx.NetworkFee, verificationGas + sizeGas);
            }
        }

        [TestMethod]
        public void ToJson()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            uut.Sender = UInt160.Zero;
            uut.SystemFee = 4200000000;
            uut.Attributes = new TransactionAttribute[0];
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
            jObj["hash"].AsString().Should().Be("0xee00d595ccd48a650f62adaccbb9c979e2dc7ef66fb5b1413f0f74d563a2d9c6");
            jObj["size"].AsNumber().Should().Be(82);
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            jObj["net_fee"].AsString().Should().Be("0");
            jObj["script"].AsString().Should().Be("4220202020202020202020202020202020202020202020202020202020202020");
            jObj["sys_fee"].AsNumber().Should().Be(42);
        }
    }
}
