using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_CloneCache
    {
        ClonedCache clonedCache;
        MyDataCache myDataCache;

        [TestInitialize]
        public void Init()
        {
            myDataCache = new MyDataCache();
            clonedCache = new ClonedCache(myDataCache);
        }

        [TestMethod]
        public void TestCloneCache()
        {
            clonedCache.Should().NotBeNull();
        }

        [TestMethod]
        public void TestAddInternal()
        {
            clonedCache.Add(new MyKey("key1"), new MyValue("value1"));
            clonedCache[new MyKey("key1")].Should().Be(new MyValue("value1"));

            clonedCache.Commit();
            myDataCache[new MyKey("key1")].Should().Be(new MyValue("value1"));
        }

        [TestMethod]
        public void TestDeleteInternal()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            clonedCache.Delete(new MyKey("key1"));   //  trackable.State = TrackState.Deleted 
            clonedCache.Commit();

            clonedCache.TryGet(new MyKey("key1")).Should().BeNull();
            myDataCache.TryGet(new MyKey("key1")).Should().BeNull();
        }

        [TestMethod]
        public void TestFindInternal()
        {
            clonedCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            var items = clonedCache.Find(new MyKey("key1").ToArray());
            items.ElementAt(0).Key.Should().Be(new MyKey("key1"));
            items.ElementAt(0).Value.Should().Be(new MyValue("value1"));
            items.Count().Should().Be(1);

            items = clonedCache.Find(new MyKey("key2").ToArray());
            items.ElementAt(0).Key.Should().Be(new MyKey("key2"));
            items.ElementAt(0).Value.Should().Be(new MyValue("value2"));
            items.Count().Should().Be(1);

            items = clonedCache.Find(new MyKey("key3").ToArray());
            items.ElementAt(0).Key.Should().Be(new MyKey("key3"));
            items.ElementAt(0).Value.Should().Be(new MyValue("value3"));
            items.Count().Should().Be(1);

            items = clonedCache.Find(new MyKey("key4").ToArray());
            items.Count().Should().Be(0);
        }

        [TestMethod]
        public void TestGetInternal()
        {
            clonedCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            clonedCache[new MyKey("key1")].Should().Be(new MyValue("value1"));
            clonedCache[new MyKey("key2")].Should().Be(new MyValue("value2"));
            clonedCache[new MyKey("key3")].Should().Be(new MyValue("value3"));

            Action action = () =>
            {
                var item = clonedCache[new MyKey("key4")];
            };
            action.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            clonedCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            clonedCache.TryGet(new MyKey("key1")).Should().Be(new MyValue("value1"));
            clonedCache.TryGet(new MyKey("key2")).Should().Be(new MyValue("value2"));
            clonedCache.TryGet(new MyKey("key3")).Should().Be(new MyValue("value3"));
            clonedCache.TryGet(new MyKey("key4")).Should().BeNull();
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            clonedCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            clonedCache.GetAndChange(new MyKey("key1")).Value = Encoding.Default.GetBytes("value_new_1");
            clonedCache.GetAndChange(new MyKey("key2")).Value = Encoding.Default.GetBytes("value_new_2");
            clonedCache.GetAndChange(new MyKey("key3")).Value = Encoding.Default.GetBytes("value_new_3");

            clonedCache.Commit();

            clonedCache[new MyKey("key1")].Should().Be(new MyValue("value_new_1"));
            clonedCache[new MyKey("key2")].Should().Be(new MyValue("value_new_2"));
            clonedCache[new MyKey("key3")].Should().Be(new MyValue("value_new_3"));
            myDataCache[new MyKey("key2")].Should().Be(new MyValue("value_new_2"));
        }
    }
}
