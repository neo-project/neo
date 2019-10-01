using FluentAssertions;
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
        readonly int max_capacity = 4;

        [TestMethod]
        public void TestCount()
        {
            var cache = new MyCache(max_capacity);
            cache.Count.Should().Be(0);

            cache.Add("hello");
            cache.Add("world");
            cache.Count.Should().Be(2);

            cache.Remove("hello");
            cache.Count.Should().Be(1);
        }

        [TestMethod]
        public void TestIsReadOnly()
        {
            var cache = new MyCache(max_capacity);
            cache.IsReadOnly.Should().BeFalse();
        }

        [TestMethod]
        public void TestAddAndAddInternal()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Contains("hello").Should().BeTrue();
            cache.Contains("world").Should().BeFalse();
            cache.Add("hello");
            cache.Count.Should().Be(1);
        }

        [TestMethod]
        public void TestAddRange()
        {
            string[] range = { "hello", "world" };
            var cache = new MyCache(max_capacity);
            cache.AddRange(range);
            cache.Count.Should().Be(2);
            cache.Contains("hello").Should().BeTrue();
            cache.Contains("world").Should().BeTrue();
            cache.Contains("non exist string").Should().BeFalse();
        }

        [TestMethod]
        public void TestClear()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Add("world");
            cache.Count.Should().Be(2);
            cache.Clear();
            cache.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestContainsKey()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Contains("hello").Should().BeTrue();
            cache.Contains("world").Should().BeFalse();
        }

        [TestMethod]
        public void TestContainsValue()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Contains("hello".GetHashCode()).Should().BeTrue();
            cache.Contains("world".GetHashCode()).Should().BeFalse();
        }

        [TestMethod]
        public void TestCopyTo()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Add("world");
            string[] temp = new string[2];

            Action action = () => cache.CopyTo(null, 1);
            action.Should().Throw<ArgumentNullException>();

            action = () => cache.CopyTo(temp, -1);
            action.Should().Throw<ArgumentOutOfRangeException>();

            action = () => cache.CopyTo(temp, 1);
            action.Should().Throw<ArgumentException>();

            cache.CopyTo(temp, 0);
            temp[0].Should().Be("hello");
            temp[1].Should().Be("world");
        }

        [TestMethod]
        public void TestRemoveKey()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Remove("hello".GetHashCode()).Should().BeTrue();
            cache.Remove("world".GetHashCode()).Should().BeFalse();
            cache.Contains("hello").Should().BeFalse();
        }

        [TestMethod]
        public void TestRemoveDisposableKey()
        {
            var entry = new CacheDisposableEntry() { Key = 1 };
            var dcache = new MyDisposableCache(100);
            dcache.Add(entry);

            entry.IsDisposed.Should().BeFalse();
            dcache.Remove(entry.Key).Should().BeTrue();
            dcache.Remove(entry.Key).Should().BeFalse();
            entry.IsDisposed.Should().BeTrue();
        }

        [TestMethod]
        public void TestRemoveValue()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Remove("hello").Should().BeTrue();
            cache.Remove("world").Should().BeFalse();
            cache.Contains("hello").Should().BeFalse();
        }

        [TestMethod]
        public void TestTryGet()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.TryGet("hello".GetHashCode(), out string output).Should().BeTrue();
            output.Should().Be("hello");
            cache.TryGet("world".GetHashCode(), out string output2).Should().BeFalse();
            output2.Should().NotBe("world");
            output2.Should().BeNull();
        }

        [TestMethod]
        public void TestArrayIndexAccess()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Add("world");
            cache["hello".GetHashCode()].Should().Be("hello");
            cache["world".GetHashCode()].Should().Be("world");

            Action action = () =>
            {
                string temp = cache["non exist string".GetHashCode()];
            };
            action.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Add("world");
            int i = 0;
            foreach (string item in cache)
            {
                if (i == 0) item.Should().Be("hello");
                if (i == 1) item.Should().Be("world");
                i++;
            }
            i.Should().Be(2);
            cache.MyGetEnumerator().Should().NotBeNull();
        }

        [TestMethod]
        public void TestOverMaxCapacity()
        {
            int i = 1;
            var cache = new MyCache(max_capacity);
            for (; i <= max_capacity; i++)
            {
                cache.Add(i.ToString());
            }
            cache.Add(i.ToString());    // The first one will be deleted 
            cache.Count.Should().Be(max_capacity);
            cache.Contains((max_capacity + 1).ToString()).Should().BeTrue();
        }

        [TestMethod]
        public void TestDispose()
        {
            var cache = new MyCache(max_capacity);
            cache.Add("hello");
            cache.Add("world");
            cache.Dispose();

            Action action = () =>
            {
                int count = cache.Count;
            };
            action.Should().Throw<ObjectDisposedException>();
        }
    }
}
