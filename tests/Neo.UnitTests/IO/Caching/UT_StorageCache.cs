// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StorageCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO.Caching;
using Neo.Persistence;
using Neo.SmartContract;
using System.Linq;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_StorageCache
    {
        [TestMethod]
        public void TestStorageCacheAddAndUpdate()
        {
            using var memoryStore = new MemoryStore();
            using var storeCache = new StorageCache<StorageKey, StorageItem>(memoryStore);

            var expectedKey = new StorageKey([0, 1, 2, 3, 4, 5, 6]);
            var expectedAddValue = new StorageItem([7, 8, 9, 0, 11, 12]);
            var expectedUpdateValue = new StorageItem([13, 14, 15, 16, 17, 18]);

            storeCache.AddOrUpdate(expectedKey, expectedAddValue);

            var actualStoreAddValue = memoryStore.TryGet(expectedKey.ToArray());
            var actualCacheAddValueRet = storeCache.TryGetValue(expectedKey, out var actualCacheAddValue);

            Assert.IsNotNull(actualStoreAddValue);
            CollectionAssert.AreEqual(expectedAddValue.ToArray(), actualStoreAddValue);

            Assert.IsTrue(actualCacheAddValueRet);
            Assert.IsNotNull(actualCacheAddValue);
            Assert.AreSame(expectedAddValue, actualCacheAddValue);

            storeCache.AddOrUpdate(expectedKey, expectedUpdateValue);

            var actualStoreUpdateValue = memoryStore.TryGet(expectedKey.ToArray());
            var actualCacheUpdateValueRet = storeCache.TryGetValue(expectedKey, out var actualCacheUpdateValue);

            Assert.IsNotNull(actualStoreUpdateValue);
            CollectionAssert.AreEqual(expectedUpdateValue.ToArray(), actualStoreUpdateValue);

            Assert.IsTrue(actualCacheUpdateValueRet);
            Assert.IsNotNull(actualCacheUpdateValue);
            Assert.AreSame(expectedUpdateValue, actualCacheUpdateValue);
        }

        [TestMethod]
        public void TestStorageCacheRemove()
        {
            using var memoryStore = new MemoryStore();
            using var storeCache = new StorageCache<StorageKey, StorageItem>(memoryStore);

            var expectedKey = new StorageKey([0, 1, 2, 3, 4, 5, 6]);
            var expectedAddValue = new StorageItem([7, 8, 9, 0, 11, 12]);

            storeCache.AddOrUpdate(expectedKey, expectedAddValue);
            storeCache.Remove(expectedKey);

            Assert.AreEqual(0, storeCache.Size);

            var actualStoreAddValue = memoryStore.TryGet(expectedKey.ToArray());
            var actualCacheAddValueRet = storeCache.TryGetValue(expectedKey, out var actualCacheAddValue);

            Assert.IsNotNull(actualStoreAddValue);
            CollectionAssert.AreEqual(expectedAddValue.ToArray(), actualStoreAddValue);

            Assert.IsTrue(actualCacheAddValueRet);
            Assert.IsNotNull(actualCacheAddValue);
            // NOTE: that when you remove from cache. `StorageCache` class has to get fetch from
            // `IStore`. Making the instance of `TValue` different.
            CollectionAssert.AreEqual(expectedAddValue.ToArray(), actualCacheAddValue.ToArray());
        }

        [TestMethod]
        public void TestStorageCacheDelete()
        {
            using var memoryStore = new MemoryStore();
            using var storeCache = new StorageCache<StorageKey, StorageItem>(memoryStore);

            var expectedKey = new StorageKey([0, 1, 2, 3, 4, 5, 6]);
            var expectedAddValue = new StorageItem([7, 8, 9, 0, 11, 12]);

            storeCache.AddOrUpdate(expectedKey, expectedAddValue);
            storeCache.Delete(expectedKey);

            Assert.AreEqual(0, storeCache.Size);

            var actualStoreAddValue = memoryStore.TryGet(expectedKey.ToArray());
            var actualCacheAddValueRet = storeCache.TryGetValue(expectedKey, out var actualCacheAddValue);

            Assert.IsNull(actualStoreAddValue);

            Assert.IsFalse(actualCacheAddValueRet);
            Assert.IsNull(actualCacheAddValue);
        }
    }
}
