using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine
    {
        private string eventName = null;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void TestBinary()
        {
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);

            var data = new byte[0];
            CollectionAssert.AreEqual(data, engine.Base64Decode(engine.Base64Encode(data)));

            CollectionAssert.AreEqual(data, engine.Base58Decode(engine.Base58Encode(data)));

            data = new byte[] { 1, 2, 3 };
            CollectionAssert.AreEqual(data, engine.Base64Decode(engine.Base64Encode(data)));

            CollectionAssert.AreEqual(data, engine.Base58Decode(engine.Base58Encode(data)));

            Assert.AreEqual("AQIDBA==", engine.Base64Encode(new byte[] { 1, 2, 3, 4 }));

            Assert.AreEqual("2VfUX", engine.Base58Encode(new byte[] { 1, 2, 3, 4 }));
        }

        [TestMethod]
        public void TestItoaAtoi()
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, null);

            Assert.AreEqual("1", engine.Itoa(BigInteger.One, 10));
            Assert.AreEqual("01", engine.Itoa(BigInteger.One, 16));
            Assert.AreEqual("-1", engine.Itoa(BigInteger.MinusOne, 10));
            Assert.AreEqual("ff", engine.Itoa(BigInteger.MinusOne, 16));
            Assert.AreEqual(-1, engine.Atoi("-1", 10));
            Assert.AreEqual(1, engine.Atoi("+1", 10));
            Assert.AreEqual(-1, engine.Atoi("ff", 16));
            Assert.ThrowsException<System.FormatException>(() => engine.Atoi("a", 10));
            Assert.ThrowsException<System.ArgumentException>(() => engine.Atoi("a", 11));
        }

        [TestMethod]
        public void TestNotify()
        {
            using var snapshot = Blockchain.Singleton.GetSnapshot();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
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
            var snapshot = Blockchain.Singleton.GetSnapshot();
            byte[] SyscallSystemRuntimeCheckWitnessHash = new byte[] { 0x68, 0xf8, 0x27, 0xec, 0x8c };
            ApplicationEngine.Run(SyscallSystemRuntimeCheckWitnessHash, snapshot);
            snapshot.PersistingBlock.Version.Should().Be(0);
            snapshot.PersistingBlock.PrevHash.Should().Be(Blockchain.GenesisBlock.Hash);
            snapshot.PersistingBlock.MerkleRoot.Should().Be(new UInt256());
        }
    }
}
