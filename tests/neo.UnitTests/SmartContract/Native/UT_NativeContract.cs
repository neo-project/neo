using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
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
            ApplicationEngine ae = ApplicationEngine.Create(TriggerType.Application, null, null, null, 0);
            testNativeContract.Initialize(ae);
        }

        private class DummyNative : NativeContract
        {
            public override int Id => 1;

            [ContractMethod(0, CallFlags.None)]
            public void NetTypes(
                    bool p1, sbyte p2, byte p3, short p4, ushort p5, int p6, uint p7, long p8, ulong p9, BigInteger p10,
                    byte[] p11, string p12, IInteroperable p13, ISerializable p14, int[] p15, ContractParameterType p16,
                    object p17)
            { }

            [ContractMethod(0, CallFlags.None)]
            public void VMTypes(
                    VM.Types.Boolean p1, VM.Types.Integer p2, VM.Types.ByteString p3, VM.Types.Buffer p4,
                    VM.Types.Array p5, VM.Types.Struct p6, VM.Types.Map p7, VM.Types.StackItem p8
                )
            { }
        }

        [TestMethod]
        public void TestToParameter()
        {
            var manifest = new DummyNative().Manifest;
            var netTypes = manifest.Abi.GetMethod("netTypes");

            Assert.AreEqual(netTypes.ReturnType, ContractParameterType.Void);
            Assert.AreEqual(netTypes.Parameters[0].Type, ContractParameterType.Boolean);
            Assert.AreEqual(netTypes.Parameters[1].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[2].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[3].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[4].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[5].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[6].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[7].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[8].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[9].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[10].Type, ContractParameterType.ByteArray);
            Assert.AreEqual(netTypes.Parameters[11].Type, ContractParameterType.String);
            Assert.AreEqual(netTypes.Parameters[12].Type, ContractParameterType.Array);
            Assert.AreEqual(netTypes.Parameters[13].Type, ContractParameterType.ByteArray);
            Assert.AreEqual(netTypes.Parameters[14].Type, ContractParameterType.Array);
            Assert.AreEqual(netTypes.Parameters[15].Type, ContractParameterType.Integer);
            Assert.AreEqual(netTypes.Parameters[16].Type, ContractParameterType.Any);

            var vmTypes = manifest.Abi.GetMethod("vMTypes");

            Assert.AreEqual(vmTypes.ReturnType, ContractParameterType.Void);
            Assert.AreEqual(vmTypes.Parameters[0].Type, ContractParameterType.Boolean);
            Assert.AreEqual(vmTypes.Parameters[1].Type, ContractParameterType.Integer);
            Assert.AreEqual(vmTypes.Parameters[2].Type, ContractParameterType.ByteArray);
            Assert.AreEqual(vmTypes.Parameters[3].Type, ContractParameterType.ByteArray);
            Assert.AreEqual(vmTypes.Parameters[4].Type, ContractParameterType.Array);
            Assert.AreEqual(vmTypes.Parameters[5].Type, ContractParameterType.Array);
            Assert.AreEqual(vmTypes.Parameters[6].Type, ContractParameterType.Map);
            Assert.AreEqual(vmTypes.Parameters[7].Type, ContractParameterType.Any);
        }

        [TestMethod]
        public void TestGetContract()
        {
            Assert.IsTrue(NativeContract.NEO == NativeContract.GetContract(NativeContract.NEO.Name));
            Assert.IsTrue(NativeContract.NEO == NativeContract.GetContract(NativeContract.NEO.Hash));
        }

        [TestMethod]
        public void TestInvoke()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            ApplicationEngine engine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, null, 0);
            engine.LoadScript(testNativeContract.Script, configureState: p => p.ScriptHash = testNativeContract.Hash);

            ByteString method1 = new ByteString(System.Text.Encoding.Default.GetBytes("wrongMethod"));
            VMArray args1 = new VMArray();
            engine.CurrentContext.EvaluationStack.Push(args1);
            engine.CurrentContext.EvaluationStack.Push(method1);
            Assert.ThrowsException<KeyNotFoundException>(() => testNativeContract.Invoke(engine));

            ByteString method2 = new ByteString(System.Text.Encoding.Default.GetBytes("helloWorld"));
            VMArray args2 = new VMArray();
            engine.CurrentContext.EvaluationStack.Push(args2);
            engine.CurrentContext.EvaluationStack.Push(method2);
            testNativeContract.Invoke(engine);
        }

        [TestMethod]
        public void TestTrigger()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();

            ApplicationEngine engine1 = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, 0);
            Assert.ThrowsException<InvalidOperationException>(() => testNativeContract.TestTrigger(engine1));

            ApplicationEngine engine2 = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, null, 0);
            testNativeContract.TestTrigger(engine2);
        }

        [TestMethod]
        public void TestTestCall()
        {
            ApplicationEngine engine = testNativeContract.TestCall("System.Blockchain.GetHeight", false, 0);
            engine.ResultStack.Should().BeEmpty();
        }
    }

    public class TestNativeContract : NativeContract
    {
        public override int Id => 0x10000006;

        [ContractMethod(0, CallFlags.None)]
        public string HelloWorld => "hello world";

        public void TestTrigger(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.OnPersist) throw new InvalidOperationException();
        }
    }
}
