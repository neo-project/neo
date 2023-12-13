using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_DataCache
    {
        private MemoryStore store = new();
        private SnapshotCache myDataCache;

        private static readonly StorageKey key1 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key1") };
        private static readonly StorageKey key2 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key2") };
        private static readonly StorageKey key3 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key3") };
        private static readonly StorageKey key4 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key4") };
        private static readonly StorageKey key5 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key5") };

        private static readonly StorageItem value1 = new(Encoding.UTF8.GetBytes("value1"));
        private static readonly StorageItem value2 = new(Encoding.UTF8.GetBytes("value2"));
        private static readonly StorageItem value3 = new(Encoding.UTF8.GetBytes("value3"));
        private static readonly StorageItem value4 = new(Encoding.UTF8.GetBytes("value4"));
        private static readonly StorageItem value5 = new(Encoding.UTF8.GetBytes("value5"));

        [TestInitialize]
        public void Initialize()
        {
            myDataCache = new(store);
        }

        [TestMethod]
        public void TestAccessByKey()
        {
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            myDataCache[key1].EqualsTo(value1).Should().BeTrue();

            // case 2 read from inner
            store.Put(key3.ToArray(), value3.ToArray());
            myDataCache[key3].EqualsTo(value3).Should().BeTrue();
        }

        [TestMethod]
        public void TestAccessByNotFoundKey()
        {
            Action action = () =>
            {
                var item = myDataCache[key1];
            };
            action.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestAccessByDeletedKey()
        {
            store.Put(key1.ToArray(), value1.ToArray());
            myDataCache.Delete(key1);

            Action action = () =>
            {
                var item = myDataCache[key1];
            };
            action.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestAdd()
        {
            myDataCache.Add(key1, value1);
            myDataCache[key1].Should().Be(value1);

            Action action = () => myDataCache.Add(key1, value1);
            action.Should().Throw<ArgumentException>();

            store.Put(key2.ToArray(), value2.ToArray());
            myDataCache.Delete(key2);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.State).FirstOrDefault());
            myDataCache.Add(key2, value2);
            Assert.AreEqual(TrackState.Changed, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.State).FirstOrDefault());

            action = () => myDataCache.Add(key2, value2);
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestCommit()
        {
            using var store = new MemoryStore();
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());

            using var snapshot = store.GetSnapshot();
            using var myDataCache = new SnapshotCache(snapshot);

            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.State).FirstOrDefault());

            myDataCache.Delete(key2);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.State).FirstOrDefault());

            Assert.AreEqual(TrackState.None, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.State).FirstOrDefault());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.State).FirstOrDefault());
            myDataCache.Add(key3, value4);
            Assert.AreEqual(TrackState.Changed, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.State).FirstOrDefault());

            myDataCache.Commit();
            Assert.AreEqual(0, myDataCache.GetChangeSet().Count());

            store.TryGet(key1.ToArray()).SequenceEqual(value1.ToArray()).Should().BeTrue();
            store.TryGet(key2.ToArray()).Should().BeNull();
            store.TryGet(key3.ToArray()).SequenceEqual(value4.ToArray()).Should().BeTrue();
        }

        [TestMethod]
        public void TestCreateSnapshot()
        {
            myDataCache.CreateSnapshot().Should().NotBeNull();
        }

        [TestMethod]
        public void TestDelete()
        {
            using var store = new MemoryStore();
            store.Put(key2.ToArray(), value2.ToArray());

            using var snapshot = store.GetSnapshot();
            using var myDataCache = new SnapshotCache(snapshot);

            myDataCache.Add(key1, value1);
            myDataCache.Delete(key1);
            store.TryGet(key1.ToArray()).Should().BeNull();

            myDataCache.Delete(key2);
            myDataCache.Commit();
            store.TryGet(key2.ToArray()).Should().BeNull();
        }

        [TestMethod]
        public void TestFind()
        {
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value4.ToArray());

            var k1 = key1.ToArray();
            var items = myDataCache.Find(k1);
            key1.Should().Be(items.ElementAt(0).Key);
            value1.Should().Be(items.ElementAt(0).Value);
            items.Count().Should().Be(1);

            // null and empty with the forward direction -> finds everything.
            items = myDataCache.Find(null);
            items.Count().Should().Be(4);
            items = myDataCache.Find(new byte[] { });
            items.Count().Should().Be(4);

            // null and empty with the backwards direction -> miserably fails.
            Action action = () => myDataCache.Find(null, SeekDirection.Backward);
            action.Should().Throw<ArgumentException>();
            action = () => myDataCache.Find(new byte[] { }, SeekDirection.Backward);
            action.Should().Throw<ArgumentException>();

            items = myDataCache.Find(k1, SeekDirection.Backward);
            key1.Should().Be(items.ElementAt(0).Key);
            value1.Should().Be(items.ElementAt(0).Value);
            items.Count().Should().Be(1);

            var prefix = k1.Take(k1.Count() - 1).ToArray(); // Just the "key" part to match everything.
            items = myDataCache.Find(prefix);
            items.Count().Should().Be(4);
            key1.Should().Be(items.ElementAt(0).Key);
            value1.Should().Be(items.ElementAt(0).Value);
            key2.Should().Be(items.ElementAt(1).Key);
            value2.Should().Be(items.ElementAt(1).Value);
            key3.Should().Be(items.ElementAt(2).Key);
            value3.EqualsTo(items.ElementAt(2).Value).Should().BeTrue();
            key4.Should().Be(items.ElementAt(3).Key);
            value4.EqualsTo(items.ElementAt(3).Value).Should().BeTrue();

            items = myDataCache.Find(prefix, SeekDirection.Backward);
            items.Count().Should().Be(4);
            key4.Should().Be(items.ElementAt(0).Key);
            value4.EqualsTo(items.ElementAt(0).Value).Should().BeTrue();
            key3.Should().Be(items.ElementAt(1).Key);
            value3.EqualsTo(items.ElementAt(1).Value).Should().BeTrue();
            key2.Should().Be(items.ElementAt(2).Key);
            value2.Should().Be(items.ElementAt(2).Value);
            key1.Should().Be(items.ElementAt(3).Key);
            value1.Should().Be(items.ElementAt(3).Value);

            items = myDataCache.Find(key5.ToArray());
            items.Count().Should().Be(0);
        }

        [TestMethod]
        public void TestSeek()
        {
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value4.ToArray());

            var items = myDataCache.Seek(key3.ToArray(), SeekDirection.Backward).ToArray();
            key3.Should().Be(items[0].Key);
            value3.EqualsTo(items[0].Value).Should().BeTrue();
            key2.Should().Be(items[1].Key);
            value2.EqualsTo(items[1].Value).Should().BeTrue();
            items.Length.Should().Be(3);

            items = myDataCache.Seek(key5.ToArray(), SeekDirection.Forward).ToArray();
            items.Length.Should().Be(0);
        }

        [TestMethod]
        public void TestFindRange()
        {
            var store = new MemoryStore();
            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value4.ToArray());

            var myDataCache = new SnapshotCache(store);
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            var items = myDataCache.FindRange(key3.ToArray(), key5.ToArray()).ToArray();
            key3.Should().Be(items[0].Key);
            value3.EqualsTo(items[0].Value).Should().BeTrue();
            key4.Should().Be(items[1].Key);
            value4.EqualsTo(items[1].Value).Should().BeTrue();
            items.Length.Should().Be(2);

            // case 2 Need to sort the cache of myDataCache

            store = new();
            store.Put(key4.ToArray(), value4.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());

            myDataCache = new(store);
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            items = myDataCache.FindRange(key3.ToArray(), key5.ToArray()).ToArray();
            key3.Should().Be(items[0].Key);
            value3.EqualsTo(items[0].Value).Should().BeTrue();
            key4.Should().Be(items[1].Key);
            value4.EqualsTo(items[1].Value).Should().BeTrue();
            items.Length.Should().Be(2);

            // case 3 FindRange by Backward

            store = new();
            store.Put(key4.ToArray(), value4.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key5.ToArray(), value5.ToArray());

            myDataCache = new(store);
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            items = myDataCache.FindRange(key5.ToArray(), key3.ToArray(), SeekDirection.Backward).ToArray();
            key5.Should().Be(items[0].Key);
            value5.EqualsTo(items[0].Value).Should().BeTrue();
            key4.Should().Be(items[1].Key);
            value4.EqualsTo(items[1].Value).Should().BeTrue();
            items.Length.Should().Be(2);
        }

        [TestMethod]
        public void TestGetChangeSet()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.State).FirstOrDefault());
            myDataCache.Add(key2, value2);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.State).FirstOrDefault());

            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value4.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.State).FirstOrDefault());
            myDataCache.Delete(key4);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key4)).Select(u => u.State).FirstOrDefault());

            var items = myDataCache.GetChangeSet();
            int i = 0;
            foreach (var item in items)
            {
                i++;
                StorageKey key = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key" + i) };
                StorageItem value = new(Encoding.UTF8.GetBytes("value" + i));
                key.Should().Be(item.Key);
                value.EqualsTo(item.Item).Should().BeTrue();
            }
            i.Should().Be(4);
        }

        [TestMethod]
        public void TestGetAndChange()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.State).FirstOrDefault());
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.State).FirstOrDefault());

            StorageItem value_bk_1 = new(Encoding.UTF8.GetBytes("value_bk_1"));
            StorageItem value_bk_2 = new(Encoding.UTF8.GetBytes("value_bk_2"));
            StorageItem value_bk_3 = new(Encoding.UTF8.GetBytes("value_bk_3"));
            StorageItem value_bk_4 = new(Encoding.UTF8.GetBytes("value_bk_4"));

            myDataCache.GetAndChange(key1, () => value_bk_1).EqualsTo(value1).Should().BeTrue();
            myDataCache.GetAndChange(key2, () => value_bk_2).EqualsTo(value2).Should().BeTrue();
            myDataCache.GetAndChange(key3, () => value_bk_3).EqualsTo(value_bk_3).Should().BeTrue();
            myDataCache.GetAndChange(key4, () => value_bk_4).EqualsTo(value_bk_4).Should().BeTrue();
        }

        [TestMethod]
        public void TestGetOrAdd()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.State).FirstOrDefault());
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.State).FirstOrDefault());

            StorageItem value_bk_1 = new(Encoding.UTF8.GetBytes("value_bk_1"));
            StorageItem value_bk_2 = new(Encoding.UTF8.GetBytes("value_bk_2"));
            StorageItem value_bk_3 = new(Encoding.UTF8.GetBytes("value_bk_3"));
            StorageItem value_bk_4 = new(Encoding.UTF8.GetBytes("value_bk_4"));

            myDataCache.GetOrAdd(key1, () => value_bk_1).EqualsTo(value1).Should().BeTrue();
            myDataCache.GetOrAdd(key2, () => value_bk_2).EqualsTo(value2).Should().BeTrue();
            myDataCache.GetOrAdd(key3, () => value_bk_3).EqualsTo(value_bk_3).Should().BeTrue();
            myDataCache.GetOrAdd(key4, () => value_bk_4).EqualsTo(value_bk_4).Should().BeTrue();
        }

        [TestMethod]
        public void TestTryGet()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.State).FirstOrDefault());
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.State).FirstOrDefault());

            myDataCache.TryGet(key1).EqualsTo(value1).Should().BeTrue();
            myDataCache.TryGet(key2).EqualsTo(value2).Should().BeTrue();
            myDataCache.TryGet(key3).Should().BeNull();
        }

        [TestMethod]
        public void TestFindInvalid()
        {
            using var store = new MemoryStore();
            using var myDataCache = new SnapshotCache(store);
            myDataCache.Add(key1, value1);

            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value3.ToArray());

            var items = myDataCache.Find().GetEnumerator();
            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(key1);

            myDataCache.TryGet(key3); // GETLINE

            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(key2);
            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(key3);
            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(key4);
            items.MoveNext().Should().Be(false);
        }
    }
}
