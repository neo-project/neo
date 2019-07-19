using LevelDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Data.LevelDB;
using Neo.Ledger;
using Neo.Persistence.LevelDB;
using System;
using System.IO;
using System.Threading;

namespace Neo.UnitTests.Persistence.LevelDB
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
            var options = new Options
            {
                CreateIfMissing = true
            };
            DbPath = Path.GetFullPath(nameof(UT_DbCache) + string.Format("_Chain_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
            db = new DB(options, DbPath);
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            db.Dispose();
            TestUtils.DeleteFiles(DbPath);
        }

        [TestMethod]
        public void TestContructor()
        {
            var snapshot = db.CreateSnapshot();
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            var dbmetadatecace = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
        }

        [TestMethod]
        public void TestAddInternal()
        {
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions();
            DbMetaDataCache<HashIndexState> dbMetaDataCache = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexState = dbMetaDataCache.Get();
            hashIndexState.Hash = UInt256.Parse("0x9852a2ab376040a5a1697613590e9fb251cec0a85ca1a6857c31a98512bdb009");
            hashIndexState.Index = 1;
            dbMetaDataCache.Commit();
            db.Write(batch, new WriteOptions());
            Slice value = db.Get(new byte[] { Prefixes.IX_CurrentBlock }, new ReadOptions());
            HashIndexState hashIndexStateOut = value.ToArray().AsSerializable<HashIndexState>();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateOut.Hash);
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions();
            HashIndexState hashIndexState = new HashIndexState();
            hashIndexState.Hash = UInt256.Parse("0x9852a2ab376040a5a1697613590e9fb251cec0a85ca1a6857c31a98512bdb009");
            hashIndexState.Index = 1;
            db.Put(new byte[] { Prefixes.IX_CurrentBlock }, hashIndexState.ToArray(), new WriteOptions());
            Slice value = db.Get(new byte[] { Prefixes.IX_CurrentBlock }, new ReadOptions());
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
            ReadOptions options = new ReadOptions();
            HashIndexState hashIndexState = new HashIndexState();
            hashIndexState.Hash = UInt256.Parse("0x9852a2ab376040a5a1697613590e9fb251cec0a85ca1a6857c31a98512bdb009");
            hashIndexState.Index = 1;
            db.Put(new byte[] { Prefixes.IX_CurrentBlock }, hashIndexState.ToArray(), new WriteOptions());
            Slice value = db.Get(new byte[] { Prefixes.IX_CurrentBlock }, new ReadOptions());
            HashIndexState hashIndexStateGet = value.ToArray().AsSerializable<HashIndexState>();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateGet.Hash);
            DbMetaDataCache<HashIndexState> dbMetaDataCache = new DbMetaDataCache<HashIndexState>(db, options, batch, Prefixes.IX_CurrentBlock);
            HashIndexState hashIndexStateOut = dbMetaDataCache.GetAndChange();
            Assert.AreEqual(hashIndexState.Hash, hashIndexStateOut.Hash);
            hashIndexStateOut.Hash = UInt256.Parse("9afadf2ccf0cb70a6ff2c5492b34a9564fed4c0516f3ec3744decdc9b7e892d4");
            hashIndexStateOut.Index = 2;
            dbMetaDataCache.Commit();
            db.Write(batch, new WriteOptions());
            value = db.Get(new byte[] { Prefixes.IX_CurrentBlock }, new ReadOptions());
            hashIndexStateGet = value.ToArray().AsSerializable<HashIndexState>();
            Assert.AreEqual(hashIndexStateOut.Hash, hashIndexStateGet.Hash);
        }
    }
}