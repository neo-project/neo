// Copyright (C) 2015-2026 The Neo Project.
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
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_StoreCache
    {
        private static byte[] MakeKey(int id, params byte[] suffix)
        {
            var key = new byte[sizeof(int) + suffix.Length];
            BitConverter.TryWriteBytes(key.AsSpan(0, sizeof(int)), id);
            suffix.CopyTo(key.AsSpan(sizeof(int)));
            return key;
        }

        [TestMethod]
        public void ReadOnly_Store_TryGet_And_Contains()
        {
            using var store = new MemoryStore();
            var keyBytes = MakeKey(0, 1, 2, 3);
            var valueBytes = new byte[] { 9, 8 };
            store.Put(keyBytes, valueBytes);

            using var cache = new StoreCache(store, readOnly: true);
            StorageKey key = keyBytes;
            Assert.IsTrue(cache.Contains(key));
            Assert.IsTrue(cache.TryGet(key, out var item));
            Assert.IsTrue(valueBytes.AsSpan().SequenceEqual(item.Value.Span));
            Assert.IsTrue(valueBytes.AsSpan().SequenceEqual(cache[key].Value.Span));
        }

        [TestMethod]
        public void Get_MissingKey_Throws()
        {
            using var store = new MemoryStore();
            using var cache = new StoreCache(store);
            StorageKey missing = MakeKey(0, 0xAB);
            Assert.ThrowsExactly<KeyNotFoundException>(() => _ = cache[missing]);
        }

        [TestMethod]
        public void Find_ReturnsMatchingEntries()
        {
            using var store = new MemoryStore();
            store.Put(MakeKey(1, 0), [1]);
            store.Put(MakeKey(1, 1), [2]);
            store.Put(MakeKey(2, 0), [3]);

            using var cache = new StoreCache(store);
            var prefix = MakeKey(1);
            var found = cache.Find(prefix, SeekDirection.Forward).ToArray();
            Assert.HasCount(2, found);
        }

        [TestMethod]
        public void Dispose_IsSafeForStoreConstructor()
        {
            using var store = new MemoryStore();
            var cache = new StoreCache(store);
            cache.Dispose();
        }
    }
}
