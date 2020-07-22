using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using VMArray = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NativeContract
    {
        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        private static readonly TestNativeContract testNativeContract = new TestNativeContract();

        [TestMethod]
        public void TestInitialize()
        {
            ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, null, 0);
            testNativeContract.Initialize(ae);
        }

        [TestMethod]
        public void TestInvoke()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.System, null, snapshot, 0);
            engine.LoadScript(testNativeContract.Script);

            ByteString method1 = new ByteString(System.Text.Encoding.Default.GetBytes("wrongMethod"));
            VMArray args1 = new VMArray();
            engine.CurrentContext.EvaluationStack.Push(args1);
            engine.CurrentContext.EvaluationStack.Push(method1);
            Assert.ThrowsException<KeyNotFoundException>(() => testNativeContract.Invoke(engine));

            ByteString method2 = new ByteString(System.Text.Encoding.Default.GetBytes("onPersist"));
            VMArray args2 = new VMArray();
            engine.CurrentContext.EvaluationStack.Push(args2);
            engine.CurrentContext.EvaluationStack.Push(method2);
            testNativeContract.Invoke(engine);
        }

        [TestMethod]
        public void TestOnPersistWithArgs()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            ApplicationEngine engine1 = ApplicationEngine.Create(TriggerType.Application, null, snapshot, 0);
            Assert.ThrowsException<InvalidOperationException>(() => testNativeContract.TestOnPersist(engine1));

            ApplicationEngine engine2 = ApplicationEngine.Create(TriggerType.System, null, snapshot, 0);
            testNativeContract.TestOnPersist(engine2);
        }

        [TestMethod]
        public void TestTestCall()
        {
            ApplicationEngine engine = testNativeContract.TestCall("System.Blockchain.GetHeight", 0);
            engine.ResultStack.Should().BeEmpty();
        }
    }

    public class TestNativeContract : NativeContract
    {
        public override string Name => "test";

        public override int Id => 0x10000006;

        public void TestOnPersist(ApplicationEngine engine)
        {
            OnPersist(engine);
        }
    }
}
