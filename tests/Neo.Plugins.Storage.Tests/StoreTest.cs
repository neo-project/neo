// Copyright (C) 2015-2024 The Neo Project.
//
// StoreTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
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

        [TestInitialize]
        public void OnStart()
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
            using var plugin = new LevelDBStore();
            TestPersistenceDelete(plugin.GetStore(path_leveldb));
            // Test all with the same store

            TestStorage(plugin.GetStore(path_leveldb));

            // Test with different storages

            TestPersistenceWrite(plugin.GetStore(path_leveldb));
            TestPersistenceRead(plugin.GetStore(path_leveldb), true);
            TestPersistenceDelete(plugin.GetStore(path_leveldb));
            TestPersistenceRead(plugin.GetStore(path_leveldb), false);
        }

        [TestMethod]
        public void TestLevelDbSnapshot()
        {
            using var plugin = new LevelDBStore();
            using var store = plugin.GetStore(path_leveldb);

            var snapshot = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey,testValue);
            // Data saved to the leveldb snapshot shall not be visible to the store
            Assert.IsNull(snapshot.TryGet(testKey));
            snapshot.Commit();

            // After commit, the data shall be visible to the store and to the snapshot
            CollectionAssert.AreEqual(testValue, snapshot.TryGet(testKey));
            var b = store.TryGet(testKey);
            CollectionAssert.AreEqual(testValue,b );

            snapshot.Dispose();
        }

        [TestMethod]
        public void TestLevelDbMultiSnapshot()
        {
            using var plugin = new LevelDBStore();
            using var store = plugin.GetStore(path_leveldb);

            var snapshot = store.GetSnapshot();
            var snapshot2 = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey,testValue);
            CollectionAssert.AreEqual(testValue, snapshot.TryGet(testKey));
            snapshot.Commit();
            CollectionAssert.AreEqual(testValue, store.TryGet(testKey));

            var ret = snapshot2.TryGet(testKey);
            Assert.IsNull(ret);

            snapshot.Dispose();
            snapshot2.Dispose();
        }

        [TestMethod]
        public void TestRocksDb()
        {
            using var plugin = new RocksDBStore();
            TestPersistenceDelete(plugin.GetStore(path_rocksdb));
            // Test all with the same store

            TestStorage(plugin.GetStore(path_rocksdb));

            // Test with different storages

            TestPersistenceWrite(plugin.GetStore(path_rocksdb));
            TestPersistenceRead(plugin.GetStore(path_rocksdb), true);
            TestPersistenceDelete(plugin.GetStore(path_rocksdb));
            TestPersistenceRead(plugin.GetStore(path_rocksdb), false);
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
