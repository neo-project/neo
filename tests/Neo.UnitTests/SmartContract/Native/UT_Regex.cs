using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_Regex
    {

        [TestMethod]
        public void TestStartsWith()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            // Positive case
            script.EmitDynamicCall(NativeContract.Regex.Hash, "startsWith", "HelloWorld", "Hello");
            // Negative case
            script.EmitDynamicCall(NativeContract.Regex.Hash, "startsWith", "HelloWorld", "World");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean());
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());
        }

        [TestMethod]
        public void TestEndsWith()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            // Positive case
            script.EmitDynamicCall(NativeContract.Regex.Hash, "endsWith", "HelloWorld", "World");
            // Negative case
            script.EmitDynamicCall(NativeContract.Regex.Hash, "endsWith", "HelloWorld", "Hello");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean());
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());
        }

        [TestMethod]
        public void TestContains()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            // Positive case
            script.EmitDynamicCall(NativeContract.Regex.Hash, "contains", "HelloWorld", "llo");
            // Negative case
            script.EmitDynamicCall(NativeContract.Regex.Hash, "contains", "HelloWorld", "xyz");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);
            Assert.IsFalse(engine.ResultStack.Pop<Boolean>().GetBoolean());
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());
        }

        [TestMethod]
        public void TestIndexOf()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            // Finding the first occurrence
            script.EmitDynamicCall(NativeContract.Regex.Hash, "indexOf", "HelloWorld", "l");
            // Element not found
            script.EmitDynamicCall(NativeContract.Regex.Hash, "indexOf", "HelloWorld", "xyz");

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(2, engine.ResultStack.Count);
            Assert.AreEqual(-1, engine.ResultStack.Pop<Integer>().GetInteger());
            Assert.AreEqual(2, engine.ResultStack.Pop<Integer>().GetInteger());
        }

        [TestMethod]
        public void TestSubstring()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            // Normal substring
            script.EmitDynamicCall(NativeContract.Regex.Hash, "substring", "HelloWorld", 0, 5);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.AreEqual("Hello", engine.ResultStack.Pop<ByteString>().GetString());
        }

        [TestMethod]
        public void TestExtremeCases() // all cases finished within 183ms
        {
            string MaxInput = "ksNLwDq6kOSpCmLDwG99YG3kOn58RAMOigesVUavY7" +
                "KNqg84cztccaRrYoimMWmjU6mwBfd4hVx6pjJSNmTcMzXHPH1dajle01" +
                "jChPxDeEGYONBAiaLtvrJRDBQWZLNgnrUBRGsVPOpd24UI6SEZ3MPgDd" +
                "gDTLilSwNRJLbhmPLpfE4cdztbKcfTBxG1CtptfSSXMmuCS5S1s6EjCN" +
                "cj3E2f6kbA0PKpeSyd3WbG35KU6Hp9Av7jTmSpXL7Z8DwevloM99G12Gt" +
                "WZ1YW4CTApApyQO51aKBTFWrirLgoVgGEnbT2YCwB3Uf0b6CWP9H50pD1" +
                "gfgzElrf27gCngXKauw9e947B4F5oJ0bEGESVVJVGxTvgOzYft9C2UUQKm" +
                "QksciFoj993wztDa8wSxuBzvEY2Nna4VVD6FQJHxrJ2EsbhJhyjHg5ZB9L" +
                "NiUELUWiTyjHJ9WkkmukYPjkbJdvUp9LF3wKylIbcqKbDtqd98kN0hHnKr" +
                "TO9DEabHzhQo01";

            string start = "ksNLwDq6kOSpCmLDwG99YG3kOn58RAMOigesVUavY7" +
                "KNqg84cztccaRrYoimMWmjU6mwBfd4hVx6pjJSNmTcMzXHPH1dajle01" +
                "jChPxDeEGYONBAiaLtvrJRDBQWZLNgnrUBRGsVPOpd24UI6SEZ3MPgDd" +
                "gDTLilSwNRJLbhmPLpfE4cdztbKcfTBxG1CtptfSSXMmuCS5S1s6EjCN" +
                "cj3E2f6kbA0PKpeSyd3WbG35KU6Hp9Av7jTmSpXL7Z8DwevloM99G12Gt";
            string end = "WZ1YW4CTApApyQO51aKBTFWrirLgoVgGEnbT2YCwB3Uf0b6CWP9H50pD1" +
                "gfgzElrf27gCngXKauw9e947B4F5oJ0bEGESVVJVGxTvgOzYft9C2UUQKm" +
                "QksciFoj993wztDa8wSxuBzvEY2Nna4VVD6FQJHxrJ2EsbhJhyjHg5ZB9L" +
                "NiUELUWiTyjHJ9WkkmukYPjkbJdvUp9LF3wKylIbcqKbDtqd98kN0hHnKr" +
                "TO9DEabHzhQo01";

            string contain = "WZ1YW4CTApApyQO51aKBTFWrirLgoVgGEnbT2YCwB3Uf0b6CWP9H50pD1" +
                "gfgzElrf27gCngXKauw9e947B4F5oJ0bEGESVVJVGxTvgOzYft9C2UUQKm" +
                "QksciFoj993wztDa8wSxuBzvEY2Nna4VVD6FQJHxrJ2EsbhJhyjHg5ZB9L";


            var snapshot = TestBlockchain.GetTestSnapshot();

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Regex.Hash, "startsWith", MaxInput, start);
            script.EmitDynamicCall(NativeContract.Regex.Hash, "endsWith", MaxInput, end);
            script.EmitDynamicCall(NativeContract.Regex.Hash, "contains", MaxInput, contain);
            script.EmitDynamicCall(NativeContract.Regex.Hash, "indexOf", MaxInput, "0bEGESVVJVGxTvgOzYf");
            script.EmitDynamicCall(NativeContract.Regex.Hash, "substring", MaxInput, 0, 500);

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(engine.Execute(), VMState.HALT);
            Assert.AreEqual(5, engine.ResultStack.Count);

            engine.ResultStack.Pop<ByteString>();

            Assert.AreEqual(354, engine.ResultStack.Pop<Integer>().GetInteger());
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());
            Assert.IsTrue(engine.ResultStack.Pop<Boolean>().GetBoolean());
        }
    }
}
