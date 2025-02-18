// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ReadOnlyStore.cs file belongs to the neo project and is free
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
    public class UT_ReadOnlyStore
    {
        [TestMethod]
        public void TestError()
        {
            var store = new MemoryStore();
            var snapshot = new StoreCache(store);

            Assert.ThrowsException<InvalidOperationException>(() => snapshot.GetChangeSet().ToArray());
            Assert.ThrowsException<InvalidOperationException>(snapshot.Commit);

            // No errors

            snapshot = new StoreCache(store.GetSnapshot());
            _ = snapshot.GetChangeSet().ToArray();
            snapshot.Commit();
        }

        [TestMethod]
        public void TestReadOnlyStoreView()
        {
            var store = new MemoryStore();
            var key = new KeyBuilder(1, 2).Add(new UInt160()).ToArray();
            var view = (IReadOnlyStore<byte[], byte[]>)store;

            // Test Contains
            Assert.IsFalse(view.Contains(key));

            // Test TryGet
            Assert.IsFalse(view.TryGet(key, out var item));

            // Test this[]
            Assert.ThrowsException<KeyNotFoundException>(() => view[key]);

            // Test Put
            var value = new byte[] { 1, 2, 3 };
            store.Put(key, value);

            // Test Contains
            Assert.IsTrue(view.Contains(key));

            // Test this[]
            CollectionAssert.AreEqual(value, view[key]);

            // Test TryGet
            Assert.IsTrue(view.TryGet(key, out item));
            CollectionAssert.AreEqual(value, item);
        }
    }
}
