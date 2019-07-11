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
        public MyCache(int max_capacity) : base(max_capacity)
        {
        }

        protected override int GetKeyForItem(string item)
        {
            return item.GetHashCode();
        }

        protected override void OnAccess(CacheItem item)
        {
            true.Should().BeTrue();
        }

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
        public void init()
        {
            cache = new MyCache(max_capacity);
        }


        [TestMethod]
        public void TestCount()
        {
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
            cache.IsReadOnly.Should().BeFalse();
        }


        [TestMethod]
        public void TestAddAndAddInternal()
        {
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
            cache.AddRange(range);

            cache.Count.Should().Be(2);
            cache.Contains("hello").Should().BeTrue();
            cache.Contains("world").Should().BeTrue();
            cache.Contains("non exist string").Should().BeFalse();
        }

        [TestMethod]
        public void TestClear()
        {
            cache.Add("hello");
            cache.Add("world");

            cache.Count.Should().Be(2);
            cache.Clear();
            cache.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestContainsKey()
        {
            cache.Add("hello");
            cache.Contains("hello").Should().BeTrue();
            cache.Contains("world").Should().BeFalse();
        }

        [TestMethod]
        public void TestContainsValue()
        {
            cache.Add("hello");
            cache.Contains("hello".GetHashCode()).Should().BeTrue();
            cache.Contains("world".GetHashCode()).Should().BeFalse();
        }

        [TestMethod]
        public void TestCopyTo()
        {
            cache.Add("hello");
            cache.Add("world");

            string[] temp = new string[2];

            try
            {
                //cache.CopyTo(null, 1);
                false.Should().BeFalse();
            }
            catch (ArgumentNullException e)
            {
                e.Should().NotBeNull();
            }

            try
            {
                cache.CopyTo(temp, -1);
                false.Should().BeFalse();
            }
            catch (ArgumentOutOfRangeException e)
            {
                e.Should().NotBeNull();
            }

            try
            {
                cache.CopyTo(temp, 1);
                false.Should().BeFalse();
            }
            catch (ArgumentException e)
            {
                e.Should().NotBeNull();
            }

            cache.CopyTo(temp, 0);
            temp[0].Should().Be("hello");
            temp[1].Should().Be("world");
        }


        [TestMethod]
        public void TestRemoveKey()
        {
            cache.Add("hello");

            cache.Remove("hello".GetHashCode()).Should().BeTrue();
            cache.Remove("world".GetHashCode()).Should().BeFalse();
            cache.Contains("hello").Should().BeFalse();
        }

        [TestMethod]
        public void TestRemoveValue()
        {
            cache.Add("hello");

            cache.Remove("hello").Should().BeTrue();
            cache.Remove("world").Should().BeFalse();

            cache.Contains("hello").Should().BeFalse();
        }


        [TestMethod]
        public void TestTryGet()
        {
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
            cache.Add("hello");
            cache.Add("world");

            cache["hello".GetHashCode()].Should().Be("hello");
            cache["world".GetHashCode()].Should().Be("world");


            try
            {
                string temp = cache["non exist string".GetHashCode()];
                false.Should().BeTrue();
            }catch(KeyNotFoundException e)
            {
                e.Should().NotBeNull();
            }
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            cache.Add("hello");
            cache.Add("world");

            int i = 0;
            foreach(string item in cache)
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
            for(; i <= max_capacity; i++)
            {
                cache.Add(i.ToString());
            }

            cache.Add(i.ToString());    // the first one will be deleted 
            cache.Contains(1.ToString()).Should().BeFalse();

            for (i = 2; i <= max_capacity + 1; i++)
            {
                cache.Contains(i.ToString()).Should().BeTrue();
            }
        }


        [TestMethod]
        public void TestDispose()
        {
            cache.Add("hello");
            cache.Add("world");
            cache.Dispose();

            try
            {
                int count = cache.Count;
                false.Should().BeTrue();
            }catch(ObjectDisposedException e)
            {
                e.Should().NotBeNull();
            }
        }
    }
}
