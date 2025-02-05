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
using Neo.Extensions;
using Neo.Persistence;
using System.IO;
using System.Linq;

namespace Neo.Plugins.Storage.Tests
{
    [TestClass]
    public class StoreTest
    {
        private const string Path_leveldb = "Data_LevelDB_UT";
        private const string Path_rocksdb = "Data_RocksDB_UT";
        private static LevelDBStore s_levelDbStore;
        private static RocksDBStore s_rocksDBStore;

        [AssemblyInitialize]
        public static void OnStart(TestContext testContext)
        {
            OnEnd();
            s_levelDbStore = new LevelDBStore();
            s_rocksDBStore = new RocksDBStore();
        }

        [AssemblyCleanup]
        public static void OnEnd()
        {
            s_levelDbStore?.Dispose();
            s_rocksDBStore?.Dispose();

            if (Directory.Exists(Path_leveldb)) Directory.Delete(Path_leveldb, true);
            if (Directory.Exists(Path_rocksdb)) Directory.Delete(Path_rocksdb, true);
        }

        #region Tests

        [TestMethod]
        public void TestMemory()
        {
            using var store = new MemoryStore();
            // Test all with the same store

            TestStorage(store);

            // Test with different storages

            TestPersistence(store);

            // Test snapshot

            TestSnapshot(store);
            TestMultiSnapshot(store);
        }

        [TestMethod]
        public void TestLevelDb()
        {
            using var store = s_levelDbStore.GetStore(Path_leveldb);

            // Test all with the same store

            TestStorage(store);

            // Test with different storages

            TestPersistence(store);

            // Test snapshot

            TestSnapshot(store);
            TestMultiSnapshot(store);
        }

        [TestMethod]
        public void TestRocksDb()
        {
            using var store = s_rocksDBStore.GetStore(Path_rocksdb);

            // Test all with the same store

            TestStorage(store);

            // Test with different storages

            TestPersistence(store);

            // Test snapshot

            TestSnapshot(store);
            TestMultiSnapshot(store);
        }

        #endregion

        public static void TestSnapshot(IStore store)
        {
            var snapshot = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey, testValue);
            // Data saved to the leveldb snapshot shall not be visible to the store
            Assert.IsNull(snapshot.TryGet(testKey));
            Assert.IsFalse(snapshot.TryGet(testKey, out var got));
            Assert.IsNull(got);

            // Value is in the write batch, not visible to the store and snapshot
            Assert.IsFalse(snapshot.Contains(testKey));
            Assert.IsFalse(store.Contains(testKey));

            snapshot.Commit();

            // After commit, the data shall be visible to the store but not to the snapshot
            Assert.IsNull(snapshot.TryGet(testKey));
            Assert.IsFalse(snapshot.TryGet(testKey, out got));
            Assert.IsNull(got);

            CollectionAssert.AreEqual(testValue, store.TryGet(testKey));
            Assert.IsTrue(store.TryGet(testKey, out got));
            CollectionAssert.AreEqual(testValue, got);

            Assert.IsFalse(snapshot.Contains(testKey));
            Assert.IsTrue(store.Contains(testKey));

            snapshot.Dispose();
        }

        public static void TestMultiSnapshot(IStore store)
        {
            using var snapshot = store.GetSnapshot();

            var testKey = new byte[] { 0x01, 0x02, 0x03 };
            var testValue = new byte[] { 0x04, 0x05, 0x06 };

            snapshot.Put(testKey, testValue);
            snapshot.Commit();
            CollectionAssert.AreEqual(testValue, store.TryGet(testKey));

            using var snapshot2 = store.GetSnapshot();

            // Data saved to the leveldb from snapshot1 shall only be visible to snapshot2
            Assert.IsTrue(snapshot2.TryGet(testKey, out var ret));
            CollectionAssert.AreEqual(testValue, ret);
        }

        /// <summary>
        /// Test Put/Delete/TryGet/Seek
        /// </summary>
        /// <param name="store">Store</param>
        private static void TestStorage(IStore store)
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

            store.Put([0x00, 0x00, 0x04], [0x04]);
            store.Put([0x00, 0x00, 0x00], [0x00]);
            store.Put([0x00, 0x00, 0x01], [0x01]);
            store.Put([0x00, 0x00, 0x02], [0x02]);
            store.Put([0x00, 0x00, 0x03], [0x03]);

            // Seek Forward

            var entries = store.Seek([0x00, 0x00, 0x02], SeekDirection.Forward).ToArray();
            Assert.AreEqual(3, entries.Length);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x02 }, entries[0].Key);
            CollectionAssert.AreEqual(new byte[] { 0x02 }, entries[0].Value);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x03 }, entries[1].Key);
            CollectionAssert.AreEqual(new byte[] { 0x03 }, entries[1].Value);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x04 }, entries[2].Key);
            CollectionAssert.AreEqual(new byte[] { 0x04 }, entries[2].Value);

            // Seek Backward

            entries = store.Seek([0x00, 0x00, 0x02], SeekDirection.Backward).ToArray();
            Assert.AreEqual(3, entries.Length);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x02 }, entries[0].Key);
            CollectionAssert.AreEqual(new byte[] { 0x02 }, entries[0].Value);
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[1].Key);
            CollectionAssert.AreEqual(new byte[] { 0x01 }, entries[1].Value);

            // Seek Backward
            store.Delete([0x00, 0x00, 0x00]);
            store.Delete([0x00, 0x00, 0x01]);
            store.Delete([0x00, 0x00, 0x02]);
            store.Delete([0x00, 0x00, 0x03]);
            store.Delete([0x00, 0x00, 0x04]);
            store.Put([0x00, 0x00, 0x00], [0x00]);
            store.Put([0x00, 0x00, 0x01], [0x01]);
            store.Put([0x00, 0x01, 0x02], [0x02]);

            entries = store.Seek([0x00, 0x00, 0x03], SeekDirection.Backward).ToArray();
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

            searchKey = [0x00, 0x01, 0xff, 0xff, 0xff];
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

                entries = snapshot.Seek([0x00, 0x00, 0x02], SeekDirection.Backward).ToArray();
                Assert.AreEqual(2, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x01 }, entries[0].Value);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00 }, entries[1].Value);

                // Seek Backward
                snapshot.Delete([0x00, 0x00, 0x00]);
                snapshot.Delete([0x00, 0x00, 0x01]);
                snapshot.Delete([0x00, 0x00, 0x02]);
                snapshot.Delete([0x00, 0x00, 0x03]);
                snapshot.Delete([0x00, 0x00, 0x04]);
                snapshot.Put([0x00, 0x00, 0x00], [0x00]);
                snapshot.Put([0x00, 0x00, 0x01], [0x01]);
                snapshot.Put([0x00, 0x01, 0x02], [0x02]);

                snapshot.Commit();
            }

            using (var snapshot = store.GetSnapshot())
            {
                entries = snapshot.Seek([0x00, 0x00, 0x03], SeekDirection.Backward).ToArray();
                Assert.AreEqual(2, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x01 }, entries[0].Value);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00 }, entries[1].Value);

                // Test keys with different lengths
                searchKey = [0x00, 0x01];
                entries = snapshot.Seek(searchKey, SeekDirection.Backward).ToArray();
                Assert.AreEqual(2, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[1].Key);

                searchKey = [0x00, 0x01, 0xff, 0xff, 0xff];
                entries = snapshot.Seek(searchKey, SeekDirection.Backward).ToArray();
                Assert.AreEqual(3, entries.Length);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x01, 0x02 }, entries[0].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x01 }, entries[1].Key);
                CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00 }, entries[2].Key);
            }
        }

        /// <summary>
        /// Test Put
        /// </summary>
        /// <param name="store">Store</param>
        private static void TestPersistence(IStore store)
        {
            store.Put([0x01, 0x02, 0x03], [0x04, 0x05, 0x06]);

            var ret = store.TryGet([0x01, 0x02, 0x03], out var retvalue);
            Assert.IsTrue(ret);
            CollectionAssert.AreEqual(new byte[] { 0x04, 0x05, 0x06 }, retvalue);

            store.Delete([0x01, 0x02, 0x03]);

            ret = store.TryGet([0x01, 0x02, 0x03], out retvalue);
            Assert.IsFalse(ret);
            Assert.IsNull(retvalue);
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
