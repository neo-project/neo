using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using Neo.Wallets.SQLite;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Transaction
    {
        Transaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Transaction();
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
            uut.Gas.Should().Be(0);
        }

        [TestMethod]
        public void Gas_Set()
        {
            long val = 4200000000;
            uut.Gas = val;
            uut.Gas.Should().Be(val);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            uut.Sender = UInt160.Zero;
            uut.Attributes = new TransactionAttribute[0];
            uut.Witnesses = new Witness[]{ new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new byte[0]
            } };

            uut.Version.Should().Be(0);
            uut.Script.Length.Should().Be(32);
            uut.Script.GetVarSize().Should().Be(33);
            uut.Size.Should().Be(82);
        }

        [TestMethod]
        public void FeeIsSignatureContract()
        {
            var store = TestBlockchain.GetStore();
            var wallet = new NEP6Wallet("unit-test.json");
            var snapshot = store.GetSnapshot();

            using (var unlock = wallet.Unlock("123"))
            {
                var acc = wallet.CreateAccount();

                // Fake balance

                var key = NativeContract.GAS.CreateStorageKey(20, acc.ScriptHash);
                snapshot.Storages.GetAndChange(key, () => new StorageItem
                {
                    Value = new Nep5AccountState()
                    {
                        Balance = 10000 * NativeContract.GAS.Factor
                    }
                    .ToByteArray()
                });

                // Make transaction

                var tx = wallet.MakeTransaction(snapshot, new TransferOutput[]
                {
                    new TransferOutput()
                    {
                         AssetId = NativeContract.GAS.Hash,
                         ScriptHash = acc.ScriptHash,
                         Value = new BigDecimal(1,8)
                    }
                }, acc.ScriptHash);

                // Sign

                var data = new ContractParametersContext(tx);
                Assert.IsTrue(wallet.Sign(data));
                tx.Witnesses = data.GetWitnesses();

                // Check

                long verificationGas = 0;
                using (ApplicationEngine engine = new ApplicationEngine(TriggerType.Verification, tx, snapshot, tx.NetworkFee, false))
                {
                    engine.LoadScript(tx.Witnesses[0].VerificationScript);
                    engine.LoadScript(tx.Witnesses[0].InvocationScript);
                    Assert.AreEqual(VMState.HALT, engine.Execute());
                    Assert.AreEqual(1, engine.ResultStack.Count);
                    Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());
                    verificationGas = engine.GasConsumed;
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
            uut.Gas = 4200000000;
            uut.Attributes = new TransactionAttribute[0];
            uut.Witnesses = new Witness[]{ new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new byte[0]
            } };

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("0xee00d595ccd48a650f62adaccbb9c979e2dc7ef66fb5b1413f0f74d563a2d9c6");
            jObj["size"].AsNumber().Should().Be(82);
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            jObj["net_fee"].AsString().Should().Be("0");
            jObj["script"].AsString().Should().Be("4220202020202020202020202020202020202020202020202020202020202020");
            jObj["gas"].AsNumber().Should().Be(42);
        }
    }
}
