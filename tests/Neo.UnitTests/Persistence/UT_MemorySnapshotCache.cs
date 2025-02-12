// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MemorySnapshotCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_MemorySnapshotCache
    {
        private MemoryStore _memoryStore;
        private MemorySnapshot _snapshot;
        private StoreCache _snapshotCache;

        [TestInitialize]
        public void Setup()
        {
            _memoryStore = new MemoryStore();
            _snapshot = _memoryStore.GetSnapshot() as MemorySnapshot;
            _snapshotCache = new StoreCache(_snapshot);
        }

        [TestCleanup]
        public void CleanUp()
        {
            _snapshotCache.Commit();
            _memoryStore.Reset();
        }

        [TestMethod]
        public void SingleSnapshotCacheTest()
        {
            var key1 = new KeyBuilder(0, 1);
            var value1 = new StorageItem([0x03, 0x04]);

            _snapshotCache.Delete(key1);
            Assert.IsFalse(_snapshotCache.TryGet(key1, out var ret));
            Assert.IsNull(ret);

            // Adding value to the snapshot cache will not affect the snapshot or the store
            // But the snapshot cache itself can see the added item right after it is added.
            _snapshotCache.Add(key1, value1);

            Assert.IsTrue(_snapshotCache.TryGet(key1, out ret));
            Assert.AreEqual(value1.Value, ret.Value);
            Assert.IsNull(_snapshot.TryGet(key1.ToArray()));
            Assert.IsNull(_memoryStore.TryGet(key1.ToArray()));

            // After commit the snapshot cache, it works the same as commit the snapshot.
            // the value can be get from the snapshot cache and store but still can not get from the snapshot
            _snapshotCache.Commit();

            Assert.IsTrue(_snapshotCache.TryGet(key1, out ret));
            Assert.AreEqual(value1.Value, ret.Value);
            Assert.IsFalse(_snapshot.Contains(key1.ToArray()));
            Assert.IsTrue(_memoryStore.Contains(key1.ToArray()));

            // Test delete

            // Reset the snapshot to make it accessible to the new value.
            _snapshot = _memoryStore.GetSnapshot() as MemorySnapshot;
            _snapshotCache = new StoreCache(_snapshot);

            // Delete value to the snapshot cache will not affect the snapshot or the store
            // But the snapshot cache itself can not see the added item.
            _snapshotCache.Delete(key1);

            // Value is removed from the snapshot cache immediately
            Assert.IsFalse(_snapshotCache.TryGet(key1, out ret));
            Assert.IsNull(ret);
            // But the underline snapshot will not be changed.
            Assert.IsTrue(_snapshot.Contains(key1.ToArray()));
            // And the store is also not affected.
            Assert.IsNotNull(_memoryStore.TryGet(key1.ToArray()));

            // commit the snapshot cache
            _snapshotCache.Commit();

            // Value is removed from both the store, but the snapshot and snapshot cache remains the same.
            Assert.IsTrue(_snapshotCache.Contains(key1));
            Assert.IsTrue(_snapshot.Contains(key1.ToArray()));
            Assert.IsFalse(_memoryStore.Contains(key1.ToArray()));
        }

        [TestMethod]
        public void MultiSnapshotCacheTest()
        {
            var key1 = new KeyBuilder(0, 1);
            var value1 = new StorageItem([0x03, 0x04]);

            _snapshotCache.Delete(key1);
            Assert.IsFalse(_snapshotCache.TryGet(key1, out var ret));
            Assert.IsNull(ret);

            // Adding value to the snapshot cache will not affect the snapshot or the store
            // But the snapshot cache itself can see the added item.
            _snapshotCache.Add(key1, value1);

            // After commit the snapshot cache, it works the same as commit the snapshot.
            // the value can be get from the snapshot cache but still can not get from the snapshot
            _snapshotCache.Commit();

            // Get a new snapshot cache to test if the value can be seen from the new snapshot cache
            var snapshotCache2 = new StoreCache(_snapshot);
            Assert.IsFalse(snapshotCache2.TryGet(key1, out ret));
            Assert.IsNull(ret);
            Assert.IsFalse(_snapshot.Contains(key1.ToArray()));

            // Test delete

            // Reset the snapshot to make it accessible to the new value.
            _snapshot = _memoryStore.GetSnapshot() as MemorySnapshot;
            _snapshotCache = new StoreCache(_snapshot);

            // Delete value to the snapshot cache will affect the snapshot
            // But the snapshot and store itself can still see the item.
            _snapshotCache.Delete(key1);

            // Commiting the snapshot cache will change the store, but the existing snapshot remains same.
            _snapshotCache.Commit();

            // reset the snapshotcache2 to snapshot
            snapshotCache2 = new StoreCache(_snapshot);
            // Value is removed from the store, but the snapshot remains the same.
            // thus the snapshot cache from the snapshot will remain the same.
            Assert.IsTrue(snapshotCache2.TryGet(key1, out ret));
            Assert.IsNotNull(ret);
            Assert.IsNull(_memoryStore.TryGet(key1.ToArray()));
        }
    }
}
