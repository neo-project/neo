using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence.LevelDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Neo.Persistence;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.IO.Caching;
using Neo.SmartContract.Manifest;
using Neo.IO.Wrappers;
using Akka.Actor;
using Moq;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_P_Helper
    {
        private LevelDBStore store;
        private static NeoSystem testBlockchain;

        private static string DbPath => Path.GetFullPath(string.Format("Chain_{0}", 123456.ToString("X8")));

        [TestInitialize]
        public void TestSetup()
        {
            if (store == null)
            {
                store = new LevelDBStore(DbPath);
                //testBlockchain = TestBlockchain.InitializeMockNeoSystem();
            }
        }

        [TestCleanup]
        public void TestEnd()
        {
            store.Dispose();
        }

        [TestMethod]
        public void TestContainsBlock()
        {
            Snapshot snapshot = store.GetSnapshot();
            TrimmedBlock block = new TrimmedBlock();
            block.ConsensusData = new ConsensusData();
            block.MerkleRoot = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02");
            block.PrevHash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            block.Timestamp = new DateTime(1968, 06, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
            block.Index = 10;
            block.NextConsensus = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            block.Witness = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            };
            block.Hashes = new UInt256[] { TestUtils.GetTransaction().Hash };

            snapshot.Blocks.Add(block.Hash, block);
            snapshot.Commit();

            //if contains block, return true
            Assert.AreEqual(snapshot.ContainsBlock(block.Hash), true);
            //if not,return false
            bool result = snapshot.ContainsBlock(UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000"));
            Assert.AreEqual(result, false);
        }

        [TestMethod]
        public void TestContainsTransaction()
        {
            Snapshot snapshot = store.GetSnapshot();
            Transaction tx = new Transaction();
            tx.Script = TestUtils.GetByteArray(32, 0x42);
            tx.Sender = UInt160.Zero;
            tx.SystemFee = 4200000000;
            tx.Attributes = new TransactionAttribute[0];
            tx.Witnesses = new[]
            {
                new Witness
                {
                    InvocationScript = new byte[0],
                    VerificationScript = new byte[0]
                }
            };
            TransactionState txState = new TransactionState();
            txState.Transaction = tx;
            txState.BlockIndex = 10;
            snapshot.Transactions.Add(tx.Hash, txState);
            snapshot.Commit();

            Assert.AreEqual(snapshot.ContainsTransaction(tx.Hash), true);

            bool result = snapshot.ContainsTransaction(UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000"));
            Assert.AreEqual(result, false);

        }

        [TestMethod]
        public void TestGetBlock()
        {
            Snapshot snapshot = store.GetSnapshot();
            TrimmedBlock block = new TrimmedBlock();
            block.ConsensusData = new ConsensusData();
            block.MerkleRoot = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02");
            block.PrevHash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            block.Timestamp = new DateTime(1968, 06, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
            block.Index = 0;
            block.NextConsensus = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            block.Witness = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            };
            block.Hashes = new UInt256[] { TestUtils.GetTransaction().Hash };

            snapshot.Blocks.Add(block.Hash, block);
            snapshot.Commit();

            var _Blockchain = new Mock<Blockchain>();
            _Blockchain.Setup(p => p.GetBlockHash(10)).Returns(block.Hash);
            NeoSystem neosystem = new NeoSystem(store);
            var blockchain = Blockchain.Singleton;


            // 
            // Props Props = Akka.Actor.Props.Create(() => new Blockchain(neosystem, store));
            // Props props = Props.Create(new Blockchain(neosystem, store);
            // IActorRef blockchain = neosystem.Blockchain;

            Assert.AreEqual(snapshot.GetBlock(10).Timestamp, block.Timestamp);

        }

        [TestMethod]
        public void TestGetBlockByHash()
        {
            Snapshot snapshot = store.GetSnapshot();
            TrimmedBlock block = new TrimmedBlock();
            block.ConsensusData = new ConsensusData();
            block.MerkleRoot = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff02");
            block.PrevHash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            block.Timestamp = new DateTime(1968, 06, 01, 0, 0, 0, DateTimeKind.Utc).ToTimestamp();
            block.Index = 10;
            block.NextConsensus = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            block.Witness = new Witness
            {
                InvocationScript = new byte[0],
                VerificationScript = new[] { (byte)OpCode.PUSHT }
            };
            block.Hashes = new UInt256[] { TestUtils.GetTransaction().Hash };

            snapshot.Blocks.Add(block.Hash, block);
            snapshot.Commit();

            Block storeBlock = snapshot.GetBlock(block.Hash);
            Assert.AreEqual(storeBlock.MerkleRoot, block.MerkleRoot);
            Assert.AreEqual(storeBlock.PrevHash, block.PrevHash);
            Assert.AreEqual(storeBlock.Timestamp, block.Timestamp);
            Assert.AreEqual(storeBlock.Index, block.Index);
            Assert.AreEqual(storeBlock.NextConsensus, block.NextConsensus);
            Assert.AreEqual(storeBlock.Witness, block.Witness);

        }
    }
}
