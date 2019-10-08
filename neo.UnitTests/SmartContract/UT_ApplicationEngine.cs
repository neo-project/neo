using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.UnitTests.Ledger;
using Neo.VM;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine
    {
        private string message = null;
        private StackItem item = null;
        private Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void TestLog()
        {
            var snapshot = Store.GetSnapshot().Clone();
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            ApplicationEngine.Log += Test_Log1;
            string logMessage = "TestMessage";

            engine.SendLog(UInt160.Zero, logMessage);
            message.Should().Be(logMessage);

            ApplicationEngine.Log += Test_Log2;
            engine.SendLog(UInt160.Zero, logMessage);
            message.Should().Be(null);

            message = logMessage;
            ApplicationEngine.Log -= Test_Log1;
            engine.SendLog(UInt160.Zero, logMessage);
            message.Should().Be(null);

            ApplicationEngine.Log -= Test_Log2;
            engine.SendLog(UInt160.Zero, logMessage);
            message.Should().Be(null);
        }

        [TestMethod]
        public void TestNotify()
        {
            var snapshot = Store.GetSnapshot().Clone();
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            ApplicationEngine.Notify += Test_Notify1;
            StackItem notifyItem = "TestItem";

            engine.SendNotification(UInt160.Zero, notifyItem);
            item.Should().Be(notifyItem);

            ApplicationEngine.Notify += Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyItem);
            item.Should().Be(null);

            item = notifyItem;
            ApplicationEngine.Notify -= Test_Notify1;
            engine.SendNotification(UInt160.Zero, notifyItem);
            item.Should().Be(null);

            ApplicationEngine.Notify -= Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyItem);
            item.Should().Be(null);
        }

        [TestMethod]
        public void TestDisposable()
        {
            var snapshot = Store.GetSnapshot().Clone();
            var replica = snapshot.Clone();
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            engine.AddDisposable(replica).Should().Be(replica);
            Action action = () => engine.Dispose();
            action.Should().NotThrow();
        }

        private void Test_Log1(object sender, LogEventArgs e)
        {
            message = e.Message;
        }

        private void Test_Log2(object sender, LogEventArgs e)
        {
            message = null;
        }

        private void Test_Notify1(object sender, NotifyEventArgs e)
        {
            item = e.State;
        }

        private void Test_Notify2(object sender, NotifyEventArgs e)
        {
            item = null;
        }

        [TestMethod]
        public void TestCreateDummyBlock()
        {
            var mockSnapshot = new Mock<Snapshot>();
            UInt256 currentBlockHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000");
            TrimmedBlock block = new TrimmedBlock();
            var cache = new TestDataCache<UInt256, TrimmedBlock>();
            cache.Add(currentBlockHash, block);
            mockSnapshot.SetupGet(p => p.Blocks).Returns(cache);
            TestMetaDataCache<HashIndexState> testCache = new TestMetaDataCache<HashIndexState>();
            mockSnapshot.SetupGet(p => p.BlockHashIndex).Returns(testCache);
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            ApplicationEngine.Run(SyscallSystemRuntimeCheckWitnessHash, mockSnapshot.Object);
            mockSnapshot.Object.PersistingBlock.Version.Should().Be(0);
            mockSnapshot.Object.PersistingBlock.PrevHash.Should().Be(currentBlockHash);
            mockSnapshot.Object.PersistingBlock.MerkleRoot.Should().Be(new UInt256());
        }

        [TestMethod]
        public void TestOnSysCall()
        {
            InteropDescriptor descriptor = new InteropDescriptor("System.Blockchain.GetHeight", Blockchain_GetHeight, 0_00000400, TriggerType.Application);
            TestApplicationEngine engine = new TestApplicationEngine(TriggerType.Application, null, null, 0);
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            engine.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
            engine.GetOnSysCall(descriptor.Hash).Should().BeFalse();

            var mockSnapshot = new Mock<Snapshot>();
            TestMetaDataCache<HashIndexState> testCache = new TestMetaDataCache<HashIndexState>();
            mockSnapshot.SetupGet(p => p.BlockHashIndex).Returns(testCache);
            engine = new TestApplicationEngine(TriggerType.Application, null, mockSnapshot.Object, 0, true);
            engine.LoadScript(SyscallSystemRuntimeCheckWitnessHash);
            engine.GetOnSysCall(descriptor.Hash).Should().BeTrue();
        }

        private static bool Blockchain_GetHeight(ApplicationEngine engine)
        {
            engine.CurrentContext.EvaluationStack.Push(engine.Snapshot.Height);
            return true;
        }
    }

    public class TestApplicationEngine : ApplicationEngine
    {
        public TestApplicationEngine(TriggerType trigger, IVerifiable container, Snapshot snapshot, long gas, bool testMode = false) : base(trigger, container, snapshot, gas, testMode)
        {
        }

        public bool GetOnSysCall(uint method)
        {
            return OnSysCall(method);
        }
    }

    public class TestMetaDataCache<T> : MetaDataCache<T> where T : class, ICloneable<T>, ISerializable, new()
    {
        public TestMetaDataCache()
            : base(null)
        {
        }

        protected override void AddInternal(T item)
        {
        }

        protected override T TryGetInternal()
        {
            return new T();
        }

        protected override void UpdateInternal(T item)
        {
        }
    }
}
