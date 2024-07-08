// Copyright (C) 2015-2024 The Neo Project.
//
// UT_MemoryClonedCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;

namespace Neo.UnitTests.Persistence;

/// <summary>
/// When adding data to `datacache` <see cref="DataCache"/>,
/// it gets passed to `snapshotcache` <see cref="SnapshotCache"/> during commit.
/// If `snapshotcache` <see cref="SnapshotCache"/>commits, the data is then passed
/// to the underlying store <see cref="IStore"/>.
/// However, because snapshots <see cref="ISnapshot"/> are immutable, the new data
/// cannot be retrieved from the snapshot <see cref="ISnapshot"/>.
///
/// When deleting data from `datacache` <see cref="DataCache"/>,
/// it won't exist in `datacache` upon commit, and therefore will be removed from `snapshotcache` <see cref="SnapshotCache"/>.
/// Upon `snapshotcache` <see cref="SnapshotCache"/>commit, the data is deleted from the store <see cref="IStore"/>.
/// However, since the snapshot <see cref="ISnapshot"/> remains unchanged, the data still exists in the snapshot.
/// If you attempt to read this data from `datacache` <see cref="DataCache"/> or `snapshotcache` <see cref="SnapshotCache"/>,
/// which do not have the data, they will retrieve it from the snapshot instead of the store.
/// Thus, they can still access data that has been deleted.
/// </summary>
[TestClass]
public class UT_MemoryClonedCache
{
    private MemoryStore _memoryStore;
    private MemorySnapshot _snapshot;
    private SnapshotCache _snapshotCache;
    private DataCache _dataCache;

    [TestInitialize]
    public void Setup()
    {
        _memoryStore = new MemoryStore();
        _snapshot = _memoryStore.GetSnapshot() as MemorySnapshot;
        _snapshotCache = new SnapshotCache(_snapshot);
        _dataCache = _snapshotCache.CreateSnapshot();
    }

    [TestCleanup]
    public void CleanUp()
    {
        _dataCache.Commit();
        _snapshotCache.Commit();
        _memoryStore.Reset();
    }

    [TestMethod]
    public void SingleSnapshotCacheTest()
    {
        var key1 = new KeyBuilder(0, 1);
        var value1 = new StorageItem([0x03, 0x04]);

        Assert.IsFalse(_dataCache.Contains(key1));
        _dataCache.Add(key1, value1);

        Assert.IsTrue(_dataCache.Contains(key1));
        Assert.IsFalse(_snapshotCache.Contains(key1));
        Assert.IsFalse(_snapshot.Contains(key1.ToArray()));
        Assert.IsFalse(_memoryStore.Contains(key1.ToArray()));

        // After the data cache is committed, it should be dropped
        // so its value after the commit is meaningless and should not be used.
        _dataCache.Commit();

        Assert.IsTrue(_dataCache.Contains(key1));
        Assert.IsTrue(_snapshotCache.Contains(key1));
        Assert.IsFalse(_snapshot.Contains(key1.ToArray()));
        Assert.IsFalse(_memoryStore.Contains(key1.ToArray()));

        // After the snapshot is committed, it should be dropped
        // so its value after the commit is meaningless and should not be used.
        _snapshotCache.Commit();

        Assert.IsTrue(_dataCache.Contains(key1));
        Assert.IsTrue(_snapshotCache.Contains(key1));
        Assert.IsFalse(_snapshot.Contains(key1.ToArray()));
        Assert.IsTrue(_memoryStore.Contains(key1.ToArray()));

        // Test delete

        // Reset the snapshot to make it accessible to the new value.
        _snapshot = _memoryStore.GetSnapshot() as MemorySnapshot;
        _snapshotCache = new SnapshotCache(_snapshot);
        _dataCache = _snapshotCache.CreateSnapshot();

        Assert.IsTrue(_dataCache.Contains(key1));
        _dataCache.Delete(key1);

        Assert.IsFalse(_dataCache.Contains(key1));
        Assert.IsTrue(_snapshotCache.Contains(key1));
        Assert.IsTrue(_snapshot.Contains(key1.ToArray()));
        Assert.IsTrue(_memoryStore.Contains(key1.ToArray()));

        // After the data cache is committed, it should be dropped
        // so its value after the commit is meaningless and should not be used.
        _dataCache.Commit();

        Assert.IsFalse(_dataCache.Contains(key1));
        Assert.IsFalse(_snapshotCache.Contains(key1));
        Assert.IsTrue(_snapshot.Contains(key1.ToArray()));
        Assert.IsTrue(_memoryStore.Contains(key1.ToArray()));


        // After the snapshot cache is committed, it should be dropped
        // so its value after the commit is meaningless and should not be used.
        _snapshotCache.Commit();

        // The reason that datacache, snapshotcache still contains key1 is because
        // they can not find the value from its cache, so they fetch it from the snapshot of the store.
        Assert.IsTrue(_dataCache.Contains(key1));
        Assert.IsTrue(_snapshotCache.Contains(key1));
        Assert.IsTrue(_snapshot.Contains(key1.ToArray()));
        Assert.IsFalse(_memoryStore.Contains(key1.ToArray()));
    }
}
