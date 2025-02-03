// Copyright (C) 2015-2025 The Neo Project.
//
// StoreTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable CS0618 // Type or member is obsolete

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Storage.LevelDB;
using Neo.Persistence;
using System.IO;
using System.Linq;

namespace Neo.Plugins.Storage.Tests
{
    [TestClass]
    public class StoreTest
    {
        private const string path_leveldb = "Data_LevelDB_UT";
        private const string path_rocksdb = "Data_RocksDB_UT";
        private static LevelDBStore levelDbStore;
        private static RocksDBStore rocksDBStore;

        [TestInitialize]
        public void OnStart()
        {
            levelDbStore ??= new LevelDBStore();
            rocksDBStore ??= new RocksDBStore();
            OnEnd();
        }

        [TestCleanup]
        public void OnEnd()
        {
            if (Directory.Exists(path_leveldb)) Directory.Delete(path_leveldb, true);
            if (Directory.Exists(path_rocksdb)) Directory.Delete(path_rocksdb, true);
        }

        [TestMethod]
        public void TestMemory()
        {
            using var store = new MemoryStore();
            TestPersistenceDelete(store);
            // Test all with the same store

            TestStorage(store);

            // Test with different storages

            TestPersistenceWrite(store);
            TestPersistenceRead(store, true);
            TestPersistenceDelete(store);
            TestPersistenceRead(store, false);
        }

        [TestMethod]
        public void TestLevelDb()
        {
            TestPersistenceDelete(levelDbStore.GetStore(path_leveldb));
            // Test all with the same store

            TestStorage(levelDbStore.GetStore(path_leveldb));

            // Test with different storages

            TestPersistenceWrite(levelDbStore.GetStore(path_leveldb));
            TestPersistenceRead(levelDbStore.GetStore(path_leveldb), true);
            TestPersistenceDelete(levelDbStore.GetStore(path_leveldb));
            TestPersistenceRead(levelDbStore.GetStore(path_leveldb), false);
        }

        [TestMethod]
        public void TestLevelDbDatabase()
        {
            using var db = DB.Open(Path.GetRandomFileName(), new() { CreateIfMissing = true });

            db.Put(WriteOptions.Default, [0x00, 0x00, 0x01], [0x01]);
            db.Put(WriteOptions.Default, [0x00, 0x00, 0x02], [0x02]);
            db.Put(WriteOptions.Default, [0x00, 0x00, 0x03], [0x03]);

            CollectionAssert.AreEqual(new byte[] { 0x01, }, db.Get(ReadOptions.Default, [0x00, 0x00, 0x01]));
            CollectionAssert.AreEqual(new byte[] { 0x02, }, db.Get(ReadOptions.Default, [0x00, 0x00, 0x02]));
            CollectionAssert.AreEqual(new byte[] { 0x03, }, db.Get(ReadOptions.Default, [0x00, 0x00, 0x03]));
        }

        [TestMethod]
        public void TestLevelDbSnapshot()
        {
            using var store = levelDbStore.GetStore(path_leveldb);

            var snapshot = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey, testValue);
            // Data saved to the leveldb snapshot shall not be visible to the store
            Assert.IsNull(snapshot.TryGet(testKey));
            Assert.IsFalse(snapshot.TryGet(testKey, out var got));
            Assert.IsNull(got);

            // Value is in the write batch, not visible to the store and snapshot
            Assert.AreEqual(false, snapshot.Contains(testKey));
            Assert.AreEqual(false, store.Contains(testKey));

            snapshot.Commit();

            // After commit, the data shall be visible to the store but not to the snapshot
            Assert.IsNull(snapshot.TryGet(testKey));
            Assert.IsFalse(snapshot.TryGet(testKey, out got));
            Assert.IsNull(got);

            CollectionAssert.AreEqual(testValue, store.TryGet(testKey));
            Assert.IsTrue(store.TryGet(testKey, out got));
            CollectionAssert.AreEqual(testValue, got);

            Assert.AreEqual(false, snapshot.Contains(testKey));
            Assert.AreEqual(true, store.Contains(testKey));

            snapshot.Dispose();
        }

        [TestMethod]
        public void TestLevelDbMultiSnapshot()
        {
            using var store = levelDbStore.GetStore(path_leveldb);

            var snapshot = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey, testValue);
            snapshot.Commit();
            CollectionAssert.AreEqual(testValue, store.TryGet(testKey));

            var snapshot2 = store.GetSnapshot();

            // Data saved to the leveldb from snapshot1 shall be visible to snapshot2 but not visible to snapshot1
            CollectionAssert.AreEqual(testValue, snapshot2.TryGet(testKey));
            Assert.IsNull(snapshot.TryGet(testKey));

            snapshot.Dispose();
            snapshot2.Dispose();
        }

        [TestMethod]
        public void TestRocksDb()
        {
            TestPersistenceDelete(rocksDBStore.GetStore(path_rocksdb));
            // Test all with the same store

            TestStorage(rocksDBStore.GetStore(path_rocksdb));

            // Test with different storages

            TestPersistenceWrite(rocksDBStore.GetStore(path_rocksdb));
            TestPersistenceRead(rocksDBStore.GetStore(path_rocksdb), true);
            TestPersistenceDelete(rocksDBStore.GetStore(path_rocksdb));
            TestPersistenceRead(rocksDBStore.GetStore(path_rocksdb), false);
        }

        [TestMethod]
        public void TestRocksDbSnapshot()
        {
            using var store = rocksDBStore.GetStore(path_leveldb);

            var snapshot = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey, testValue);
            // Data saved to the leveldb snapshot shall not be visible
            Assert.IsNull(snapshot.TryGet(testKey));
            Assert.IsFalse(snapshot.TryGet(testKey, out var got));
            Assert.IsNull(got);

            Assert.IsNull(store.TryGet(testKey));
            Assert.IsFalse(store.TryGet(testKey, out got));
            Assert.IsNull(got);

            // Value is in the write batch, not visible to the store and snapshot
            Assert.AreEqual(false, snapshot.Contains(testKey));
            Assert.AreEqual(false, store.Contains(testKey));

            snapshot.Commit();

            // After commit, the data shall be visible to the store but not to the snapshot
            Assert.IsNull(snapshot.TryGet(testKey));
            Assert.IsFalse(snapshot.TryGet(testKey, out got));
            Assert.IsNull(got);

            CollectionAssert.AreEqual(testValue, store.TryGet(testKey));
            Assert.IsTrue(store.TryGet(testKey, out got));
            CollectionAssert.AreEqual(testValue, got);

            Assert.AreEqual(false, snapshot.Contains(testKey));
            Assert.AreEqual(true, store.Contains(testKey));

            snapshot.Dispose();
        }

        [TestMethod]
        public void TestRocksDbMultiSnapshot()
        {
            using var store = rocksDBStore.GetStore(path_leveldb);

            var snapshot = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey, testValue);
            snapshot.Commit();
            CollectionAssert.AreEqual(testValue, store.TryGet(testKey));

            var snapshot2 = store.GetSnapshot();
            // Data saved to the leveldb from snapshot1 shall only be visible to snapshot2
            CollectionAssert.AreEqual(testValue, snapshot2.TryGet(testKey));

            snapshot.Dispose();
            snapshot2.Dispose();
        }

        /// <summary>
        /// Test Put/Delete/TryGet/Seek
        /// </summary>
        /// <param name="store">Store</param>
        private void TestStorage(IStore store)
        {
            using (store)
            {
                var key1 = new byte[] { 0x01, 0x02 };
                var value1 = new byte[] { 0x03, 0x04 };

                store.Delete(key1);
                var ret = store.TryGet(key1);
                Assert.IsNull(ret);

                store.Put(key1, value1);
                ret = store.TryGet(key1);
                CollectionAssert.AreEqual(value1, ret);
                Assert.IsTrue(store.Contains(key1));

                ret = store.TryGet(value1);
                Assert.IsNull(ret);
                Assert.IsTrue(store.Contains(key1));

                store.Delete(key1);

                ret = store.TryGet(key1);
                Assert.IsNull(ret);
                Assert.IsFalse(store.Contains(key1));

                // Test seek in order

                store.Put(new byte[] { 0x00, 0x00, 0x04 }, new byte[] { 0x04 });
                store.Put(new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0x00 });
                store.Put(new byte[] { 0x00, 0x00, 0x01 }, new byte[] { 0x01 });
                store.Put(new byte[] { 0x00, 0x00, 0x02 }, new byte[] { 0x02 });
                store.Put(new byte[] { 0x00, 0x00, 0x03 }, new byte[] { 0x03 });

                // Seek Forward

                var entries = store.Seek(new byte[] { 0x00, 0x00, 0x02 }, SeekDirection.Forward).ToArray();
                Assert.AreEqual(3, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x02 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x02 }, entries[0].Value);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x03 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x03 }, entries[1].Value);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x04 }, entries[2].Key);
                CollectionAssert.AreEqual(new byte[] { 0x04 }, entries[2].Value);

                // Seek Backward

                entries = store.Seek(new byte[] { 0x00, 0x00, 0x02 }, SeekDirection.Backward).ToArray();
                Assert.AreEqual(3, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x02 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x02 }, entries[0].Value);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x01 }, entries[1].Value);

                // Seek Backward
                store.Delete(new byte[] { 0x00, 0x00, 0x00 });
                store.Delete(new byte[] { 0x00, 0x00, 0x01 });
                store.Delete(new byte[] { 0x00, 0x00, 0x02 });
                store.Delete(new byte[] { 0x00, 0x00, 0x03 });
                store.Delete(new byte[] { 0x00, 0x00, 0x04 });
                store.Put(new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0x00 });
                store.Put(new byte[] { 0x00, 0x00, 0x01 }, new byte[] { 0x01 });
                store.Put(new byte[] { 0x00, 0x01, 0x02 }, new byte[] { 0x02 });

                entries = store.Seek(new byte[] { 0x00, 0x00, 0x03 }, SeekDirection.Backward).ToArray();
                Assert.AreEqual(2, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x01 }, entries[0].Value);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00 }, entries[1].Value);

                // Seek null
                entries = store.Seek(null, SeekDirection.Forward).ToArray();
                Assert.AreEqual(3, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x01, 0x02 }, entries[2].Key);

                // Seek empty
                entries = store.Seek([], SeekDirection.Forward).ToArray();
                Assert.AreEqual(3, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x01, 0x02 }, entries[2].Key);

                // Test keys with different lengths
                var searchKey = new byte[] { 0x00, 0x01 };
                entries = store.Seek(searchKey, SeekDirection.Backward).ToArray();
                Assert.AreEqual(2, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);

                searchKey = new byte[] { 0x00, 0x01, 0xff, 0xff, 0xff };
                entries = store.Seek(searchKey, SeekDirection.Backward).ToArray();
                Assert.AreEqual(3, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x01, 0x02 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[2].Key);

                // Test Snapshot
                // Note: These tests were added because of `MemorySnapshot`
                using (var snapshot = store.GetSnapshot())
                {
                    // Seek null
                    entries = snapshot.Seek(null, SeekDirection.Backward).ToArray();
                    Assert.AreEqual(0, entries.Length);

                    // Seek empty
                    entries = snapshot.Seek([], SeekDirection.Backward).ToArray();
                    Assert.AreEqual(0, entries.Length);

                    // Seek Backward

                    entries = snapshot.Seek(new byte[] { 0x00, 0x00, 0x02 }, SeekDirection.Backward).ToArray();
                    Assert.AreEqual(2, entries.Length);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                    CollectionAssert.AreEqual(new byte[] { 0x01 }, entries[0].Value);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);
                    CollectionAssert.AreEqual(new byte[] { 0x00 }, entries[1].Value);

                    // Seek Backward
                    snapshot.Delete(new byte[] { 0x00, 0x00, 0x00 });
                    snapshot.Delete(new byte[] { 0x00, 0x00, 0x01 });
                    snapshot.Delete(new byte[] { 0x00, 0x00, 0x02 });
                    snapshot.Delete(new byte[] { 0x00, 0x00, 0x03 });
                    snapshot.Delete(new byte[] { 0x00, 0x00, 0x04 });
                    snapshot.Put(new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0x00 });
                    snapshot.Put(new byte[] { 0x00, 0x00, 0x01 }, new byte[] { 0x01 });
                    snapshot.Put(new byte[] { 0x00, 0x01, 0x02 }, new byte[] { 0x02 });

                    snapshot.Commit();
                }

                using (var snapshot = store.GetSnapshot())
                {
                    entries = snapshot.Seek(new byte[] { 0x00, 0x00, 0x03 }, SeekDirection.Backward).ToArray();
                    Assert.AreEqual(2, entries.Length);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                    CollectionAssert.AreEqual(new byte[] { 0x01 }, entries[0].Value);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);
                    CollectionAssert.AreEqual(new byte[] { 0x00 }, entries[1].Value);

                    // Test keys with different lengths
                    searchKey = new byte[] { 0x00, 0x01 };
                    entries = snapshot.Seek(searchKey, SeekDirection.Backward).ToArray();
                    Assert.AreEqual(2, entries.Length);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);

                    searchKey = new byte[] { 0x00, 0x01, 0xff, 0xff, 0xff };
                    entries = snapshot.Seek(searchKey, SeekDirection.Backward).ToArray();
                    Assert.AreEqual(3, entries.Length);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x01, 0x02 }, entries[0].Key);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[1].Key);
                    CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[2].Key);
                }
            }
        }

        /// <summary>
        /// Test Put
        /// </summary>
        /// <param name="store">Store</param>
        private void TestPersistenceWrite(IStore store)
        {
            using (store)
            {
                store.Put(new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x04, 0x05, 0x06 });
            }
        }

        /// <summary>
        /// Test Put
        /// </summary>
        /// <param name="store">Store</param>
        private void TestPersistenceDelete(IStore store)
        {
            using (store)
            {
                store.Delete(new byte[] { 0x01, 0x02, 0x03 });
            }
        }

        /// <summary>
        /// Test Read
        /// </summary>
        /// <param name="store">Store</param>
        /// <param name="shouldExist">Should exist</param>
        private void TestPersistenceRead(IStore store, bool shouldExist)
        {
            using (store)
            {

                var ret = store.TryGet(new byte[] { 0x01, 0x02, 0x03 });
                if (shouldExist) CollectionAssert.AreEqual(new byte[] { 0x04, 0x05, 0x06 }, ret);
                else Assert.IsNull(ret);
            }
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
