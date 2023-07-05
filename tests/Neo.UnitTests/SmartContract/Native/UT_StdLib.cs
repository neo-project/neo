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
            Assert.AreEqual("3b9aca00", StdLib.Itoa(1_000_000_000, 16));
            Assert.AreEqual(-1, StdLib.Atoi("-1", 10));
            Assert.AreEqual(1, StdLib.Atoi("+1", 10));
            Assert.AreEqual(-1, StdLib.Atoi("ff", 16));
            Assert.AreEqual(-1, StdLib.Atoi("FF", 16));
            Assert.ThrowsException<System.FormatException>(() => StdLib.Atoi("a", 10));
            Assert.ThrowsException<System.FormatException>(() => StdLib.Atoi("g", 16));
            Assert.ThrowsException<System.ArgumentOutOfRangeException>(() => StdLib.Atoi("a", 11));
        }

        [TestMethod]
        public void MemoryCompare()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "c");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "d");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "abc");
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memoryCompare", "abc", "abcd");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(4, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(0, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
            }
        }

        [TestMethod]
        public void CheckDecodeEncode()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckEncode", new byte[] { 1, 2, 3 });

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                Assert.AreEqual("3DUz7ncyT", engine.ResultStack.Pop<ByteString>().GetString());
            }

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckDecode", "3DUz7ncyT");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(1, engine.ResultStack.Count);

                CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, engine.ResultStack.Pop<ByteString>().GetSpan().ToArray());
            }

            // Error

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckDecode", "AA");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
            }

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "base58CheckDecode", null);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
            }
        }

        [TestMethod]
        public void MemorySearch()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 0);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 1);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 2);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 3);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "d", 0);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 0, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 1, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 2, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 3, false);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "d", 0, false);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
            }

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 0, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 1, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 2, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "c", 3, true);
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "memorySearch", "abc", "d", 0, true);

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
                Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
            }
        }

        [TestMethod]
        public void StringSplit()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "stringSplit", "a,b", ",");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(1, engine.ResultStack.Count);

            var arr = engine.ResultStack.Pop<Array>();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual("a", arr[0].GetString());
            Assert.AreEqual("b", arr[1].GetString());
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

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(2, engine.ResultStack.Count);

                engine.ResultStack.Pop<Null>();
                Assert.IsTrue(engine.ResultStack.Pop().GetInteger() == 123);
            }

            // Error 1 - Wrong Json

            using (ScriptBuilder script = new())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "***");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
                Assert.AreEqual(0, engine.ResultStack.Count);
            }

            // Error 2 - No decimals

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonDeserialize", "123.45");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
                Assert.AreEqual(0, engine.ResultStack.Count);
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

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.HALT);
                Assert.AreEqual(5, engine.ResultStack.Count);

                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "{\"key\":\"value\"}");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "null");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "\"test\"");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "true");
                Assert.IsTrue(engine.ResultStack.Pop<ByteString>().GetString() == "5");
            }

            // Error

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(NativeContract.StdLib.Hash, "jsonSerialize");

                using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
                engine.LoadScript(script.ToArray());

                Assert.AreEqual(engine.Execute(), VMState.FAULT);
                Assert.AreEqual(0, engine.ResultStack.Count);
            }
        }

        [TestMethod]
        public void TestRuntime_Serialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "serialize", 100);
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "serialize", "test");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);

            Assert.AreEqual(engine.ResultStack.Pop<ByteString>().GetSpan().ToHexString(), "280474657374");
            Assert.AreEqual(engine.ResultStack.Pop<ByteString>().GetSpan().ToHexString(), "210164");
        }

        [TestMethod]
        public void TestRuntime_Deserialize()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            // Good

            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "deserialize", "280474657374".HexToBytes());
            script.EmitDynamicCall(NativeContract.StdLib.Hash, "deserialize", "210164".HexToBytes());

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);

            Assert.AreEqual(engine.ResultStack.Pop<Integer>().GetInteger(), 100);
            Assert.AreEqual(engine.ResultStack.Pop<ByteString>().GetString(), "test");
        }
    }
}
