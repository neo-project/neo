using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM.Types;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_ApplicationEngine
    {
        private string eventName = null;

        [TestMethod]
        public void TestNotify()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestBlockchain.TheNeoSystem.Settings);
            ApplicationEngine.Notify += Test_Notify1;
            const string notifyEvent = "TestEvent";

            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(notifyEvent);

            ApplicationEngine.Notify += Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(null);

            eventName = notifyEvent;
            ApplicationEngine.Notify -= Test_Notify1;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(null);

            ApplicationEngine.Notify -= Test_Notify2;
            engine.SendNotification(UInt160.Zero, notifyEvent, new Array());
            eventName.Should().Be(null);
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
            var snapshot = TestBlockchain.GetTestSnapshot();
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            ApplicationEngine engine = ApplicationEngine.Run(SyscallSystemRuntimeCheckWitnessHash, snapshot);
            engine.PersistingBlock.Version.Should().Be(0);
            engine.PersistingBlock.PrevHash.Should().Be(TestBlockchain.TheNeoSystem.GenesisBlock.Hash);
            engine.PersistingBlock.MerkleRoot.Should().Be(new UInt256());
        }

        [TestMethod]
        public void TestGenesisBlockNonce()
        {
            TestBlockchain.TheNeoSystem.GenesisBlock.Nonce.Should().Be(2083236893);
        }
    }
}
