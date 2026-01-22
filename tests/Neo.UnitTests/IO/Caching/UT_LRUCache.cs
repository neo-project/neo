// Copyright (C) 2015-2026 The Neo Project.
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
            Assert.IsEmpty(cache);

            var key1 = "1".GetHashCode();
            var key2 = "2".GetHashCode();
            var key3 = "3".GetHashCode();
            var key4 = "4".GetHashCode();
            var key5 = "5".GetHashCode();

            cache.Add("1");
            cache.Add("2");
            cache.Add("3");
            Assert.HasCount(3, cache);
            Assert.Contains("1", cache);
            Assert.Contains("2", cache);
            Assert.Contains("3", cache);
            Assert.DoesNotContain("4", cache);

            var cached = cache[key2];
            Assert.AreEqual("2", cached);
            Assert.HasCount(3, cache);
            Assert.Contains("1", cache);
            Assert.Contains("2", cache);
            Assert.Contains("3", cache);
            Assert.DoesNotContain("4", cache);

            cache.Add("4");
            Assert.HasCount(3, cache);
            Assert.Contains("3", cache);
            Assert.Contains("2", cache);
            Assert.Contains("4", cache);
            Assert.DoesNotContain("1", cache);

            cache.Add("5");
            Assert.HasCount(3, cache);
            Assert.DoesNotContain("1", cache);
            Assert.Contains("2", cache);
            Assert.DoesNotContain("3", cache);
            Assert.Contains("4", cache);
            Assert.Contains("5", cache);

            cache.Add("6");
            Assert.HasCount(3, cache);
            Assert.Contains("5", cache);
        }
    }
}
