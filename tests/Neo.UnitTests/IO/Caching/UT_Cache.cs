// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Cache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Neo.UnitTests.IO.Caching
{
    class MyCache : Cache<int, string>
    {
        public MyCache(int max_capacity) : base(max_capacity) { }

        protected override int GetKeyForItem(string item)
        {
            return item.GetHashCode();
        }

        protected override void OnAccess(CacheItem item) { }

        public IEnumerator MyGetEnumerator()
        {
            IEnumerable enumerable = this;
            return enumerable.GetEnumerator();
        }
    }

    class CacheDisposableEntry : IDisposable
    {
        public int Key { get; set; }
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    class MyDisposableCache : Cache<int, CacheDisposableEntry>
    {
        public MyDisposableCache(int max_capacity) : base(max_capacity) { }

        protected override int GetKeyForItem(CacheDisposableEntry item)
        {
            return item.Key;
        }

        protected override void OnAccess(CacheItem item) { }

        public IEnumerator MyGetEnumerator()
        {
            IEnumerable enumerable = this;
            return enumerable.GetEnumerator();
        }
    }

    [TestClass]
    public class UT_Cache
    {
        MyCache cache;
        readonly int max_capacity = 4;

        [TestInitialize]
        public void Init()
        {
            cache = new MyCache(max_capacity);
        }

        [TestMethod]
        public void TestCount()
        {
            Assert.AreEqual(0, cache.Count);

            cache.Add("hello");
            cache.Add("world");
            Assert.AreEqual(2, cache.Count);

            cache.Remove("hello");
            Assert.AreEqual(1, cache.Count);
        }

        [TestMethod]
        public void TestIsReadOnly()
        {
            Assert.IsFalse(cache.IsReadOnly);
        }

        [TestMethod]
        public void TestAddAndAddInternal()
        {
            cache.Add("hello");
            Assert.IsTrue(cache.Contains("hello"));
            Assert.IsFalse(cache.Contains("world"));
            cache.Add("hello");
            Assert.AreEqual(1, cache.Count);
        }

        [TestMethod]
        public void TestAddRange()
        {
            string[] range = { "hello", "world" };
            cache.AddRange(range);
            Assert.AreEqual(2, cache.Count);
            Assert.IsTrue(cache.Contains("hello"));
            Assert.IsTrue(cache.Contains("world"));
            Assert.IsFalse(cache.Contains("non exist string"));
        }

        [TestMethod]
        public void TestClear()
        {
            cache.Add("hello");
            cache.Add("world");
            Assert.AreEqual(2, cache.Count);
            cache.Clear();
            Assert.AreEqual(0, cache.Count);
        }

        [TestMethod]
        public void TestContainsKey()
        {
            cache.Add("hello");
            Assert.IsTrue(cache.Contains("hello"));
            Assert.IsFalse(cache.Contains("world"));
        }

        [TestMethod]
        public void TestContainsValue()
        {
            cache.Add("hello");
            Assert.IsTrue(cache.Contains("hello".GetHashCode()));
            Assert.IsFalse(cache.Contains("world".GetHashCode()));
        }

        [TestMethod]
        public void TestCopyTo()
        {
            cache.Add("hello");
            cache.Add("world");
            string[] temp = new string[2];

            Action action = () => cache.CopyTo(null, 1);
            Assert.ThrowsException<ArgumentNullException>(() => action());

            action = () => cache.CopyTo(temp, -1);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => action());

            action = () => cache.CopyTo(temp, 1);
            Assert.ThrowsException<ArgumentException>(() => action());

            cache.CopyTo(temp, 0);
            Assert.AreEqual("hello", temp[0]);
            Assert.AreEqual("world", temp[1]);
        }

        [TestMethod]
        public void TestRemoveKey()
        {
            cache.Add("hello");
            Assert.IsTrue(cache.Remove("hello".GetHashCode()));
            Assert.IsFalse(cache.Remove("world".GetHashCode()));
            Assert.IsFalse(cache.Contains("hello"));
        }

        [TestMethod]
        public void TestRemoveDisposableKey()
        {
            var entry = new CacheDisposableEntry() { Key = 1 };
            var dcache = new MyDisposableCache(100)
            {
                entry
            };

            Assert.IsFalse(entry.IsDisposed);
            Assert.IsTrue(dcache.Remove(entry.Key));
            Assert.IsFalse(dcache.Remove(entry.Key));
            Assert.IsTrue(entry.IsDisposed);
        }

        [TestMethod]
        public void TestRemoveValue()
        {
            cache.Add("hello");
            Assert.IsTrue(cache.Remove("hello"));
            Assert.IsFalse(cache.Remove("world"));
            Assert.IsFalse(cache.Contains("hello"));
        }

        [TestMethod]
        public void TestTryGet()
        {
            cache.Add("hello");
            Assert.IsTrue(cache.TryGet("hello".GetHashCode(), out string output));
            Assert.AreEqual("hello", output);
            Assert.IsFalse(cache.TryGet("world".GetHashCode(), out string output2));
            Assert.IsNull(output2);
        }

        [TestMethod]
        public void TestArrayIndexAccess()
        {
            cache.Add("hello");
            cache.Add("world");
            Assert.AreEqual("hello", cache["hello".GetHashCode()]);
            Assert.AreEqual("world", cache["world".GetHashCode()]);

            Action action = () =>
            {
                string temp = cache["non exist string".GetHashCode()];
            };
            Assert.ThrowsException<KeyNotFoundException>(() => action());
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            cache.Add("hello");
            cache.Add("world");
            int i = 0;
            foreach (string item in cache)
            {
                if (i == 0) Assert.AreEqual("hello", item);
                if (i == 1) Assert.AreEqual("world", item);
                i++;
            }
            Assert.AreEqual(2, i);
            Assert.IsNotNull(cache.MyGetEnumerator());
        }

        [TestMethod]
        public void TestOverMaxCapacity()
        {
            int i = 1;
            for (; i <= max_capacity; i++)
            {
                cache.Add(i.ToString());
            }
            cache.Add(i.ToString());    // The first one will be deleted
            Assert.AreEqual(max_capacity, cache.Count);
            Assert.IsTrue(cache.Contains((max_capacity + 1).ToString()));
        }

        [TestMethod]
        public void TestDispose()
        {
            cache.Add("hello");
            cache.Add("world");
            cache.Dispose();

            Action action = () =>
            {
                int count = cache.Count;
            };
            Assert.ThrowsException<ObjectDisposedException>(() => action());
        }
    }
}
