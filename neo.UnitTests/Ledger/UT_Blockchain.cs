using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;

namespace Neo.UnitTests.Ledger
{
    internal class TestBlock : Block
    {
        public override bool Verify(Snapshot snapshot)
        {
            return true;
        }

        public static TestBlock Cast(Block input)
        {
            return input.ToArray().AsSerializable<TestBlock>();
        }
    }

    internal class TestHeader : Header
    {
        public override bool Verify(Snapshot snapshot)
        {
            return true;
        }

        public static TestHeader Cast(Header input)
        {
            return input.ToArray().AsSerializable<TestHeader>();
        }
    }

    [TestClass]
    public class UT_Blockchain
    {
        private NeoSystem system;
        private Store store;
        Transaction txSample = Blockchain.GenesisBlock.Transactions[0];

        [TestInitialize]
        public void Initialize()
        {
            system = TestBlockchain.InitializeMockNeoSystem();
            store = TestBlockchain.GetStore();
            Blockchain.Singleton.MemPool.TryAdd(txSample.Hash, txSample);
        }

        [TestMethod]
        public void TestConstructor()
        {
            system.ActorSystem.ActorOf(Blockchain.Props(system, store)).Should().NotBeSameAs(system.Blockchain);
        }

        [TestMethod]
        public void TestContainsBlock()
        {
            Blockchain.Singleton.ContainsBlock(UInt256.Zero).Should().BeFalse();
        }

        [TestMethod]
        public void TestContainsTransaction()
        {
            Blockchain.Singleton.ContainsTransaction(UInt256.Zero).Should().BeFalse();
            Blockchain.Singleton.ContainsTransaction(txSample.Hash).Should().BeTrue();
        }

        [TestMethod]
        public void TestGetCurrentBlockHash()
        {
            Blockchain.Singleton.CurrentBlockHash.Should().Be(UInt256.Parse("0x0d492ce0f38090a65b2b01af50f7a6d685b6b76fbc41672762e96b05d15d742c"));
        }

        [TestMethod]
        public void TestGetCurrentHeaderHash()
        {
            Blockchain.Singleton.CurrentHeaderHash.Should().Be(UInt256.Parse("0x0d492ce0f38090a65b2b01af50f7a6d685b6b76fbc41672762e96b05d15d742c"));
        }

        [TestMethod]
        public void TestGetBlock()
        {
            Blockchain.Singleton.GetBlock(UInt256.Zero).Should().BeNull();
        }

        [TestMethod]
        public void TestGetBlockHash()
        {
            Blockchain.Singleton.GetBlockHash(0).Should().Be(UInt256.Parse("0x0d492ce0f38090a65b2b01af50f7a6d685b6b76fbc41672762e96b05d15d742c"));
            Blockchain.Singleton.GetBlockHash(10).Should().BeNull();
        }

        [TestMethod]
        public void TestGetTransaction()
        {
            Blockchain.Singleton.GetTransaction(UInt256.Zero).Should().BeNull();
            Blockchain.Singleton.GetTransaction(txSample.Hash).Should().NotBeNull();
        }
    }
}
