// Copyright (C) 2015-2026 The Neo Project.
//
// UT_MemoryStoreProvider.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_MemoryStoreProvider
    {
        [TestMethod]
        public void Name_IsMemoryStore()
        {
            var provider = new MemoryStoreProvider();
            Assert.AreEqual(nameof(MemoryStore), provider.Name);
        }

        [TestMethod]
        public void GetStore_ReturnsIndependentStores()
        {
            var provider = new MemoryStoreProvider();
            using var store1 = provider.GetStore(null);
            using var store2 = provider.GetStore("ignored");

            Assert.IsInstanceOfType<MemoryStore>(store1);
            Assert.IsInstanceOfType<MemoryStore>(store2);
            Assert.AreNotSame(store1, store2);

            store1.Put([1], [2]);
            Assert.IsFalse(store2.TryGet([1], out _));
        }
    }
}
