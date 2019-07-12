using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Neo.IO;
using Neo.IO.Data.LevelDB;
using Neo.Persistence.LevelDB;
using Neo.Ledger;
using System.Threading;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_DbMetaDataCache
    {
        private DB db;

        private string DbPath;

        [TestInitialize]
        public void TestSetUp()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            var options = new Options();
            options.CreateIfMissing = true;
            DbPath = Path.GetFullPath(nameof(UT_DbCache) + string.Format("_Chain_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
            db = DB.Open(DbPath, options);
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            db.Dispose();
            TestUtils.DeleteFile(DbPath);
        }

        [TestMethod]
        public void TestContructor()
        {
            Snapshot snapshot = db.GetSnapshot();
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            var dbmetadatecace = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
        }

        [TestMethod]
        public void TestAddInternal()
        {
            WriteBatch batch = new WriteBatch();
            ReadOptions options = ReadOptions.Default;
            DbMetaDataCache<HashIndexState> dbMetaDataCache = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexState = dbMetaDataCache.Get();
            hashIndexState.Hash = UInt256.Parse("0x9852a2ab376040a5a1697613590e9fb251cec0a85ca1a6857c31a98512bdb009");
            hashIndexState.Index = 1;
            dbMetaDataCache.Commit();
            db.Write(WriteOptions.Default, batch);
            Slice value = db.Get(ReadOptions.Default, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexStateOut = value.ToArray().AsSerializable<HashIndexState>();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateOut.Hash);
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            WriteBatch batch = new WriteBatch();
            ReadOptions options = ReadOptions.Default;
            HashIndexState hashIndexState = new HashIndexState();
            hashIndexState.Hash = UInt256.Parse("0x9852a2ab376040a5a1697613590e9fb251cec0a85ca1a6857c31a98512bdb009");
            hashIndexState.Index = 1;
            db.Put(WriteOptions.Default, Prefixes.IX_CurrentBlock, hashIndexState.ToArray());
            Slice value = db.Get(ReadOptions.Default, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexStateGet = value.ToArray().AsSerializable<HashIndexState>();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateGet.Hash);
            DbMetaDataCache<HashIndexState> dbMetaDataCache = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexStateOut = dbMetaDataCache.Get();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateOut.Hash);
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            WriteBatch batch = new WriteBatch();
            ReadOptions options = ReadOptions.Default;
            HashIndexState hashIndexState = new HashIndexState();
            hashIndexState.Hash = UInt256.Parse("0x9852a2ab376040a5a1697613590e9fb251cec0a85ca1a6857c31a98512bdb009");
            hashIndexState.Index = 1;
            db.Put(WriteOptions.Default, Prefixes.IX_CurrentBlock, hashIndexState.ToArray());
            Slice value = db.Get(ReadOptions.Default, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexStateGet = value.ToArray().AsSerializable<HashIndexState>();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateGet.Hash);
            DbMetaDataCache<HashIndexState> dbMetaDataCache = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexStateOut = dbMetaDataCache.GetAndChange();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateOut.Hash);
            hashIndexStateOut.Hash = UInt256.Parse("9afadf2ccf0cb70a6ff2c5492b34a9564fed4c0516f3ec3744decdc9b7e892d4");
            hashIndexStateOut.Index = 2;
            dbMetaDataCache.Commit();
            db.Write(WriteOptions.Default, batch);
            value = db.Get(ReadOptions.Default, Prefixes.IX_CurrentBlock);
            hashIndexStateGet = value.ToArray().AsSerializable<HashIndexState>();
            Assert.AreEqual(hashIndexStateOut.Hash, hashIndexStateGet.Hash);
        }
    }
}