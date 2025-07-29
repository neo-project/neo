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
            using var store = new FasterDbStore(Path.GetRandomFileName(), Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            using (var snapshot = store.GetSnapshot())
            {
                var result = snapshot.TryGet([0x01, 0x00], out var value);
                CollectionAssert.AreEqual(new byte[] { 0x00 }, value);

                result = snapshot.TryGet([0x01, 0x01], out value);
                CollectionAssert.AreEqual(new byte[] { 0x01 }, value);

                result = snapshot.TryGet([0x01, 0x02], out value);
                CollectionAssert.AreEqual(new byte[] { 0x02 }, value);
            }
        }

        [TestMethod]
        public void TestPut()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName(), Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            using (var snapshot = store.GetSnapshot())
            {
                snapshot.Put([0x01, 0x01], [0xee]);
                snapshot.Commit();
            }

            CollectionAssert.AreEqual(new byte[] { 0x00 }, store.TryGet([0x01, 0x00]));
            CollectionAssert.AreEqual(new byte[] { 0xee }, store.TryGet([0x01, 0x01]));
            CollectionAssert.AreEqual(new byte[] { 0x02 }, store.TryGet([0x01, 0x02]));
        }

        [TestMethod]
        public void TestDelete()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName(), Path.GetRandomFileName());

            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x01, 0x02], [0x02]);

            using (var snapshot = store.GetSnapshot())
            {
                snapshot.Delete([0x01, 0x01]);
                snapshot.Commit();
            }

            CollectionAssert.AreEqual(new byte[] { 0x00 }, store.TryGet([0x01, 0x00]));
            Assert.IsNull(store.TryGet([0x01, 0x01]));
            CollectionAssert.AreEqual(new byte[] { 0x02 }, store.TryGet([0x01, 0x02]));
        }

        [TestMethod]
        public void TestSeek()
        {
            using var store = new FasterDbStore(Path.GetRandomFileName(), Path.GetRandomFileName());

            store.Put([0x01, 0x02], [0x02]);
            store.Put([0x02, 0x01], [0x11]);
            store.Put([0x01, 0x00], [0x00]);
            store.Put([0x01, 0x01], [0x01]);
            store.Put([0x02, 0x00], [0x10]);

            using var snapshot = store.GetSnapshot();
            store.Put([0x02, 0x01], [0x11]);

            var items = snapshot.Find([0x02], SeekDirection.Forward).ToArray();

            Assert.AreEqual(2, items.Length);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00 }, items[0].Key);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x01 }, items[1].Key);

            items = [.. snapshot.Find(null, SeekDirection.Forward)];

            Assert.AreEqual(5, items.Length);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x00 }, items[0].Key);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x01 }, items[1].Key);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02 }, items[2].Key);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x00 }, items[3].Key);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x01 }, items[4].Key);

            items = [.. snapshot.Find([0x01, 0x02], SeekDirection.Backward)];

            Assert.AreEqual(3, items.Length);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02 }, items[0].Key);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x01 }, items[1].Key);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x00 }, items[2].Key);

            items = [.. snapshot.Find([0x02], SeekDirection.Backward)];

            Assert.AreEqual(3, items.Length);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02 }, items[0].Key);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x01 }, items[1].Key);
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x00 }, items[2].Key);

            items = [.. snapshot.Find(null, SeekDirection.Backward)];

            Assert.AreEqual(0, items.Length);
        }
    }
}
