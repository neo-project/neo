using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using VMArray = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NativeContract
    {
        Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void TestInitialize()
        {
            ApplicationEngine ae = new ApplicationEngine(TriggerType.Application, null, null, 0);
            TestNativeContract pc = new TestNativeContract();
            pc.Initialize(ae).Should().BeTrue();

            ae = new ApplicationEngine(TriggerType.System, null, null, 0);
            Action action = () => pc.Initialize(ae);
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void TestInvoke()
        {
            ApplicationEngine engine1 = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), 0);
            TestNativeContract testNativeContract = new TestNativeContract();

            ScriptBuilder sb1 = new ScriptBuilder();

            sb1.EmitSysCall("null".ToInteropMethodHash());
            engine1.LoadScript(sb1.ToArray());
            testNativeContract.Invoke(engine1).Should().BeFalse();

            ApplicationEngine engine2 = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), 0);

            ScriptBuilder sb2 = new ScriptBuilder();
            sb2.EmitSysCall("test".ToInteropMethodHash());
            engine2.LoadScript(sb2.ToArray());

            ByteArray method1 = new ByteArray(System.Text.Encoding.Default.GetBytes("wrongMethod"));
            VMArray args1 = new VMArray();
            engine2.CurrentContext.EvaluationStack.Push(args1);
            engine2.CurrentContext.EvaluationStack.Push(method1);
            testNativeContract.Invoke(engine2).Should().BeFalse();

            ByteArray method2 = new ByteArray(System.Text.Encoding.Default.GetBytes("onPersist"));
            VMArray args2 = new VMArray();
            engine2.CurrentContext.EvaluationStack.Push(args2);
            engine2.CurrentContext.EvaluationStack.Push(method2);
            testNativeContract.Invoke(engine2).Should().BeTrue();
        }

        [TestMethod]
        public void TestOnPersistWithArgs()
        {
            ApplicationEngine engine1 = new ApplicationEngine(TriggerType.Application, null, Store.GetSnapshot(), 0);
            TestNativeContract testNativeContract = new TestNativeContract();
            VMArray args = new VMArray();

            VM.Types.Boolean result1 = new VM.Types.Boolean(false);
            testNativeContract.TestOnPersist(engine1, args).Should().Be(result1);

            ApplicationEngine engine2 = new ApplicationEngine(TriggerType.System, null, Store.GetSnapshot(), 0);
            VM.Types.Boolean result2 = new VM.Types.Boolean(true);
            testNativeContract.TestOnPersist(engine2, args).Should().Be(result2);
        }

        [TestMethod]
        public void TestTestCall()
        {
            TestNativeContract testNativeContract = new TestNativeContract();
            ApplicationEngine engine = testNativeContract.TestCall("System.Blockchain.GetHeight", 0);
            engine.ResultStack.Should().BeEmpty();
        }
    }

    public class TestNativeContract : NativeContract
    {
        public override string ServiceName => "test";
        public StackItem TestOnPersist(ApplicationEngine engine, VMArray args)
        {
            return OnPersist(engine, args);
        }
    }
}
