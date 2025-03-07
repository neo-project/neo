// Copyright (C) 2015-2025 The Neo Project.
//
// UT_FasterDbSnapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Storage;
using Neo.Persistence;
using System.IO;
using System.Linq;

namespace Neo.Build.Core.Tests.Storage
{
    [TestClass]
    public class UT_FasterDbSnapshot
    {
        [TestMethod]
        public void TestTryGet()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            using (var snapshot = store.GetSnapshot())
            {
                var result = snapshot.TryGet([0x01, 0x00], out var value);
                CollectionAssert.AreEqual(value, new byte[] { 0x00 });

                result = snapshot.TryGet([0x01, 0x01], out value);
                CollectionAssert.AreEqual(value, new byte[] { 0x01 });

                result = snapshot.TryGet([0x01, 0x02], out value);
                CollectionAssert.AreEqual(value, new byte[] { 0x02 });
            }
        }

        [TestMethod]
        public void TestPut()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            using (var snapshot = store.GetSnapshot())
            {
                snapshot.Put([0x01, 0x01], [0xee]);
                snapshot.Commit();
            }

            CollectionAssert.AreEqual(store.TryGet([0x01, 0x00]), new byte[] { 0x00 });
            CollectionAssert.AreEqual(store.TryGet([0x01, 0x01]), new byte[] { 0xee });
            CollectionAssert.AreEqual(store.TryGet([0x01, 0x02]), new byte[] { 0x02 });
        }

        [TestMethod]
        public void TestDelete()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            using (var snapshot = store.GetSnapshot())
            {
                snapshot.Delete([0x01, 0x01]);
                snapshot.Commit();
            }

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

            using var snapshot = store.GetSnapshot();
            var items = snapshot.Seek([0x02], SeekDirection.Forward).ToArray();

            store.Put([0x02, 0x01], [0x11]);

            Assert.AreEqual(2, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x02, 0x00 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x02, 0x01 });

            items = [.. snapshot.Seek(null, SeekDirection.Forward)];

            Assert.AreEqual(5, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x01, 0x00 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x01, 0x01 });
            CollectionAssert.AreEqual(items[2].Key, new byte[] { 0x01, 0x02 });
            CollectionAssert.AreEqual(items[3].Key, new byte[] { 0x02, 0x00 });
            CollectionAssert.AreEqual(items[4].Key, new byte[] { 0x02, 0x01 });

            items = [.. snapshot.Seek([0x01, 0x02], SeekDirection.Backward)];

            Assert.AreEqual(3, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x01, 0x02 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x01, 0x01 });
            CollectionAssert.AreEqual(items[2].Key, new byte[] { 0x01, 0x00 });

            items = [.. snapshot.Seek([0x02], SeekDirection.Backward)];

            Assert.AreEqual(3, items.Length);
            CollectionAssert.AreEqual(items[0].Key, new byte[] { 0x01, 0x02 });
            CollectionAssert.AreEqual(items[1].Key, new byte[] { 0x01, 0x01 });
            CollectionAssert.AreEqual(items[2].Key, new byte[] { 0x01, 0x00 });

            items = [.. snapshot.Seek(null, SeekDirection.Backward)];

            Assert.AreEqual(0, items.Length);
        }
    }
}
