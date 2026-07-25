// Copyright (C) 2015-2026 The Neo Project.
//
// UT_StoreFactory.cs file belongs to the neo project and is free
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
using System;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_StoreFactory
    {
        [TestMethod]
        public void GetStoreProvider_MemoryStore_IsRegistered()
        {
            var provider = StoreFactory.GetStoreProvider(nameof(MemoryStore));
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType<MemoryStoreProvider>(provider);
            Assert.AreEqual(nameof(MemoryStore), provider.Name);
        }

        [TestMethod]
        public void GetStoreProvider_EmptyName_ReturnsDefaultMemoryProvider()
        {
            var provider = StoreFactory.GetStoreProvider("");
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType<MemoryStoreProvider>(provider);
        }

        [TestMethod]
        public void GetStoreProvider_Unknown_ReturnsNull()
        {
            Assert.IsNull(StoreFactory.GetStoreProvider("does-not-exist-provider"));
        }

        [TestMethod]
        public void GetStore_CreatesUsableMemoryStore()
        {
            using var store = StoreFactory.GetStore(nameof(MemoryStore), "ignored");
            Assert.IsInstanceOfType<MemoryStore>(store);
            store.Put([1], [2]);
            Assert.IsTrue(store.TryGet([1], out var value));
            Assert.IsTrue(new byte[] { 2 }.AsSpan().SequenceEqual(value));
        }
    }
}
