// Copyright (C) 2015-2026 The Neo Project.
//
// UT_FIFOCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_FIFOCache
    {
        private sealed class StringCache(int maxCapacity) : FIFOCache<string, string>(maxCapacity)
        {
            protected override string GetKeyForItem(string item) => item;
        }

        [TestMethod]
        public void Access_DoesNotReorder_FifoEviction()
        {
            var cache = new StringCache(2)
            {
                "a",
                "b"
            };
            // Touch "a" — FIFO OnAccess is no-op, so "a" should still be evicted first
            Assert.IsTrue(cache.TryGet("a", out _));
            cache.Add("c");

            Assert.IsFalse(cache.TryGet("a", out _));
            Assert.IsTrue(cache.TryGet("b", out _));
            Assert.IsTrue(cache.TryGet("c", out _));
            Assert.AreEqual(2, cache.Count);
        }

        [TestMethod]
        public void Add_SameKey_UpdatesValue()
        {
            var cache = new StringCache(2)
            {
                "x"
            };
            Assert.AreEqual(1, cache.Count);
            cache.Add("x");
            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(cache.TryGet("x", out var value));
            Assert.AreEqual("x", value);
        }
    }
}
