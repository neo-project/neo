using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_GasToken
    {
        Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void CheckScriptHash_Name()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("name");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteArray));
            Encoding.ASCII.GetString((result as VM.Types.ByteArray).GetByteArray()).Should().Be("GAS");
        }

        [TestMethod]
        public void CheckScriptHash_Symbol()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("symbol");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.ByteArray));
            Encoding.ASCII.GetString((result as VM.Types.ByteArray).GetByteArray()).Should().Be("gas");
        }

        [TestMethod]
        public void CheckScriptHash_Decimals()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("decimals");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Integer));
            (result as VM.Types.Integer).GetBigInteger().Should().Be(8);
        }

        [TestMethod]
        public void CheckScriptHash_SupportedStandards()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            engine.LoadScript(UT_NeoToken.NativeContract("Neo.Native.Tokens.GAS"));

            var script = new ScriptBuilder();
            script.EmitPush(0);
            script.Emit(OpCode.PACK);
            script.EmitPush("supportedStandards");
            engine.LoadScript(script.ToArray());

            engine.Execute();
            engine.State.Should().Be(VMState.HALT);

            var result = engine.ResultStack.Pop();
            result.Should().BeOfType(typeof(VM.Types.Array));
            (result as VM.Types.Array).ToArray()
                .Select(u => Encoding.ASCII.GetString(u.GetByteArray()))
                .ToArray()
                .Should().BeEquivalentTo(new string[] { "NEP-5", "NEP-10" });
        }

        [TestMethod]
        public void CheckScriptHash_BadScript()
        {
            var engine = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), Fixed8.Zero);

            var script = new ScriptBuilder();
            script.Emit(OpCode.NOP);
            engine.LoadScript(script.ToArray());

            typeof(GasToken).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { engine }).Should().Be(false);
        }
    }
}