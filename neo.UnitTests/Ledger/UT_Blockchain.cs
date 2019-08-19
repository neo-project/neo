using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.IO;

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
            TestBlock block = new TestBlock();
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            input.Serialize(writer);
            stream.Seek(0, SeekOrigin.Begin);
            block.Deserialize(reader);
            return block;
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
            TestHeader header = new TestHeader();
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            BinaryReader reader = new BinaryReader(stream);
            input.Serialize(writer);
            stream.Seek(0, SeekOrigin.Begin);
            header.Deserialize(reader);
            return header;
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
            Blockchain.Singleton.ContainsTransaction(UInt256.Zero).Should().BeTrue();
            Blockchain.Singleton.ContainsTransaction(txSample.Hash).Should().BeTrue();
        }

        [TestMethod]
        public void TestGetCurrentBlockHash()
        {
            Blockchain.Singleton.CurrentBlockHash.Should().Be(UInt256.Parse("5662a113d8fa9532ea9c52046a463e2e3fcfcdd6192d99cad805b376fb643ceb"));
        }

        [TestMethod]
        public void TestGetCurrentHeaderHash()
        {
            Blockchain.Singleton.CurrentHeaderHash.Should().Be(UInt256.Parse("5662a113d8fa9532ea9c52046a463e2e3fcfcdd6192d99cad805b376fb643ceb"));
        }

        [TestMethod]
        public void TestGetBlock()
        {
            Blockchain.Singleton.GetBlock(UInt256.Zero).Should().BeNull();
        }

        [TestMethod]
        public void TestGetBlockHash()
        {
            Blockchain.Singleton.GetBlockHash(0).Should().Be(UInt256.Parse("5662a113d8fa9532ea9c52046a463e2e3fcfcdd6192d99cad805b376fb643ceb"));
            Blockchain.Singleton.GetBlockHash(10).Should().BeNull();
        }

        [TestMethod]
        public void TestGetTransaction()
        {
            Blockchain.Singleton.GetTransaction(UInt256.Zero).Should().NotBeNull();
            Blockchain.Singleton.GetTransaction(txSample.Hash).Should().NotBeNull();
        }
    }
}
