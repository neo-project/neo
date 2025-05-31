// Copyright (C) 2015-2025 The Neo Project.
//
// UT_FasterDbStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Storage;
using Neo.Persistence;
using System;
using System.IO;
using System.Linq;

namespace Neo.Build.Core.Tests.Storage
{
    [TestClass]
    public class UT_FasterDbStore
    {
        [TestMethod]
        public void TestPutAndTryGet()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            CollectionAssert.AreEqual(store.TryGet([0x01, 0x00]), new byte[] { 0x00 });
            CollectionAssert.AreEqual(store.TryGet([0x01, 0x01]), new byte[] { 0x01 });
            CollectionAssert.AreEqual(store.TryGet([0x01, 0x02]), new byte[] { 0x02 });
        }

        [TestMethod]
        public void TestDelete()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            store.Delete([0x01, 0x01]);

            CollectionAssert.AreEqual(store.TryGet([0x01, 0x00]), new byte[] { 0x00 });
            CollectionAssert.AreEqual(store.TryGet([0x01, 0x01]), null);
            CollectionAssert.AreEqual(store.TryGet([0x01, 0x02]), new byte[] { 0x02 });
        }

        [TestMethod]
        public void TestSeek()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName());

            store.Put([0x01, 0x02], [0x02]);
            store.Put([0x02, 0x01], [0x11]);
            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x02, 0x00], [0x10]);

            var items = store.Seek([0x02], SeekDirection.Forward).ToArray();

            Assert.AreEqual(2, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x02, 0x00 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x02, 0x01 });

            items = [.. store.Seek(null, SeekDirection.Forward)];

            Assert.AreEqual(5, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x01, 0x00 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x01, 0x01 });
            CollectionAssert.AreEqual(items[2].Key, new byte[] { 0x01, 0x02 });
            CollectionAssert.AreEqual(items[3].Key, new byte[] { 0x02, 0x00 });
            CollectionAssert.AreEqual(items[4].Key, new byte[] { 0x02, 0x01 });

            items = [.. store.Seek([0x01, 0x02], SeekDirection.Backward)];

            Assert.AreEqual(3, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x01, 0x02 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x01, 0x01 });
            CollectionAssert.AreEqual(items[2].Key, new byte[] { 0x01, 0x00 });

            items = [.. store.Seek([0x02], SeekDirection.Backward)];

            Assert.AreEqual(3, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x01, 0x02 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x01, 0x01 });
            CollectionAssert.AreEqual(items[2].Key, new byte[] { 0x01, 0x00 });

            items = [.. store.Seek(null, SeekDirection.Backward)];

            Assert.AreEqual(0, items.Length);
        }

        [TestMethod]
        public void TestCreateFullCheckPoint()
        {
            var checkpointId = Guid.Empty;

            using (var store = new FasterDbStore("chkpntTest"))
            {
                store.Put([0x01, 0x00], [0x00]);
                store.Put([0x01, 0x01], [0x01]);
                store.Put([0x01, 0x02], [0x02]);

                checkpointId = store.CreateFullCheckPoint();

                store.Put([0x01, 0xff], [0xff]);

                CollectionAssert.AreEqual(store.TryGet([0x01, 0xff]), new byte[] { 0xff });
                CollectionAssert.AreEqual(store.TryGet([0x01, 0x00]), new byte[] { 0x00 });
                CollectionAssert.AreEqual(store.TryGet([0x01, 0x01]), new byte[] { 0x01 });
                CollectionAssert.AreEqual(store.TryGet([0x01, 0x02]), new byte[] { 0x02 });
            }

            using (var store = new FasterDbStore("chkpntTest", checkpointId))
            {
                CollectionAssert.AreEqual(store.TryGet([0x01, 0x00]), new byte[] { 0x00 });
                CollectionAssert.AreEqual(store.TryGet([0x01, 0x01]), new byte[] { 0x01 });
                CollectionAssert.AreEqual(store.TryGet([0x01, 0x02]), new byte[] { 0x02 });
            }
        }
    }
}
