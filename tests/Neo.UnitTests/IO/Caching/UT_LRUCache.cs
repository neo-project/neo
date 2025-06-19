// Copyright (C) 2015-2025 The Neo Project.
//
// UT_LRUCache.cs file belongs to the neo project and is free
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
    class DemoLRUCache : LRUCache<int, string>
    {
        public DemoLRUCache(int maxCapacity) : base(maxCapacity) { }

        protected override int GetKeyForItem(string item) => item.GetHashCode();
    }

    [TestClass]
    public class UT_LRUCache
    {
        [TestMethod]
        public void TestLRUCache()
        {
            var cache = new DemoLRUCache(3);
            Assert.AreEqual(0, cache.Count);

            var key1 = "1".GetHashCode();
            var key2 = "2".GetHashCode();
            var key3 = "3".GetHashCode();
            var key4 = "4".GetHashCode();
            var key5 = "5".GetHashCode();

            cache.Add("1");
            cache.Add("2");
            cache.Add("3");
            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.Contains(key1));
            Assert.IsTrue(cache.Contains(key2));
            Assert.IsTrue(cache.Contains(key3));
            Assert.IsFalse(cache.Contains(key4));

            var cached = cache[key2];
            Assert.AreEqual("2", cached);
            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.Contains(key1));
            Assert.IsTrue(cache.Contains(key2));
            Assert.IsTrue(cache.Contains(key3));
            Assert.IsFalse(cache.Contains(key4));

            cache.Add("4");
            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.Contains(key3));
            Assert.IsTrue(cache.Contains(key2));
            Assert.IsTrue(cache.Contains(key4));
            Assert.IsFalse(cache.Contains(key1));

            cache.Add("5");
            Assert.AreEqual(3, cache.Count);
            Assert.IsFalse(cache.Contains(key1));
            Assert.IsTrue(cache.Contains(key2));
            Assert.IsFalse(cache.Contains(key3));
            Assert.IsTrue(cache.Contains(key4));
            Assert.IsTrue(cache.Contains(key5));

            cache.Add("6");
            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.Contains(key5));
        }
    }
}
