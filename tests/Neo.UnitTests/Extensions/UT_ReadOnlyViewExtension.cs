// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ReadOnlyViewExtension.cs file belongs to the neo project and is free
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
using System.Linq;

namespace Neo.UnitTests.Extensions
{
    [TestClass]
    public class UT_ReadOnlyViewExtension
    {
        [TestMethod]
        public void TestScan()
        {
            using var store = new MemoryStore();

            var key1 = StorageKey.CreateSearchPrefix(1, [1]);
            var key2 = StorageKey.CreateSearchPrefix(1, [2]);
            var key3 = StorageKey.CreateSearchPrefix(2, [3]);
            var key4 = StorageKey.CreateSearchPrefix(2, [4]);

            store.Put(key1, [1, 2, 3]);
            store.Put(key2, [4, 5, 6]);
            store.Put(key3, [7, 8, 9]);
            store.Put(key4, [10, 11, 12]);

            var view = new ReadOnlyStoreView(store);
            var items = view.ScanPrefix(StorageKey.CreateSearchPrefix(1, [])).ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.IsTrue(items[0].Key.ToArray().SequenceEqual(key1.ToArray()));
            Assert.IsTrue(items[0].Value.ToArray().SequenceEqual(new byte[] { 1, 2, 3 }));
            Assert.IsTrue(items[1].Key.ToArray().SequenceEqual(key2.ToArray()));
            Assert.IsTrue(items[1].Value.ToArray().SequenceEqual(new byte[] { 4, 5, 6 }));

            items = view.ScanPrefix(StorageKey.CreateSearchPrefix(1, []), SeekDirection.Backward).ToArray();
            Assert.AreEqual(2, items.Length);
            Assert.IsTrue(items[0].Key.ToArray().SequenceEqual(key2.ToArray()));
            Assert.IsTrue(items[0].Value.ToArray().SequenceEqual(new byte[] { 4, 5, 6 }));
            Assert.IsTrue(items[1].Key.ToArray().SequenceEqual(key1.ToArray()));
            Assert.IsTrue(items[1].Value.ToArray().SequenceEqual(new byte[] { 1, 2, 3 }));

            items = view.ScanRange(key1, key4).ToArray();
            Assert.AreEqual(3, items.Length);
            Assert.IsTrue(items[0].Key.ToArray().SequenceEqual(key1.ToArray()));
            Assert.IsTrue(items[0].Value.ToArray().SequenceEqual(new byte[] { 1, 2, 3 }));
            Assert.IsTrue(items[1].Key.ToArray().SequenceEqual(key2.ToArray()));
            Assert.IsTrue(items[1].Value.ToArray().SequenceEqual(new byte[] { 4, 5, 6 }));
            Assert.IsTrue(items[2].Key.ToArray().SequenceEqual(key3.ToArray()));
            Assert.IsTrue(items[2].Value.ToArray().SequenceEqual(new byte[] { 7, 8, 9 }));

            items = view.ScanRange(key4, key1, SeekDirection.Backward).ToArray();
            Assert.AreEqual(3, items.Length);
            Assert.IsTrue(items[0].Key.ToArray().SequenceEqual(key4.ToArray()));
            Assert.IsTrue(items[0].Value.ToArray().SequenceEqual(new byte[] { 10, 11, 12 }));
            Assert.IsTrue(items[1].Key.ToArray().SequenceEqual(key3.ToArray()));
            Assert.IsTrue(items[1].Value.ToArray().SequenceEqual(new byte[] { 7, 8, 9 }));
            Assert.IsTrue(items[2].Key.ToArray().SequenceEqual(key2.ToArray()));
            Assert.IsTrue(items[2].Value.ToArray().SequenceEqual(new byte[] { 4, 5, 6 }));

            // ScanPrefix with all 0xff and bacword is ok.
            var key5 = StorageKey.CreateSearchPrefix(-1, [5]);
            store.Put(key5, [0xf1]);
            items = view.ScanPrefix([0xff], SeekDirection.Backward).ToArray();
            Assert.AreEqual(1, items.Length);
            Assert.IsTrue(items[0].Key.ToArray().SequenceEqual(key5.ToArray()));
            Assert.IsTrue(items[0].Value.ToArray().SequenceEqual(new byte[] { 0xf1 }));
        }

        [TestMethod]
        public void TestFindEmptyPrefix()
        {
            using var store = new MemoryStore();
            using var dataCache = new SnapshotCache(store);

            var k1 = StorageKey.CreateSearchPrefix(-1, []);
            var k2 = StorageKey.CreateSearchPrefix(-1, [0x01]);
            var k3 = StorageKey.CreateSearchPrefix(-1, [0xff, 0x02]);

            dataCache.Add(k1, new StorageItem([1, 2, 3]));
            dataCache.Add(k2, new StorageItem([4, 5, 6]));
            dataCache.Add(k3, new StorageItem([7, 8, 9]));

            var items = dataCache.Find().ToArray();
            Assert.AreEqual(3, items.Length);
            Assert.IsTrue(items[0].Key.ToArray().SequenceEqual(k1.ToArray()));
            Assert.IsTrue(items[1].Key.ToArray().SequenceEqual(k2.ToArray()));
            Assert.IsTrue(items[2].Key.ToArray().SequenceEqual(k3.ToArray()));

            // null and empty are not supported for backwards direction now.
            Action action = () => dataCache.Find(null, SeekDirection.Backward);
            Assert.ThrowsException<ArgumentNullException>(action);

            action = () => dataCache.Find(new byte[] { }, SeekDirection.Backward);
            Assert.ThrowsException<ArgumentOutOfRangeException>(action);

            action = () => dataCache.Find([0xff], SeekDirection.Backward).ToArray();
            Assert.ThrowsException<NotSupportedException>(action);
        }
    }
}

