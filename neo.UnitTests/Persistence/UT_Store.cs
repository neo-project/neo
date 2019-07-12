using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.LevelDB;
using Neo.SmartContract.Manifest;
using Neo.VM;
using System;
using System.IO;
using System.Threading;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Store
    {
        private LevelDBStore store;
        private string dbPath;

        [TestInitialize]
        public void TestSetup()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            dbPath = Path.GetFullPath(nameof(UT_Store) + string.Format("_Chain_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
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
            DataCache<UInt256, TrimmedBlock> blocks = ((IPersistence)store).Blocks;
            TrimmedBlock storeBlock = blocks.TryGet(block.Hash);
            Assert.AreEqual(block.MerkleRoot, storeBlock.MerkleRoot);
            Assert.AreEqual(block.Timestamp, storeBlock.Timestamp);
            Assert.AreEqual(block.PrevHash, storeBlock.PrevHash);
            Assert.AreEqual(block.Index, storeBlock.Index);
            Assert.AreEqual(block.Hashes[0].ToString(), storeBlock.Hashes[0].ToString());
        }

        [TestMethod]
        public void TestGetContracts()
        {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = new ContractState
            {
                Script = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                Manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"))
            };

            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = ((IPersistence)store).Contracts;
            ContractState storeState = contracts.TryGet(state.ScriptHash);
            Assert.AreEqual(storeState.Script.ToHexString(), state.Script.ToHexString());
            Assert.AreEqual(storeState.Manifest.Abi.Hash, state.Manifest.Abi.Hash);
            Assert.AreEqual(storeState.Manifest.ToString(), state.Manifest.ToString());
        }

        [TestMethod]
        public void TestGetHeaderHashList()
        {
            Snapshot snapshot = store.GetSnapshot();
            HeaderHashList headerHashList = new HeaderHashList
            {
                Hashes = new UInt256[] { UInt256.Zero }
            };
            UInt32Wrapper uInt32Wrapper = 123;
            snapshot.HeaderHashList.Add(uInt32Wrapper, headerHashList);
            snapshot.Commit();
            var headerHashLists = ((IPersistence)store).HeaderHashList;
            var storeHeaderHashList = headerHashLists.TryGet(uInt32Wrapper);
            Assert.AreEqual(storeHeaderHashList.Hashes[0], headerHashList.Hashes[0]);
        }

        [TestMethod]
        public void TestGetTransactions()
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
            var transactions = ((IPersistence)store).Transactions;
            var storeTransaction = transactions.TryGet(tx.Hash);
            Assert.AreEqual(storeTransaction.Transaction.Script.ToHexString(), tx.Script.ToHexString());
            Assert.AreEqual(storeTransaction.Transaction.Sender, tx.Sender);
            Assert.AreEqual(storeTransaction.Transaction.SystemFee, tx.SystemFee);
        }

        [TestMethod]
        public void TestGetStorages()
        {
            Snapshot snapshot = store.GetSnapshot();
            var key = new StorageKey
            {
                ScriptHash = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                Key = new byte[] { 0x00, 0xff, 0x00, 0xff }
            };
            var storageItem = new StorageItem
            {
                Value = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                IsConstant = true
            };
            snapshot.Storages.Add(key, storageItem);
            snapshot.Commit();
            var storeStorageItem = ((IPersistence)store).Storages.TryGet(key);
            Assert.AreEqual(storeStorageItem.Value.ToHexString(), storageItem.Value.ToHexString());
            Assert.AreEqual(storeStorageItem.IsConstant, storageItem.IsConstant);
        }

        [TestMethod]
        public void TestGetBlockHashIndex()
        {
            Snapshot snapshot = store.GetSnapshot();
            MetaDataCache<HashIndexState> cache = snapshot.BlockHashIndex;
            HashIndexState state = cache.Get();
            state.Hash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            state.Index = 10;
            snapshot.Commit();
            HashIndexState storeState = ((IPersistence)store).BlockHashIndex.Get();
            Assert.AreEqual(state.Hash, storeState.Hash);
            Assert.AreEqual(state.Index, storeState.Index);
        }

        [TestMethod]
        public void TestGetHeaderHashIndex()
        {
            Snapshot snapshot = store.GetSnapshot();
            MetaDataCache<HashIndexState> cache = snapshot.HeaderHashIndex;
            HashIndexState state = cache.Get();
            state.Hash = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            state.Index = 100;
            snapshot.Commit();
            HashIndexState storeState = ((IPersistence)store).HeaderHashIndex.Get();
            Assert.AreEqual(state.Hash, storeState.Hash);
            Assert.AreEqual(state.Index, storeState.Index);
        }
    }
}