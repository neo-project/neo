using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_CloneCache
    {
        CloneCache<MyKey, MyValue> cloneCache;
        MyDataCache<MyKey, MyValue> myDataCache;

        [TestInitialize]
        public void Init()
        {
            myDataCache = new MyDataCache<MyKey, MyValue>();
            cloneCache = new CloneCache<MyKey, MyValue>(myDataCache);
        }

        [TestMethod]
        public void TestCloneCache()
        {
            cloneCache.Should().NotBeNull();
        }

        [TestMethod]
        public void TestAddInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            cloneCache[new MyKey("key1")].Should().Be(new MyValue("value1"));

            cloneCache.Commit();
            myDataCache[new MyKey("key1")].Should().Be(new MyValue("value1"));
        }

        [TestMethod]
        public void TestDeleteInternal()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            cloneCache.Delete(new MyKey("key1"));   //  trackable.State = TrackState.Deleted 
            cloneCache.Commit();

            cloneCache.TryGet(new MyKey("key1")).Should().BeNull();
            myDataCache.TryGet(new MyKey("key1")).Should().BeNull();
        }

        [TestMethod]
        public void TestFindInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            var items = cloneCache.Find(new MyKey("key1").ToArray());
            items.ElementAt(0).Key.Should().Be(new MyKey("key1"));
            items.ElementAt(0).Value.Should().Be(new MyValue("value1"));
            items.Count().Should().Be(1);

            items = cloneCache.Find(new MyKey("key2").ToArray());
            items.ElementAt(0).Key.Should().Be(new MyKey("key2"));
            items.ElementAt(0).Value.Should().Be(new MyValue("value2"));
            items.Count().Should().Be(1);

            items = cloneCache.Find(new MyKey("key3").ToArray());
            items.ElementAt(0).Key.Should().Be(new MyKey("key3"));
            items.ElementAt(0).Value.Should().Be(new MyValue("value3"));
            items.Count().Should().Be(1);

            items = cloneCache.Find(new MyKey("key4").ToArray());
            items.Count().Should().Be(0);
        }

        [TestMethod]
        public void TestGetInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            cloneCache[new MyKey("key1")].Should().Be(new MyValue("value1"));
            cloneCache[new MyKey("key2")].Should().Be(new MyValue("value2"));
            cloneCache[new MyKey("key3")].Should().Be(new MyValue("value3"));

            Action action = () =>
            {
                var item = cloneCache[new MyKey("key4")];
            };
            action.ShouldThrow<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            cloneCache.TryGet(new MyKey("key1")).Should().Be(new MyValue("value1"));
            cloneCache.TryGet(new MyKey("key2")).Should().Be(new MyValue("value2"));
            cloneCache.TryGet(new MyKey("key3")).Should().Be(new MyValue("value3"));
            cloneCache.TryGet(new MyKey("key4")).Should().BeNull();
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            cloneCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            cloneCache.GetAndChange(new MyKey("key1")).Value = "value_new_1";
            cloneCache.GetAndChange(new MyKey("key2")).Value = "value_new_2";
            cloneCache.GetAndChange(new MyKey("key3")).Value = "value_new_3";

            cloneCache.Commit();

            cloneCache[new MyKey("key1")].Should().Be(new MyValue("value_new_1"));
            cloneCache[new MyKey("key2")].Should().Be(new MyValue("value_new_2"));
            cloneCache[new MyKey("key3")].Should().Be(new MyValue("value_new_3"));
            myDataCache[new MyKey("key2")].Should().Be(new MyValue("value_new_2"));
        }
    }
}
