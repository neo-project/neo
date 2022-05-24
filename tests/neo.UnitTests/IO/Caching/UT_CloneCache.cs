using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
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

        private static readonly StorageKey key1 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key1") };
        private static readonly StorageKey key2 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key2") };
        private static readonly StorageKey key3 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key3") };
        private static readonly StorageKey key4 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key4") };

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
            clonedCache.Add(key1, new MyValue("value1"));
            clonedCache[key1].Should().Be(new MyValue("value1"));

            clonedCache.Commit();
            Assert.IsTrue(myDataCache[key1].Value.Span.SequenceEqual(new MyValue("value1").Value.Span));
        }

        [TestMethod]
        public void TestDeleteInternal()
        {
            myDataCache.Add(key1, new MyValue("value1"));
            clonedCache.Delete(key1);   //  trackable.State = TrackState.Deleted
            clonedCache.Commit();

            clonedCache.TryGet(key1).Should().BeNull();
            myDataCache.TryGet(key1).Should().BeNull();
        }

        [TestMethod]
        public void TestFindInternal()
        {
            clonedCache.Add(key1, new MyValue("value1"));
            myDataCache.Add(key2, new MyValue("value2"));
            myDataCache.InnerDict.Add(key3, new MyValue("value3"));

            var items = clonedCache.Find(key1.ToArray());
            items.ElementAt(0).Key.Should().Be(key1);
            items.ElementAt(0).Value.Should().Be(new MyValue("value1"));
            items.Count().Should().Be(1);

            items = clonedCache.Find(key2.ToArray());
            items.ElementAt(0).Key.Should().Be(key2);
            new MyValue("value2").Should().Be(items.ElementAt(0).Value);
            items.Count().Should().Be(1);

            items = clonedCache.Find(key3.ToArray());
            items.ElementAt(0).Key.Should().Be(key3);
            new MyValue("value3").Should().Be(items.ElementAt(0).Value);
            items.Count().Should().Be(1);

            items = clonedCache.Find(key4.ToArray());
            items.Count().Should().Be(0);
        }

        [TestMethod]
        public void TestGetInternal()
        {
            clonedCache.Add(key1, new MyValue("value1"));
            myDataCache.Add(key2, new MyValue("value2"));
            myDataCache.InnerDict.Add(key3, new MyValue("value3"));

            new MyValue("value1").Should().Be(clonedCache[key1]);
            new MyValue("value2").Should().Be(clonedCache[key2]);
            new MyValue("value3").Should().Be(clonedCache[key3]);

            Action action = () =>
            {
                var item = clonedCache[key4];
            };
            action.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            clonedCache.Add(key1, new MyValue("value1"));
            myDataCache.Add(key2, new MyValue("value2"));
            myDataCache.InnerDict.Add(key3, new MyValue("value3"));

            new MyValue("value1").Should().Be(clonedCache.TryGet(key1));
            new MyValue("value2").Should().Be(clonedCache.TryGet(key2));
            new MyValue("value3").Should().Be(clonedCache.TryGet(key3));
            clonedCache.TryGet(key4).Should().BeNull();
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            clonedCache.Add(key1, new MyValue("value1"));
            myDataCache.Add(key2, new MyValue("value2"));
            myDataCache.InnerDict.Add(key3, new MyValue("value3"));

            clonedCache.GetAndChange(key1).Value = Encoding.Default.GetBytes("value_new_1");
            clonedCache.GetAndChange(key2).Value = Encoding.Default.GetBytes("value_new_2");
            clonedCache.GetAndChange(key3).Value = Encoding.Default.GetBytes("value_new_3");

            clonedCache.Commit();

            new MyValue("value_new_1").Should().Be(clonedCache[key1]);
            new MyValue("value_new_2").Should().Be(clonedCache[key2]);
            new MyValue("value_new_3").Should().Be(clonedCache[key3]);
            new MyValue("value_new_2").Should().Be(clonedCache[key2]);
        }

        [TestMethod]
        public void TestCacheOverrideIssue2572()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var storages = snapshot.CreateSnapshot();

            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x00, 0x01 }, Id = 0 },
                new StorageItem() { Value = Array.Empty<byte>() }
                );
            storages.Add
                (
                new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 },
                new StorageItem() { Value = new byte[] { 0x05 } }
                );

            storages.Commit();

            var item = storages.GetAndChange(new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 });
            item.Value = new byte[] { 0x06 };

            var res = snapshot.TryGet(new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 });
            Assert.AreEqual("05", res.Value.Span.ToHexString());
            storages.Commit();
            res = snapshot.TryGet(new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 });
            Assert.AreEqual("06", res.Value.Span.ToHexString());
        }
    }
}
