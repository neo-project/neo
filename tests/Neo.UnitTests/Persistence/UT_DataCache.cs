// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DataCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_DataCache
    {
        private readonly MemoryStore store = new();
        private StoreCache myDataCache;

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

            Assert.IsTrue(myDataCache[key1].EqualsTo(value1));

            // case 2 read from inner
            store.Put(key3.ToArray(), value3.ToArray());
            Assert.IsTrue(myDataCache[key3].EqualsTo(value3));
        }

        [TestMethod]
        public void TestAccessByNotFoundKey()
        {
            Action action = () =>
            {
                var item = myDataCache[key1];
            };
            Assert.ThrowsException<KeyNotFoundException>(action);
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
            Assert.ThrowsException<KeyNotFoundException>(action);
        }

        [TestMethod]
        public void TestAdd()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(value1, myDataCache[key1]);

            Action action = () => myDataCache.Add(key1, value1);
            Assert.ThrowsException<ArgumentException>(action);

            store.Put(key2.ToArray(), value2.ToArray());
            myDataCache.Delete(key2);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.Value.State).FirstOrDefault());
            myDataCache.Add(key2, value2);
            Assert.AreEqual(TrackState.Changed, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.Value.State).FirstOrDefault());

            action = () => myDataCache.Add(key2, value2);
            Assert.ThrowsException<ArgumentException>(action);
        }

        [TestMethod]
        public void TestCommit()
        {
            using var store = new MemoryStore();
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());

            using var snapshot = store.GetSnapshot();
            using var myDataCache = new StoreCache(snapshot);

            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.Value.State).FirstOrDefault());

            myDataCache.Delete(key2);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.Value.State).FirstOrDefault());

            Assert.AreEqual(TrackState.None, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.Value.State).FirstOrDefault());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.Value.State).FirstOrDefault());
            myDataCache.Add(key3, value4);
            Assert.AreEqual(TrackState.Changed, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.Value.State).FirstOrDefault());

            // If we use myDataCache after it is committed, it will return wrong result.
            myDataCache.Commit();
            Assert.AreEqual(0, myDataCache.GetChangeSet().Count());

            Assert.IsTrue(store.TryGet(key1.ToArray()).SequenceEqual(value1.ToArray()));
            Assert.IsNull(store.TryGet(key2.ToArray()));
            Assert.IsTrue(store.TryGet(key3.ToArray()).SequenceEqual(value4.ToArray()));

            Assert.IsTrue(myDataCache.TryGet(key1).Value.ToArray().SequenceEqual(value1.ToArray()));
            // Though value is deleted from the store, the value can still be gotten from the snapshot cache.
            Assert.IsTrue(myDataCache.TryGet(key2).Value.ToArray().SequenceEqual(value2.ToArray()));
            Assert.IsTrue(myDataCache.TryGet(key3).Value.ToArray().SequenceEqual(value4.ToArray()));
        }

        [TestMethod]
        public void TestCreateSnapshot()
        {
            Assert.IsNotNull(myDataCache.CloneCache());
        }

        [TestMethod]
        public void TestDelete()
        {
            using var store = new MemoryStore();
            store.Put(key2.ToArray(), value2.ToArray());

            using var snapshot = store.GetSnapshot();
            using var myDataCache = new StoreCache(snapshot);

            myDataCache.Add(key1, value1);
            myDataCache.Delete(key1);
            Assert.IsNull(store.TryGet(key1.ToArray()));

            myDataCache.Delete(key2);
            myDataCache.Commit();
            Assert.IsNull(store.TryGet(key2.ToArray()));
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
            Assert.AreEqual(key1, items.ElementAt(0).Key);
            Assert.AreEqual(value1, items.ElementAt(0).Value);
            Assert.AreEqual(1, items.Count());

            // null and empty with the forward direction -> finds everything.
            items = myDataCache.Find(null);
            Assert.AreEqual(4, items.Count());
            items = myDataCache.Find([]);
            Assert.AreEqual(4, items.Count());

            // null and empty with the backwards direction -> miserably fails.
            Action action = () => myDataCache.Find(null, SeekDirection.Backward);
            Assert.ThrowsException<ArgumentNullException>(action);
            action = () => myDataCache.Find([], SeekDirection.Backward);
            Assert.ThrowsException<ArgumentOutOfRangeException>(action);

            items = myDataCache.Find(k1, SeekDirection.Backward);
            Assert.AreEqual(key1, items.ElementAt(0).Key);
            Assert.AreEqual(value1, items.ElementAt(0).Value);
            Assert.AreEqual(1, items.Count());

            var prefix = k1.Take(k1.Count() - 1).ToArray(); // Just the "key" part to match everything.
            items = myDataCache.Find(prefix);
            Assert.AreEqual(4, items.Count());
            Assert.AreEqual(key1, items.ElementAt(0).Key);
            Assert.AreEqual(value1, items.ElementAt(0).Value);
            Assert.AreEqual(key2, items.ElementAt(1).Key);
            Assert.AreEqual(value2, items.ElementAt(1).Value);
            Assert.AreEqual(key3, items.ElementAt(2).Key);
            Assert.IsTrue(items.ElementAt(2).Value.EqualsTo(value3));
            Assert.AreEqual(key4, items.ElementAt(3).Key);
            Assert.IsTrue(items.ElementAt(3).Value.EqualsTo(value4));

            items = myDataCache.Find(prefix, SeekDirection.Backward);
            Assert.AreEqual(4, items.Count());
            Assert.AreEqual(key4, items.ElementAt(0).Key);
            Assert.IsTrue(items.ElementAt(0).Value.EqualsTo(value4));
            Assert.AreEqual(key3, items.ElementAt(1).Key);
            Assert.IsTrue(items.ElementAt(1).Value.EqualsTo(value3));
            Assert.AreEqual(key2, items.ElementAt(2).Key);
            Assert.AreEqual(value2, items.ElementAt(2).Value);
            Assert.AreEqual(key1, items.ElementAt(3).Key);
            Assert.AreEqual(value1, items.ElementAt(3).Value);

            items = myDataCache.Find(key5.ToArray());
            Assert.AreEqual(0, items.Count());
        }

        [TestMethod]
        public void TestSeek()
        {
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value4.ToArray());

            var items = myDataCache.Seek(key3.ToArray(), SeekDirection.Backward).ToArray();
            Assert.AreEqual(key3, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(value3));
            Assert.AreEqual(key2, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(value2));
            Assert.AreEqual(3, items.Length);

            items = myDataCache.Seek(key5.ToArray(), SeekDirection.Forward).ToArray();
            Assert.AreEqual(0, items.Length);
        }

        [TestMethod]
        public void TestFindRange()
        {
            var store = new MemoryStore();
            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value4.ToArray());

            var myDataCache = new StoreCache(store);
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            var items = myDataCache.FindRange(key3.ToArray(), key5.ToArray()).ToArray();
            Assert.AreEqual(key3, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(value3));
            Assert.AreEqual(key4, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(value4));
            Assert.AreEqual(2, items.Length);

            // case 2 Need to sort the cache of myDataCache

            store = new();
            store.Put(key4.ToArray(), value4.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());

            myDataCache = new(store);
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            items = myDataCache.FindRange(key3.ToArray(), key5.ToArray()).ToArray();
            Assert.AreEqual(key3, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(value3));
            Assert.AreEqual(key4, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(value4));
            Assert.AreEqual(2, items.Length);

            // case 3 FindRange by Backward

            store = new();
            store.Put(key4.ToArray(), value4.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key5.ToArray(), value5.ToArray());

            myDataCache = new(store);
            myDataCache.Add(key1, value1);
            myDataCache.Add(key2, value2);

            items = myDataCache.FindRange(key5.ToArray(), key3.ToArray(), SeekDirection.Backward).ToArray();
            Assert.AreEqual(key5, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(value5));
            Assert.AreEqual(key4, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(value4));
            Assert.AreEqual(2, items.Length);
        }

        [TestMethod]
        public void TestGetChangeSet()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.Value.State).FirstOrDefault());
            myDataCache.Add(key2, value2);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key2)).Select(u => u.Value.State).FirstOrDefault());

            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value4.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.Value.State).FirstOrDefault());
            myDataCache.Delete(key4);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key4)).Select(u => u.Value.State).FirstOrDefault());

            var items = myDataCache.GetChangeSet();
            int i = 0;
            foreach (var item in items)
            {
                i++;
                StorageKey key = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key" + i) };
                StorageItem value = new(Encoding.UTF8.GetBytes("value" + i));
                Assert.AreEqual(key, item.Key);
                Assert.IsTrue(value.EqualsTo(item.Value.Item));
            }
            Assert.AreEqual(4, i);
        }

        [TestMethod]
        public void TestGetAndChange()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.Value.State).FirstOrDefault());
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.Value.State).FirstOrDefault());

            StorageItem value_bk_1 = new(Encoding.UTF8.GetBytes("value_bk_1"));
            StorageItem value_bk_2 = new(Encoding.UTF8.GetBytes("value_bk_2"));
            StorageItem value_bk_3 = new(Encoding.UTF8.GetBytes("value_bk_3"));
            StorageItem value_bk_4 = new(Encoding.UTF8.GetBytes("value_bk_4"));

            Assert.IsTrue(myDataCache.GetAndChange(key1, () => value_bk_1).EqualsTo(value1));
            Assert.IsTrue(myDataCache.GetAndChange(key2, () => value_bk_2).EqualsTo(value2));
            Assert.IsTrue(myDataCache.GetAndChange(key3, () => value_bk_3).EqualsTo(value_bk_3));
            Assert.IsTrue(myDataCache.GetAndChange(key4, () => value_bk_4).EqualsTo(value_bk_4));
        }

        [TestMethod]
        public void TestGetOrAdd()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.Value.State).FirstOrDefault());
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.Value.State).FirstOrDefault());

            StorageItem value_bk_1 = new(Encoding.UTF8.GetBytes("value_bk_1"));
            StorageItem value_bk_2 = new(Encoding.UTF8.GetBytes("value_bk_2"));
            StorageItem value_bk_3 = new(Encoding.UTF8.GetBytes("value_bk_3"));
            StorageItem value_bk_4 = new(Encoding.UTF8.GetBytes("value_bk_4"));

            Assert.IsTrue(myDataCache.GetOrAdd(key1, () => value_bk_1).EqualsTo(value1));
            Assert.IsTrue(myDataCache.GetOrAdd(key2, () => value_bk_2).EqualsTo(value2));
            Assert.IsTrue(myDataCache.GetOrAdd(key3, () => value_bk_3).EqualsTo(value_bk_3));
            Assert.IsTrue(myDataCache.GetOrAdd(key4, () => value_bk_4).EqualsTo(value_bk_4));
        }

        [TestMethod]
        public void TestTryGet()
        {
            myDataCache.Add(key1, value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key1)).Select(u => u.Value.State).FirstOrDefault());
            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            myDataCache.Delete(key3);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(key3)).Select(u => u.Value.State).FirstOrDefault());

            Assert.IsTrue(myDataCache.TryGet(key1).EqualsTo(value1));
            Assert.IsTrue(myDataCache.TryGet(key2).EqualsTo(value2));
            Assert.IsNull(myDataCache.TryGet(key3));
        }

        [TestMethod]
        public void TestFindInvalid()
        {
            using var store = new MemoryStore();
            using var myDataCache = new StoreCache(store);
            myDataCache.Add(key1, value1);

            store.Put(key2.ToArray(), value2.ToArray());
            store.Put(key3.ToArray(), value3.ToArray());
            store.Put(key4.ToArray(), value3.ToArray());

            var items = myDataCache.Find().GetEnumerator();
            items.MoveNext();
            Assert.AreEqual(key1, items.Current.Key);

            myDataCache.TryGet(key3); // GETLINE

            items.MoveNext();
            Assert.AreEqual(key2, items.Current.Key);
            items.MoveNext();
            Assert.AreEqual(key3, items.Current.Key);
            items.MoveNext();
            Assert.AreEqual(key4, items.Current.Key);
            Assert.IsFalse(items.MoveNext());
        }
    }
}
