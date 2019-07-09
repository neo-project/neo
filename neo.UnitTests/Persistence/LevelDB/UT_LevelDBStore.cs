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

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_LevelDBStore
    {
        private LevelDBStore store;

        private static string DbPath => Path.GetFullPath(nameof(UT_LevelDBStore) + string.Format("_Chain_{0}", 123456.ToString("X8")));

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
            //get block from internal
            TrimmedBlock storeBlock = blocks.TryGet(block.Hash);
            Assert.AreEqual(block.MerkleRoot, storeBlock.MerkleRoot);
            Assert.AreEqual(block.Timestamp, storeBlock.Timestamp);
            Assert.AreEqual(block.PrevHash, storeBlock.PrevHash);
            Assert.AreEqual(block.Index, storeBlock.Index);
            Assert.AreEqual(block.Hashes[0].ToString(), storeBlock.Hashes[0].ToString());
            //get block from cache
            storeBlock = blocks.TryGet(block.Hash);
            Assert.AreEqual(block.MerkleRoot, storeBlock.MerkleRoot);
            Assert.AreEqual(block.Timestamp, storeBlock.Timestamp);
            Assert.AreEqual(block.PrevHash, storeBlock.PrevHash);
            Assert.AreEqual(block.Index, storeBlock.Index);
            Assert.AreEqual(block.Hashes[0].ToString(), storeBlock.Hashes[0].ToString());
            
            blocks.Delete(block.Hash);
            Assert.IsNull(blocks.TryGet(block.Hash));
            Assert.IsNull(blocks.TryGet(UInt256.Zero));
        }

        [TestMethod]
        public void TestGetContracts()
        {
            Snapshot snapshot = store.GetSnapshot();
            ContractState state = new ContractState {
                Script = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                Manifest = ContractManifest.CreateDefault(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"))
            };

            snapshot.Contracts.Add(state.ScriptHash, state);
            snapshot.Commit();
            DataCache<UInt160, ContractState> contracts = store.GetContracts();
            ContractState storeState = contracts.TryGet(state.ScriptHash);
            Assert.AreEqual(storeState.Script.ToHexString(), state.Script.ToHexString());
            Assert.AreEqual(storeState.Manifest.Abi.Hash, state.Manifest.Abi.Hash);
            Assert.AreEqual(storeState.Manifest.ToString(), state.Manifest.ToString());
        }

        [TestMethod]
        public void TestGetHeaderHashList()
        {
            Snapshot snapshot = store.GetSnapshot();
            HeaderHashList headerHashList = new HeaderHashList {
                Hashes = new UInt256[] { UInt256.Zero}
            };
            UInt32Wrapper uInt32Wrapper = 123;
            snapshot.HeaderHashList.Add(uInt32Wrapper, headerHashList);
            snapshot.Commit();
            var headerHashLists = store.GetHeaderHashList();
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
            snapshot.Transactions.Add(tx.Hash,txState);
            snapshot.Commit();
            var transactions = store.GetTransactions();
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
            var storeStorageItem = store.GetStorages().TryGet(key);
            Assert.AreEqual(storeStorageItem.Value.ToHexString(),storageItem.Value.ToHexString());
            Assert.AreEqual(storeStorageItem.IsConstant,storageItem.IsConstant);

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
            HashIndexState storeState = store.GetBlockHashIndex().Get();
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
            HashIndexState storeState = store.GetHeaderHashIndex().Get();
            Assert.AreEqual(state.Hash, storeState.Hash);
            Assert.AreEqual(state.Index, storeState.Index);
        }

       

        [TestMethod]
        public void TestPutAndGet()
        {
            store.Put(0x01, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x00, 0xff, 0x00, 0xff });
            var value = store.Get(0x01, new byte[] { 0x01, 0x02, 0x03, 0x04 });
            Assert.AreEqual(value.ToHexString(), new byte[] { 0x00, 0xff, 0x00, 0xff }.ToHexString());
        }

        [TestMethod]
        public void TestPutSyncAndGet()
        {
            store.PutSync(0x02, new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x00, 0xff, 0x00, 0xff });
            var value = store.Get(0x02, new byte[] { 0x01, 0x02, 0x03, 0x04 });
            Assert.AreEqual(value.ToHexString(), new byte[] { 0x00, 0xff, 0x00, 0xff }.ToHexString());
        }

        [TestMethod]
        public void TestGetNull()
        {
            Assert.IsNull(store.Get(0x03, new byte[] { 0x01, 0x02, 0x03, 0x04 }));
        }

        [ClassCleanup]
        public static void DeleteDir()
        {
            TestUtils.DeleteFile(DbPath);
        }

        
    }
}
