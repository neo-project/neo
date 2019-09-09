using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_BulkInvPayload
    {
        [TestMethod]
        public void Max_By_Size()
        {
            var entries = new Transaction[100];
            for (int x = 0; x < entries.Length; x++)
            {
                entries[x] = TestUtils.CreateTransaction(10240);
            }

            var groups = BulkInvPayload.CreateGroup(InventoryType.TX, entries.Select(u => u.Hash).ToArray(),
                hash => entries.Where(t => t.Hash == hash).FirstOrDefault()
                )
                .ToArray();

            foreach (var group in groups)
            {
                if (group.Command == MessageCommand.Transaction) continue;

                Assert.AreEqual(MessageCommand.BulkInv, group.Command);
                Assert.AreEqual(InventoryType.TX, ((BulkInvPayload)group.Payload).Type);
                Assert.IsTrue(((BulkInvPayload)group.Payload).Values.Sum(u => u.Size) < BulkInvPayload.MaxSize);
            }
        }

        [TestMethod]
        public void Serialization_Transaction()
        {
            var entries = new Transaction[] { TestUtils.CreateTransaction(), TestUtils.CreateTransaction() };
            var hashes = entries.Select(u => u.Hash).ToArray();

            var group = BulkInvPayload.CreateGroup(InventoryType.TX, hashes,
                hash => entries.Where(t => t.Hash == hash).FirstOrDefault()
                )
                .ToArray();

            Assert.AreEqual(1, group.Length);
            Assert.AreEqual(MessageCommand.BulkInv, group[0].Command);
            Assert.IsInstanceOfType(group[0].Payload, typeof(BulkInvPayload));

            var payload = (BulkInvPayload)group[0].Payload;
            var data = payload.ToArray();
            var copy = data.AsSerializable<BulkInvPayload>();

            Assert.AreEqual(InventoryType.TX, copy.Type);
            Assert.AreEqual(entries[0].Hash, ((Transaction)copy.Values[0]).Hash);
            Assert.AreEqual(entries[1].Hash, ((Transaction)copy.Values[1]).Hash);
        }

        [TestMethod]
        public void Serialization_Block()
        {
            var entries = new Block[] { TestUtils.CreateBlock(1), TestUtils.CreateBlock(1) };
            var hashes = entries.Select(u => u.Hash).ToArray();

            var group = BulkInvPayload.CreateGroup(InventoryType.Block, hashes,
                hash => entries.Where(t => t.Hash == hash).FirstOrDefault()
                )
                .ToArray();

            Assert.AreEqual(1, group.Length);
            Assert.AreEqual(MessageCommand.BulkInv, group[0].Command);
            Assert.IsInstanceOfType(group[0].Payload, typeof(BulkInvPayload));

            var payload = (BulkInvPayload)group[0].Payload;
            var data = payload.ToArray();
            var copy = data.AsSerializable<BulkInvPayload>();

            Assert.AreEqual(InventoryType.Block, copy.Type);
            Assert.AreEqual(entries[0].Hash, ((Block)copy.Values[0]).Hash);
            Assert.AreEqual(entries[1].Hash, ((Block)copy.Values[1]).Hash);
        }

        [TestMethod]
        public void Serialization_Consensus()
        {
            var entries = new ConsensusPayload[] { TestUtils.CreateConsensusPayload(), TestUtils.CreateConsensusPayload() };
            var hashes = entries.Select(u => u.Hash).ToArray();

            var group = BulkInvPayload.CreateGroup(InventoryType.Consensus, hashes,
                hash => entries.Where(t => t.Hash == hash).FirstOrDefault()
                )
                .ToArray();

            Assert.AreEqual(1, group.Length);
            Assert.AreEqual(MessageCommand.BulkInv, group[0].Command);
            Assert.IsInstanceOfType(group[0].Payload, typeof(BulkInvPayload));

            var payload = (BulkInvPayload)group[0].Payload;
            var data = payload.ToArray();
            var copy = data.AsSerializable<BulkInvPayload>();

            Assert.AreEqual(InventoryType.Consensus, copy.Type);
            Assert.AreEqual(entries[0].Hash, ((ConsensusPayload)copy.Values[0]).Hash);
            Assert.AreEqual(entries[1].Hash, ((ConsensusPayload)copy.Values[1]).Hash);
        }

        [TestMethod]
        public void Null_Entries()
        {
            var entries = new Transaction[] { TestUtils.CreateTransaction() };
            var hashes = new UInt256[] { UInt256.Zero, entries[0].Hash };

            var group = BulkInvPayload.CreateGroup(InventoryType.TX, hashes,
                hash => entries.Where(t => t.Hash == hash).FirstOrDefault()
                )
                .ToArray();

            Assert.AreEqual(1, group.Length);
            Assert.AreEqual(MessageCommand.Transaction, group[0].Command);
            Assert.IsInstanceOfType(group[0].Payload, typeof(Transaction));
        }
    }
}
