// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StoreCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Collections.Caching;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;

namespace Neo.UnitTests.Collections.Caching
{
    [TestClass]
    public class UT_StoreCache
    {

        [TestMethod]
        public void TestAddAndGetSync()
        {
            using var memoryStore = new MemoryStore();
            var storeCache = new StoreCache<StorageKey, StorageItem>(memoryStore);

            var expectedKey = new StorageKey([0, 1, 2, 3, 4, 5, 6]);
            var expectedValue = new StorageItem([7, 8, 9, 0, 11, 12]);

            storeCache[expectedKey] = expectedValue;

            var actualStoreValue = memoryStore.TryGet(expectedKey.ToArray());
            var actualCacheValue = storeCache[expectedKey];

            Assert.IsNotNull(actualStoreValue);
            Assert.IsNotNull(actualCacheValue);

            CollectionAssert.AreEqual(expectedValue.ToArray(), actualStoreValue);
            Assert.AreSame(expectedValue, actualCacheValue);
        }

        [TestMethod]
        public void TestStoreCacheGetNonCachedData()
        {
            using var memoryStore = new MemoryStore();
            var storeCache = new StoreCache<StorageKey, StorageItem>(memoryStore);

            var expectedKey = new StorageKey([0, 1, 2, 3, 4, 5, 6]);
            var expectedValue = new StorageItem([7, 8, 9, 0, 11, 12]);

            memoryStore.Put(expectedKey.ToArray(), expectedValue.ToArray());

            Assert.IsTrue(storeCache.TryGetValue(expectedKey, out _));
            Assert.IsTrue(storeCache.ContainsKey(expectedKey));
            _ = storeCache[expectedKey];
        }
    }
}
