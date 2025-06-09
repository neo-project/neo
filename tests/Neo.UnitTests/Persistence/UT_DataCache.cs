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
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_DataCache
    {
        private readonly MemoryStore _store = new();
        private StoreCache _myDataCache;

        private static readonly StorageKey s_key1 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key1") };
        private static readonly StorageKey s_key2 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key2") };
        private static readonly StorageKey s_key3 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key3") };
        private static readonly StorageKey s_key4 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key4") };
        private static readonly StorageKey s_key5 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key5") };

        private static readonly StorageItem s_value1 = new(Encoding.UTF8.GetBytes("value1"));
        private static readonly StorageItem s_value2 = new(Encoding.UTF8.GetBytes("value2"));
        private static readonly StorageItem s_value3 = new(Encoding.UTF8.GetBytes("value3"));
        private static readonly StorageItem s_value4 = new(Encoding.UTF8.GetBytes("value4"));
        private static readonly StorageItem s_value5 = new(Encoding.UTF8.GetBytes("value5"));

        [TestInitialize]
        public void Initialize()
        {
            _myDataCache = new(_store, false);
        }

        [TestMethod]
        public void TestAccessByKey()
        {
            _myDataCache.Add(s_key1, s_value1);
            _myDataCache.Add(s_key2, s_value2);

            Assert.IsTrue(_myDataCache[s_key1].EqualsTo(s_value1));

            // case 2 read from inner
            _store.Put(s_key3.ToArray(), s_value3.ToArray());
            Assert.IsTrue(_myDataCache[s_key3].EqualsTo(s_value3));
        }

        [TestMethod]
        public void TestAccessByNotFoundKey()
        {
            Assert.ThrowsExactly<KeyNotFoundException>(() =>
            {
                _ = _myDataCache[s_key1];
            });
        }

        [TestMethod]
        public void TestAccessByDeletedKey()
        {
            _store.Put(s_key1.ToArray(), s_value1.ToArray());
            _myDataCache.Delete(s_key1);

            Assert.ThrowsExactly<KeyNotFoundException>(() =>
            {
                _ = _myDataCache[s_key1];
            });
        }

        [TestMethod]
        public void TestAdd()
        {
            var read = 0;
            var updated = 0;
            _myDataCache.OnRead += (sender, key, value) => { read++; };
            _myDataCache.OnUpdate += (sender, key, value) => { updated++; };
            _myDataCache.Add(s_key1, s_value1);

            Assert.AreEqual(s_value1, _myDataCache[s_key1]);
            Assert.AreEqual(0, read);
            Assert.AreEqual(0, updated);

            Action action = () => _myDataCache.Add(s_key1, s_value1);
            Assert.ThrowsExactly<ArgumentException>(action);

            _store.Put(s_key2.ToArray(), s_value2.ToArray());
            _myDataCache.Delete(s_key2);
            Assert.AreEqual(TrackState.Deleted, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key2)).Select(u => u.Value.State).FirstOrDefault());
            _myDataCache.Add(s_key2, s_value2);
            Assert.AreEqual(TrackState.Changed, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key2)).Select(u => u.Value.State).FirstOrDefault());

            action = () => _myDataCache.Add(s_key2, s_value2);
            Assert.ThrowsExactly<ArgumentException>(action);
        }

        [TestMethod]
        public void TestCommit()
        {
            using var store = new MemoryStore();
            store.Put(s_key2.ToArray(), s_value2.ToArray());
            store.Put(s_key3.ToArray(), s_value3.ToArray());

            using var snapshot = store.GetSnapshot();
            using var myDataCache = new StoreCache(snapshot);

            var read = 0;
            var updated = 0;
            myDataCache.OnRead += (sender, key, value) => { read++; };
            myDataCache.OnUpdate += (sender, key, value) => { updated++; };

            myDataCache.Add(s_key1, s_value1);
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key1)).Select(u => u.Value.State).FirstOrDefault());
            Assert.AreEqual(0, read);
            Assert.AreEqual(0, updated);

            myDataCache.Delete(s_key2);

            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key2)).Select(u => u.Value.State).FirstOrDefault());
            Assert.AreEqual(1, read);
            Assert.AreEqual(0, updated);
            Assert.AreEqual(TrackState.None, myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key3)).Select(u => u.Value.State).FirstOrDefault());

            myDataCache.Delete(s_key3);

            Assert.AreEqual(2, read);
            Assert.AreEqual(0, updated);
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key3)).Select(u => u.Value.State).FirstOrDefault());

            myDataCache.Add(s_key3, s_value4);
            Assert.AreEqual(TrackState.Changed, myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key3)).Select(u => u.Value.State).FirstOrDefault());
            Assert.AreEqual(2, read);
            Assert.AreEqual(0, updated);

            // If we use myDataCache after it is committed, it will return wrong result.
            myDataCache.Commit();

            Assert.AreEqual(0, myDataCache.GetChangeSet().Count());
            Assert.AreEqual(2, read);
            Assert.AreEqual(1, updated);
            Assert.IsTrue(store.TryGet(s_key1.ToArray()).SequenceEqual(s_value1.ToArray()));
            Assert.IsNull(store.TryGet(s_key2.ToArray()));
            Assert.IsTrue(store.TryGet(s_key3.ToArray()).SequenceEqual(s_value4.ToArray()));

            Assert.IsTrue(myDataCache.TryGet(s_key1).Value.ToArray().SequenceEqual(s_value1.ToArray()));
            // Though value is deleted from the store, the value can still be gotten from the snapshot cache.
            Assert.IsTrue(myDataCache.TryGet(s_key2).Value.ToArray().SequenceEqual(s_value2.ToArray()));
            Assert.IsTrue(myDataCache.TryGet(s_key3).Value.ToArray().SequenceEqual(s_value4.ToArray()));
        }

        [TestMethod]
        public void TestCreateSnapshot()
        {
            Assert.IsNotNull(_myDataCache.CloneCache());
        }

        [TestMethod]
        public void TestDelete()
        {
            using var store = new MemoryStore();
            store.Put(s_key2.ToArray(), s_value2.ToArray());

            using var snapshot = store.GetSnapshot();
            using var myDataCache = new StoreCache(snapshot);

            myDataCache.Add(s_key1, s_value1);
            myDataCache.Delete(s_key1);
            Assert.IsNull(store.TryGet(s_key1.ToArray()));

            myDataCache.Delete(s_key2);
            myDataCache.Commit();
            Assert.IsNull(store.TryGet(s_key2.ToArray()));
        }

        [TestMethod]
        public void TestFind()
        {
            _myDataCache.Add(s_key1, s_value1);
            _myDataCache.Add(s_key2, s_value2);

            _store.Put(s_key3.ToArray(), s_value3.ToArray());
            _store.Put(s_key4.ToArray(), s_value4.ToArray());

            var k1 = s_key1.ToArray();
            var items = _myDataCache.Find(k1);
            Assert.AreEqual(s_key1, items.ElementAt(0).Key);
            Assert.AreEqual(s_value1, items.ElementAt(0).Value);
            Assert.AreEqual(1, items.Count());

            // null and empty with the forward direction -> finds everything.
            items = _myDataCache.Find(null);
            Assert.AreEqual(4, items.Count());
            items = _myDataCache.Find([]);
            Assert.AreEqual(4, items.Count());

            // null and empty with the backwards direction -> miserably fails.
            Action action = () => _myDataCache.Find(null, SeekDirection.Backward);
            Assert.ThrowsExactly<ArgumentNullException>(action);
            action = () => _myDataCache.Find([], SeekDirection.Backward);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(action);

            items = _myDataCache.Find(k1, SeekDirection.Backward);
            Assert.AreEqual(s_key1, items.ElementAt(0).Key);
            Assert.AreEqual(s_value1, items.ElementAt(0).Value);
            Assert.AreEqual(1, items.Count());

            var prefix = k1.Take(k1.Length - 1).ToArray(); // Just the "key" part to match everything.
            items = _myDataCache.Find(prefix);
            Assert.AreEqual(4, items.Count());
            Assert.AreEqual(s_key1, items.ElementAt(0).Key);
            Assert.AreEqual(s_value1, items.ElementAt(0).Value);
            Assert.AreEqual(s_key2, items.ElementAt(1).Key);
            Assert.AreEqual(s_value2, items.ElementAt(1).Value);
            Assert.AreEqual(s_key3, items.ElementAt(2).Key);
            Assert.IsTrue(items.ElementAt(2).Value.EqualsTo(s_value3));
            Assert.AreEqual(s_key4, items.ElementAt(3).Key);
            Assert.IsTrue(items.ElementAt(3).Value.EqualsTo(s_value4));

            items = _myDataCache.Find(prefix, SeekDirection.Backward);
            Assert.AreEqual(4, items.Count());
            Assert.AreEqual(s_key4, items.ElementAt(0).Key);
            Assert.IsTrue(items.ElementAt(0).Value.EqualsTo(s_value4));
            Assert.AreEqual(s_key3, items.ElementAt(1).Key);
            Assert.IsTrue(items.ElementAt(1).Value.EqualsTo(s_value3));
            Assert.AreEqual(s_key2, items.ElementAt(2).Key);
            Assert.AreEqual(s_value2, items.ElementAt(2).Value);
            Assert.AreEqual(s_key1, items.ElementAt(3).Key);
            Assert.AreEqual(s_value1, items.ElementAt(3).Value);

            items = _myDataCache.Find(s_key5);
            Assert.AreEqual(0, items.Count());
        }

        [TestMethod]
        public void TestSeek()
        {
            _myDataCache.Add(s_key1, s_value1);
            _myDataCache.Add(s_key2, s_value2);

            _store.Put(s_key3.ToArray(), s_value3.ToArray());
            _store.Put(s_key4.ToArray(), s_value4.ToArray());

            var items = _myDataCache.Seek(s_key3.ToArray(), SeekDirection.Backward).ToArray();
            Assert.AreEqual(s_key3, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(s_value3));
            Assert.AreEqual(s_key2, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(s_value2));
            Assert.AreEqual(3, items.Length);

            items = [.. _myDataCache.Seek(s_key5.ToArray(), SeekDirection.Forward)];
            Assert.AreEqual(0, items.Length);
        }

        [TestMethod]
        public void TestFindRange()
        {
            var store = new MemoryStore();
            store.Put(s_key3.ToArray(), s_value3.ToArray());
            store.Put(s_key4.ToArray(), s_value4.ToArray());

            var myDataCache = new StoreCache(store);
            myDataCache.Add(s_key1, s_value1);
            myDataCache.Add(s_key2, s_value2);

            var items = myDataCache.FindRange(s_key3.ToArray(), s_key5.ToArray()).ToArray();
            Assert.AreEqual(s_key3, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(s_value3));
            Assert.AreEqual(s_key4, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(s_value4));
            Assert.AreEqual(2, items.Length);

            // case 2 Need to sort the cache of myDataCache

            store = new();
            store.Put(s_key4.ToArray(), s_value4.ToArray());
            store.Put(s_key3.ToArray(), s_value3.ToArray());

            myDataCache = new(store);
            myDataCache.Add(s_key1, s_value1);
            myDataCache.Add(s_key2, s_value2);

            items = [.. myDataCache.FindRange(s_key3.ToArray(), s_key5.ToArray())];
            Assert.AreEqual(s_key3, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(s_value3));
            Assert.AreEqual(s_key4, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(s_value4));
            Assert.AreEqual(2, items.Length);

            // case 3 FindRange by Backward

            store = new();
            store.Put(s_key4.ToArray(), s_value4.ToArray());
            store.Put(s_key3.ToArray(), s_value3.ToArray());
            store.Put(s_key5.ToArray(), s_value5.ToArray());

            myDataCache = new(store);
            myDataCache.Add(s_key1, s_value1);
            myDataCache.Add(s_key2, s_value2);

            items = [.. myDataCache.FindRange(s_key5.ToArray(), s_key3.ToArray(), SeekDirection.Backward)];
            Assert.AreEqual(s_key5, items[0].Key);
            Assert.IsTrue(items[0].Value.EqualsTo(s_value5));
            Assert.AreEqual(s_key4, items[1].Key);
            Assert.IsTrue(items[1].Value.EqualsTo(s_value4));
            Assert.AreEqual(2, items.Length);
        }

        [TestMethod]
        public void TestGetChangeSet()
        {
            _myDataCache.Add(s_key1, s_value1);
            Assert.AreEqual(TrackState.Added, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key1)).Select(u => u.Value.State).FirstOrDefault());
            _myDataCache.Add(s_key2, s_value2);
            Assert.AreEqual(TrackState.Added, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key2)).Select(u => u.Value.State).FirstOrDefault());

            _store.Put(s_key3.ToArray(), s_value3.ToArray());
            _store.Put(s_key4.ToArray(), s_value4.ToArray());
            _myDataCache.Delete(s_key3);
            Assert.AreEqual(TrackState.Deleted, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key3)).Select(u => u.Value.State).FirstOrDefault());
            _myDataCache.Delete(s_key4);
            Assert.AreEqual(TrackState.Deleted, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key4)).Select(u => u.Value.State).FirstOrDefault());

            var items = _myDataCache.GetChangeSet();
            var i = 0;
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
            _myDataCache.Add(s_key1, s_value1);
            Assert.AreEqual(TrackState.Added, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key1)).Select(u => u.Value.State).FirstOrDefault());
            _store.Put(s_key2.ToArray(), s_value2.ToArray());
            _store.Put(s_key3.ToArray(), s_value3.ToArray());
            _myDataCache.Delete(s_key3);
            Assert.AreEqual(TrackState.Deleted, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key3)).Select(u => u.Value.State).FirstOrDefault());

            StorageItem value_bk_1 = new(Encoding.UTF8.GetBytes("value_bk_1"));
            StorageItem value_bk_2 = new(Encoding.UTF8.GetBytes("value_bk_2"));
            StorageItem value_bk_3 = new(Encoding.UTF8.GetBytes("value_bk_3"));
            StorageItem value_bk_4 = new(Encoding.UTF8.GetBytes("value_bk_4"));

            Assert.IsTrue(_myDataCache.GetAndChange(s_key1, () => value_bk_1).EqualsTo(s_value1));
            Assert.IsTrue(_myDataCache.GetAndChange(s_key2, () => value_bk_2).EqualsTo(s_value2));
            Assert.IsTrue(_myDataCache.GetAndChange(s_key3, () => value_bk_3).EqualsTo(value_bk_3));
            Assert.IsTrue(_myDataCache.GetAndChange(s_key4, () => value_bk_4).EqualsTo(value_bk_4));
        }

        [TestMethod]
        public void TestGetOrAdd()
        {
            _myDataCache.Add(s_key1, s_value1);
            Assert.AreEqual(TrackState.Added, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key1)).Select(u => u.Value.State).FirstOrDefault());
            _store.Put(s_key2.ToArray(), s_value2.ToArray());
            _store.Put(s_key3.ToArray(), s_value3.ToArray());
            _myDataCache.Delete(s_key3);
            Assert.AreEqual(TrackState.Deleted, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key3)).Select(u => u.Value.State).FirstOrDefault());

            StorageItem value_bk_1 = new(Encoding.UTF8.GetBytes("value_bk_1"));
            StorageItem value_bk_2 = new(Encoding.UTF8.GetBytes("value_bk_2"));
            StorageItem value_bk_3 = new(Encoding.UTF8.GetBytes("value_bk_3"));
            StorageItem value_bk_4 = new(Encoding.UTF8.GetBytes("value_bk_4"));

            Assert.IsTrue(_myDataCache.GetOrAdd(s_key1, () => value_bk_1).EqualsTo(s_value1));
            Assert.IsTrue(_myDataCache.GetOrAdd(s_key2, () => value_bk_2).EqualsTo(s_value2));
            Assert.IsTrue(_myDataCache.GetOrAdd(s_key3, () => value_bk_3).EqualsTo(value_bk_3));
            Assert.IsTrue(_myDataCache.GetOrAdd(s_key4, () => value_bk_4).EqualsTo(value_bk_4));
        }

        [TestMethod]
        public void TestTryGet()
        {
            _myDataCache.Add(s_key1, s_value1);
            Assert.AreEqual(TrackState.Added, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key1)).Select(u => u.Value.State).FirstOrDefault());
            _store.Put(s_key2.ToArray(), s_value2.ToArray());
            _store.Put(s_key3.ToArray(), s_value3.ToArray());
            _myDataCache.Delete(s_key3);
            Assert.AreEqual(TrackState.Deleted, _myDataCache.GetChangeSet().Where(u => u.Key.Equals(s_key3)).Select(u => u.Value.State).FirstOrDefault());

            Assert.IsTrue(_myDataCache.TryGet(s_key1).EqualsTo(s_value1));
            Assert.IsTrue(_myDataCache.TryGet(s_key2).EqualsTo(s_value2));
            Assert.IsNull(_myDataCache.TryGet(s_key3));
        }

        [TestMethod]
        public void TestFindInvalid()
        {
            using var store = new MemoryStore();
            using var myDataCache = new StoreCache(store);
            myDataCache.Add(s_key1, s_value1);

            store.Put(s_key2.ToArray(), s_value2.ToArray());
            store.Put(s_key3.ToArray(), s_value3.ToArray());
            store.Put(s_key4.ToArray(), s_value3.ToArray());

            var items = myDataCache.Find(SeekDirection.Forward).GetEnumerator();
            items.MoveNext();
            Assert.AreEqual(s_key1, items.Current.Key);

            myDataCache.TryGet(s_key3); // GETLINE

            items.MoveNext();
            Assert.AreEqual(s_key2, items.Current.Key);
            items.MoveNext();
            Assert.AreEqual(s_key3, items.Current.Key);
            items.MoveNext();
            Assert.AreEqual(s_key4, items.Current.Key);
            Assert.IsFalse(items.MoveNext());
        }
    }
}
