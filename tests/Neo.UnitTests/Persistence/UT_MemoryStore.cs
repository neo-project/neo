// Copyright (C) 2015-2024 The Neo Project.
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
        public void NeoSystemTest()
        {
            var neoSystem = new NeoSystem(TestProtocolSettings.Default, new MemoryStoreProvider());
            Assert.IsNotNull(neoSystem.StoreView);
            var snapshot = neoSystem.StoreView;
            var key = new StorageKey( Encoding.UTF8.GetBytes("testKey"));
            var value =new StorageItem( Encoding.UTF8.GetBytes("testValue"));
            snapshot.Add(key, value);
            snapshot.Commit();
            var result = snapshot.TryGet(key);
            Assert.AreEqual("testValue", Encoding.UTF8.GetString(result.Value.ToArray()));
        }

    }
}
