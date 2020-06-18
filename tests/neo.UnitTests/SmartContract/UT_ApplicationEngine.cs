using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Ledger;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine
    {
        private string message = null;
        private string eventName = null;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestNotify()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            var engine = new ApplicationEngine(TriggerType.Application, null, snapshot, 0, true);
            ApplicationEngine.Notify += Test_Notify1;
            const string notifyEvent = "TestEvent";

            engine.SendNotification(UInt160.Zero, notifyEvent, null);
            eventName.Should().Be(notifyEvent);

            ApplicationEngine.Notify += Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, null);
            eventName.Should().Be(null);

            eventName = notifyEvent;
            ApplicationEngine.Notify -= Test_Notify1;
            engine.SendNotification(UInt160.Zero, notifyEvent, null);
            eventName.Should().Be(null);

            ApplicationEngine.Notify -= Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, null);
            eventName.Should().Be(null);
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
            eventName = e.EventName;
        }

        private void Test_Notify2(object sender, NotifyEventArgs e)
        {
            eventName = null;
        }

        [TestMethod]
        public void TestCreateDummyBlock()
        {
            var snapshot = Blockchain.Singleton.GetSnapshot();
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            ApplicationEngine.Run(SyscallSystemRuntimeCheckWitnessHash, snapshot);
            snapshot.PersistingBlock.Version.Should().Be(0);
            snapshot.PersistingBlock.PrevHash.Should().Be(Blockchain.GenesisBlock.Hash);
            snapshot.PersistingBlock.MerkleRoot.Should().Be(new UInt256());
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
