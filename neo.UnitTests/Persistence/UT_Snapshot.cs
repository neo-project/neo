using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.LevelDB;
using Neo.VM;

namespace Neo.UnitTests
{
    class MySnapshot : Snapshot
    {
        public override DataCache<UInt256, TrimmedBlock> Blocks => throw new NotImplementedException();

        public override DataCache<UInt256, TransactionState> Transactions => throw new NotImplementedException();

        public override DataCache<UInt160, ContractState> Contracts => throw new NotImplementedException();

        public override DataCache<StorageKey, StorageItem> Storages => throw new NotImplementedException();

        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList => throw new NotImplementedException();

        public override MetaDataCache<HashIndexState> BlockHashIndex => throw new NotImplementedException();

        public override MetaDataCache<HashIndexState> HeaderHashIndex => throw new NotImplementedException();

        public void SetPersistingBlock(Block block)
        {
            PersistingBlock = block;
        }

        public Block GetPersistingBlock()
        {
            return PersistingBlock;
        }
    }

    [TestClass]
    public class UT_Snapshot
    {
        private Snapshot snapshot;

        private LevelDBStore store;

        private string dbPath;

        [TestInitialize]
        public void TestSetup()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            dbPath = Path.GetFullPath(nameof(UT_Snapshot) + string.Format("_Chain_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
            if (store == null)
            {
                store = new LevelDBStore(dbPath);
            }
        }

        [TestCleanup]
        public void DeleteDir()
        {
            store.Dispose();
            store = null;
            TestUtils.DeleteFile(dbPath);
        }


        [TestMethod]
        public void TestGetCurrentHeaderHash()
        {
            snapshot = store.GetSnapshot();
            var state = snapshot.HeaderHashIndex.Get();
            state.Hash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            state.Index = 10;
            snapshot.Commit();
            snapshot.CurrentHeaderHash.Should().Be(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01"));
            snapshot.Dispose();
            // test get Item from internal
            snapshot = store.GetSnapshot();
            snapshot.CurrentHeaderHash.Should().Be(UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01"));
        }

        [TestMethod]
        public void TestGetHeaderHeight()
        {
            snapshot = store.GetSnapshot();
            var state = snapshot.HeaderHashIndex.Get();
            state.Hash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            state.Index = 10;
            snapshot.Commit();
            snapshot.HeaderHeight.Should().Be(10);
            snapshot.Dispose();
            // test get Item from internal
            snapshot = store.GetSnapshot();
            snapshot.HeaderHeight.Should().Be(10);
        }

        [TestMethod]
        public void TestSetPersistingBlock()
        {
            var mySnapshot = new MySnapshot();
            Block block = new Block();
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
            mySnapshot.PersistingBlock = block;
            Block setblock = mySnapshot.PersistingBlock;
            Assert.AreEqual(block.MerkleRoot.ToString(), setblock.MerkleRoot.ToString());
            Assert.AreEqual(block.Timestamp, setblock.Timestamp);
            Assert.AreEqual(block.PrevHash, setblock.PrevHash);
            Assert.AreEqual(block.Index, setblock.Index);
        }

        [TestMethod]
        public void TestDispose()
        {
            var mySnapshot = new MySnapshot();
            mySnapshot.Dispose();
            true.Should().BeTrue();
        }
    }
}