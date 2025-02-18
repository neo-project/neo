// Copyright (C) 2015-2025 The Neo Project.
//
// UT_CloneCache.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_CloneCache
    {
        private readonly MemoryStore _store = new();

        private static readonly StorageKey s_key1 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key1") };
        private static readonly StorageKey s_key2 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key2") };
        private static readonly StorageKey s_key3 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key3") };
        private static readonly StorageKey s_key4 = new() { Id = 0, Key = Encoding.UTF8.GetBytes("key4") };

        private static readonly StorageItem s_value1 = new(Encoding.UTF8.GetBytes("value1"));
        private static readonly StorageItem s_value2 = new(Encoding.UTF8.GetBytes("value2"));
        private static readonly StorageItem s_value3 = new(Encoding.UTF8.GetBytes("value3"));

        [TestMethod]
        public void TestCloneCache()
        {
            var myDataCache = new StoreCache(_store);
            var clonedCache = myDataCache.CloneCache();
            Assert.IsNotNull(clonedCache);
        }

        [TestMethod]
        public void TestAddInternal()
        {
            var myDataCache = new StoreCache(_store);
            var clonedCache = myDataCache.CloneCache();

            clonedCache.Add(s_key1, s_value1);
            Assert.AreEqual(s_value1, clonedCache[s_key1]);

            clonedCache.Commit();
            Assert.IsTrue(myDataCache[s_key1].Value.Span.SequenceEqual(s_value1.Value.Span));
        }

        [TestMethod]
        public void TestDeleteInternal()
        {
            var myDataCache = new StoreCache(_store);
            var clonedCache = myDataCache.CloneCache();

            myDataCache.Add(s_key1, s_value1);
            clonedCache.Delete(s_key1);   //  trackable.State = TrackState.Deleted
            clonedCache.Commit();

            Assert.IsNull(clonedCache.TryGet(s_key1));
            Assert.IsNull(myDataCache.TryGet(s_key1));
        }

        [TestMethod]
        public void TestFindInternal()
        {
            var myDataCache = new StoreCache(_store);
            var clonedCache = myDataCache.CloneCache();

            clonedCache.Add(s_key1, s_value1);
            myDataCache.Add(s_key2, s_value2);
            _store.Put(s_key3.ToArray(), s_value3.ToArray());

            var items = clonedCache.Find(s_key1.ToArray());
            Assert.AreEqual(s_key1, items.ElementAt(0).Key);
            Assert.AreEqual(s_value1, items.ElementAt(0).Value);
            Assert.AreEqual(1, items.Count());

            items = clonedCache.Find(s_key2.ToArray());
            Assert.AreEqual(s_key2, items.ElementAt(0).Key);
            Assert.IsTrue(s_value2.EqualsTo(items.ElementAt(0).Value));
            Assert.AreEqual(1, items.Count());

            items = clonedCache.Find(s_key3.ToArray());
            Assert.AreEqual(s_key3, items.ElementAt(0).Key);
            Assert.IsTrue(s_value3.EqualsTo(items.ElementAt(0).Value));
            Assert.AreEqual(1, items.Count());

            items = clonedCache.Find(s_key4.ToArray());
            Assert.AreEqual(0, items.Count());
        }

        [TestMethod]
        public void TestGetInternal()
        {
            var myDataCache = new StoreCache(_store);
            var clonedCache = myDataCache.CloneCache();

            clonedCache.Add(s_key1, s_value1);
            myDataCache.Add(s_key2, s_value2);
            _store.Put(s_key3.ToArray(), s_value3.ToArray());

            Assert.IsTrue(s_value1.EqualsTo(clonedCache[s_key1]));
            Assert.IsTrue(s_value2.EqualsTo(clonedCache[s_key2]));
            Assert.IsTrue(s_value3.EqualsTo(clonedCache[s_key3]));

            void Action()
            {
                var item = clonedCache[s_key4];
            }
            Assert.ThrowsException<KeyNotFoundException>(Action);
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            var myDataCache = new StoreCache(_store);
            var clonedCache = myDataCache.CloneCache();

            clonedCache.Add(s_key1, s_value1);
            myDataCache.Add(s_key2, s_value2);
            _store.Put(s_key3.ToArray(), s_value3.ToArray());

            Assert.IsTrue(s_value1.EqualsTo(clonedCache.TryGet(s_key1)));
            Assert.IsTrue(s_value2.EqualsTo(clonedCache.TryGet(s_key2)));
            Assert.IsTrue(s_value3.EqualsTo(clonedCache.TryGet(s_key3)));
            Assert.IsNull(clonedCache.TryGet(s_key4));
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            var myDataCache = new StoreCache(_store);
            var clonedCache = myDataCache.CloneCache();

            clonedCache.Add(s_key1, s_value1);
            myDataCache.Add(s_key2, s_value2);
            _store.Put(s_key3.ToArray(), s_value3.ToArray());

            clonedCache.GetAndChange(s_key1).Value = Encoding.Default.GetBytes("value_new_1");
            clonedCache.GetAndChange(s_key2).Value = Encoding.Default.GetBytes("value_new_2");
            clonedCache.GetAndChange(s_key3).Value = Encoding.Default.GetBytes("value_new_3");

            clonedCache.Commit();

            StorageItem value_new_1 = new(Encoding.UTF8.GetBytes("value_new_1"));
            StorageItem value_new_2 = new(Encoding.UTF8.GetBytes("value_new_2"));
            StorageItem value_new_3 = new(Encoding.UTF8.GetBytes("value_new_3"));

            Assert.IsTrue(value_new_1.EqualsTo(clonedCache[s_key1]));
            Assert.IsTrue(value_new_2.EqualsTo(clonedCache[s_key2]));
            Assert.IsTrue(value_new_3.EqualsTo(clonedCache[s_key3]));
            Assert.IsTrue(value_new_2.EqualsTo(clonedCache[s_key2]));
        }

        [TestMethod]
        public void TestCacheOverrideIssue2572()
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var storages = snapshotCache.CloneCache();

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

            var res = snapshotCache.TryGet(new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 });
            Assert.AreEqual("05", res.Value.Span.ToHexString());
            storages.Commit();
            res = snapshotCache.TryGet(new StorageKey() { Key = new byte[] { 0x01, 0x01 }, Id = 0 });
            Assert.AreEqual("06", res.Value.Span.ToHexString());
        }
    }
}
