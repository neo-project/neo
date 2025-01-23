// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MemoryStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_MemoryStore
    {
        private NeoSystem _neoSystem;
        private MemoryStore _memoryStore;

        [TestInitialize]
        public void Setup()
        {
            _memoryStore = new MemoryStore();
            _neoSystem = new NeoSystem(TestProtocolSettings.Default, new TestMemoryStoreProvider(_memoryStore));
        }

        [TestCleanup]
        public void CleanUp()
        {
            _memoryStore.Reset();
        }

        [TestMethod]
        public void LoadStoreTest()
        {
            Assert.IsInstanceOfType<MemoryStore>(TestBlockchain.TheNeoSystem.LoadStore("abc"));
        }

        [TestMethod]
        public void StoreTest()
        {
            using var store = new MemoryStore();

            store.Delete([1]);
            Assert.AreEqual(null, store.TryGet([1]));
            Assert.IsFalse(store.TryGet([1], out var got));
            Assert.AreEqual(null, got);

            store.Put([1], [1, 2, 3]);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, store.TryGet([1]));

            store.Put([2], [4, 5, 6]);
            CollectionAssert.AreEqual(new byte[] { 1 }, store.Seek(Array.Empty<byte>()).Select(u => u.Key).First());
            CollectionAssert.AreEqual(new byte[] { 2 }, store.Seek([2], SeekDirection.Backward).Select(u => u.Key).First());
            CollectionAssert.AreEqual(new byte[] { 1 }, store.Seek([1], SeekDirection.Backward).Select(u => u.Key).First());

            store.Delete([1]);
            store.Delete([2]);

            store.Put([0x00, 0x00, 0x00], [0x00]);
            store.Put([0x00, 0x00, 0x01], [0x01]);
            store.Put([0x00, 0x00, 0x02], [0x02]);
            store.Put([0x00, 0x00, 0x03], [0x03]);
            store.Put([0x00, 0x00, 0x04], [0x04]);

            var entries = store.Seek(Array.Empty<byte>(), SeekDirection.Backward).ToArray();
            Assert.AreEqual(entries.Length, 0);
        }

        [TestMethod]
        public void NeoSystemStoreViewTest()
        {
            Assert.IsNotNull(_neoSystem.StoreView);
            var store = _neoSystem.StoreView;
            var key = new StorageKey(Encoding.UTF8.GetBytes("testKey"));
            var value = new StorageItem(Encoding.UTF8.GetBytes("testValue"));

            store.Add(key, value);
            store.Commit();

            var result = store.TryGet(key);
            // The StoreView is a readonly view of the store, here it will have value in the cache
            Assert.AreEqual("testValue", Encoding.UTF8.GetString(result.Value.ToArray()));

            // But the value will not be written to the underlying store even its committed.
            Assert.IsNull(_memoryStore.TryGet(key.ToArray()));
            Assert.IsFalse(_memoryStore.TryGet(key.ToArray(), out var got));
            Assert.AreEqual(null, got);
        }

        [TestMethod]
        public void NeoSystemStoreAddTest()
        {
            var storeCache = _neoSystem.GetSnapshotCache();
            var key = new KeyBuilder(0, 0);
            storeCache.Add(key, new StorageItem(UInt256.Zero.ToArray()));
            storeCache.Commit();

            CollectionAssert.AreEqual(UInt256.Zero.ToArray(), storeCache.TryGet(key).ToArray());
        }

        [TestMethod]
        public void NeoSystemStoreGetAndChange()
        {
            var storeView = _neoSystem.GetSnapshotCache();
            var key = new KeyBuilder(1, 1);
            var item = new StorageItem([1, 2, 3]);
            storeView.Delete(key);
            Assert.AreEqual(null, storeView.TryGet(key));
            storeView.Add(key, item);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, storeView.TryGet(key).ToArray());

            var key2 = new KeyBuilder(1, 2);
            var item2 = new StorageItem([4, 5, 6]);
            storeView.Add(key2, item2);
            CollectionAssert.AreEqual(key2.ToArray(), storeView.Seek(key2.ToArray(), SeekDirection.Backward).Select(u => u.Key).First().ToArray());
            CollectionAssert.AreEqual(key.ToArray(), storeView.Seek(key.ToArray(), SeekDirection.Backward).Select(u => u.Key).First().ToArray());

            storeView.Delete(key);
            storeView.Delete(key2);

            storeView.Add(new KeyBuilder(1, 0x000000), new StorageItem([0x00]));
            storeView.Add(new KeyBuilder(1, 0x000001), new StorageItem([0x01]));
            storeView.Add(new KeyBuilder(1, 0x000002), new StorageItem([0x02]));
            storeView.Add(new KeyBuilder(1, 0x000003), new StorageItem([0x03]));
            storeView.Add(new KeyBuilder(1, 0x000004), new StorageItem([0x04]));

            var entries = storeView.Seek([], SeekDirection.Backward).ToArray();
            // Memory store has different seek behavior than the snapshot
            Assert.AreEqual(entries.Length, 38);
        }
    }
}
