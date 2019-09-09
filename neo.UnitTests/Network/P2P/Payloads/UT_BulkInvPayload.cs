using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Consensus;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_BulkInvPayload
    {
        private uint _nonce = 0;

        private Block CreateBlock(int txs)
        {
            _nonce++;

            var block = new Block()
            {
                ConsensusData = new ConsensusData() { Nonce = _nonce, PrimaryIndex = 0 },
                Index = _nonce,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Timestamp = 0,
                Transactions = new Transaction[txs],
                Version = 0,
                Witness = new Witness() { InvocationScript = new byte[0], VerificationScript = new byte[0] }
            };

            for (int x = 0; x < txs; x++) block.Transactions[x] = CreateTx();

            block.MerkleRoot = Block.CalculateMerkleRoot(block.ConsensusData.Hash, block.Transactions.Select(u => u.Hash).ToArray());
            return block;
        }

        private Transaction CreateTx(int scriptLength = 1)
        {
            _nonce++;

            return new Transaction()
            {
                Attributes = new TransactionAttribute[0],
                Cosigners = new Cosigner[0],
                NetworkFee = 0,
                Nonce = _nonce,
                Script = new byte[scriptLength],
                Sender = UInt160.Zero,
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
                Witnesses = new Witness[0]
            };
        }

        private ConsensusPayload CreateConsensus()
        {
            _nonce++;

            return new ConsensusPayload()
            {
                Data = new byte[0],
                Version = 0,
                BlockIndex = _nonce,
                PrevHash = UInt256.Zero,
                ValidatorIndex = 0,
                ConsensusMessage = new ChangeView()
                {
                    Reason = ChangeViewReason.BlockRejectedByPolicy,
                    Timestamp = 0,
                    ViewNumber = 0,
                },
                Witness = new Witness() { InvocationScript = new byte[0], VerificationScript = new byte[0] }
            };
        }

        [TestMethod]
        public void Max_By_Entries()
        {
            var entries = new Transaction[501];
            for (int x = 0; x < entries.Length; x++) entries[x] = CreateTx();

            var group = BulkInvPayload.CreateGroup(InventoryType.TX, entries.Select(u => u.Hash).ToArray(),
                hash => entries.Where(t => t.Hash == hash).FirstOrDefault()
                )
                .ToArray();

            Assert.AreEqual(2, group.Length);

            Assert.AreEqual(MessageCommand.BulkInv, group[0].Command);
            Assert.AreEqual(InventoryType.TX, ((BulkInvPayload)group[0].Payload).Type);
            Assert.AreEqual(500, ((BulkInvPayload)group[0].Payload).Values.Length);

            Assert.AreEqual(MessageCommand.Transaction, group[1].Command);
            Assert.IsInstanceOfType(group[1].Payload, typeof(Transaction));
        }

        [TestMethod]
        public void Max_By_Size()
        {
            var entries = new Transaction[100];
            for (int x = 0; x < entries.Length; x++)
            {
                entries[x] = CreateTx(10240);
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
            var entries = new Transaction[] { CreateTx(), CreateTx() };
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
            var entries = new Block[] { CreateBlock(1), CreateBlock(1) };
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
            var entries = new ConsensusPayload[] { CreateConsensus(), CreateConsensus() };
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
            var entries = new Transaction[] { CreateTx() };
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
