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

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_LevelDBStore
    {
        private LevelDBStore store;

        private static string DbPath => Path.GetFullPath(string.Format("Chain_{0}", 123456.ToString("X8")));

        [TestInitialize]
        public void TestSetup()
        {
            if (store == null)
            {
                store = new LevelDBStore(DbPath);
            }
        }

        [TestCleanup]
        public void TestEnd()
        {
            store.Dispose();
        }

        [TestMethod]
        public void TestGetBlocks()
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
            DataCache<UInt256, TrimmedBlock> blocks = store.GetBlocks();
            TrimmedBlock newBlock = blocks.TryGet(block.Hash);
            Assert.AreEqual(block.MerkleRoot, newBlock.MerkleRoot);
            Assert.AreEqual(block.Timestamp, newBlock.Timestamp);
            Assert.AreEqual(block.PrevHash, newBlock.PrevHash);
            Assert.AreEqual(block.Index, newBlock.Index);
            Assert.AreEqual(block.Hashes[0].ToString(), newBlock.Hashes[0].ToString());
        }


        [ClassCleanup]
        public static void DeleteDir()
        {
            TestUtils.DeleteFile(DbPath);
        }

        
    }
}
