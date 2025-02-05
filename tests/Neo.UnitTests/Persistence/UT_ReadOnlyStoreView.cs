// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ReadOnlyStoreView.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using System.Collections.Generic;

namespace Neo.UnitTests.Persistence
{
    public class UT_ReadOnlyStoreView
    {
        [TestMethod]
        public void TestReadOnlyStoreView()
        {
            var store = new MemoryStore();
            var key = new KeyBuilder(1, 2).Add(new UInt160()).ToArray();
            var view = new ReadOnlyStoreView(store);

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
            CollectionAssert.AreEqual(value, view[key].Value.ToArray());

            // Test TryGet
            Assert.IsTrue(view.TryGet(key, out item));
            CollectionAssert.AreEqual(value, item.Value.ToArray());
        }
    }
}
