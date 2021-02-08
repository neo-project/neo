using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_StdLib
    {
        [TestInitialize]
        public void Init()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestBinary()
        {
            var data = System.Array.Empty<byte>();

            CollectionAssert.AreEqual(data, StdLib.Base64Decode(StdLib.Base64Encode(data)));
            CollectionAssert.AreEqual(data, StdLib.Base58Decode(StdLib.Base58Encode(data)));

            data = new byte[] { 1, 2, 3 };

            CollectionAssert.AreEqual(data, StdLib.Base64Decode(StdLib.Base64Encode(data)));
            CollectionAssert.AreEqual(data, StdLib.Base58Decode(StdLib.Base58Encode(data)));
            Assert.AreEqual("AQIDBA==", StdLib.Base64Encode(new byte[] { 1, 2, 3, 4 }));
            Assert.AreEqual("2VfUX", StdLib.Base58Encode(new byte[] { 1, 2, 3, 4 }));
        }

        [TestMethod]
        public void TestItoaAtoi()
        {
            Assert.AreEqual("1", StdLib.Itoa(BigInteger.One, 10));
            Assert.AreEqual("1", StdLib.Itoa(BigInteger.One, 16));
            Assert.AreEqual("-1", StdLib.Itoa(BigInteger.MinusOne, 10));
            Assert.AreEqual("f", StdLib.Itoa(BigInteger.MinusOne, 16));
            Assert.AreEqual(-1, StdLib.Atoi("-1", 10));
            Assert.AreEqual(1, StdLib.Atoi("+1", 10));
            Assert.AreEqual(-1, StdLib.Atoi("ff", 16));
            Assert.ThrowsException<System.FormatException>(() => StdLib.Atoi("a", 10));
            Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => StdLib.Atoi("a", 11));
        }

        [TestMethod]
        public void Json_Deserialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "123");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "null");

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.HALT);
                    Assert.AreEqual(2, engine.ResultStack.Count);

                    engine.ResultStack.Pop<Null>();
                    Assert.IsTrue(engine.ResultStack.Pop().GetInteger() == 123);
                }
            }

            // Error 1 - Wrong Json

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "***");

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.FAULT);
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
            }

            // Error 2 - No decimals

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "123.45");

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.FAULT);
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
            }
        }

        [TestMethod]
        public void Json_Serialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", 5);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", "test");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", new object[] { null });
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize", new ContractParameter(ContractParameterType.Map)
                {
                    Value = new List<KeyValuePair<ContractParameter, ContractParameter>>() {
                        { new KeyValuePair<ContractParameter, ContractParameter>(
                            new ContractParameter(ContractParameterType.String){ Value="key" },
                            new ContractParameter(ContractParameterType.String){ Value= "value" })
                        }
                    }
                });

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.HALT);
                    Assert.AreEqual(5, engine.ResultStack.Count);

                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "{\"key\":\"value\"}");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "null");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "\"test\"");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "1");
                    Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "5");
                }
            }

            // Error

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize");

                using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings))
                {
                    engine.LoadScript(script.ToArray());

                    Assert.AreEqual(engine.Execute(), VMState.FAULT);
                    Assert.AreEqual(0, engine.ResultStack.Count);
                }
            }
        }
    }
}
