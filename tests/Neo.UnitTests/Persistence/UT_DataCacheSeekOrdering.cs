// Copyright (C) 2015-2026 The Neo Project.
//
// UT_DataCacheSeekOrdering.cs file belongs to the neo project and is free
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
using System.Linq;
using System.Threading.Tasks;

namespace Neo.UnitTests.Persistence
{
    [TestClass]
    public class UT_DataCacheSeekOrdering
    {
        [TestMethod]
        public void ConcurrentColdSeeksPreserveKeyOrdering()
        {
            using var store = new MemoryStore();
            using var cache = new StoreCache(store, false);
            // Do not serialize keys before the readers start: the lazy key buffers are cold.
            for (int i = 255; i >= 0; i--)
                cache.Add(new StorageKey { Id = 1, Key = new byte[] { (byte)i } }, new StorageItem(new byte[] { (byte)i }));
            byte[] expected = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            Parallel.For(0, 16, _ =>
            {
                var actual = cache.Seek().Select(p => p.Key.Key.Span[0]).ToArray();
                Assert.AreSequenceEqual(expected, actual);
            });
        }

        [TestMethod]
        public void ActiveEnumeratorRetainsCapturedEntries()
        {
            using var store = new MemoryStore();
            using var cache = new StoreCache(store, false);
            var first = new StorageKey { Id = 1, Key = new byte[] { 1 } };
            var second = new StorageKey { Id = 1, Key = new byte[] { 2 } };
            cache.Add(second, new StorageItem(new byte[] { 2 }));
            cache.Add(first, new StorageItem(new byte[] { 1 }));
            using var iterator = cache.Seek().GetEnumerator();
            Assert.IsTrue(iterator.MoveNext());
            Assert.AreEqual(first, iterator.Current.Key);
            cache.Delete(second);
            cache.Add(second, new StorageItem(new byte[] { 20 }));
            Assert.IsTrue(iterator.MoveNext());
            Assert.AreEqual((byte)2, iterator.Current.Value.Value.Span[0]);
            Assert.IsFalse(iterator.MoveNext());
            Assert.AreSequenceEqual(new byte[] { 1, 20 }, cache.Seek().Select(p => p.Value.Value.Span[0]).ToArray());
        }

        [TestMethod]
        [DataRow(SeekDirection.Forward, 0)]
        [DataRow(SeekDirection.Forward, 2)]
        [DataRow(SeekDirection.Forward, 20)]
        [DataRow(SeekDirection.Backward, 0)]
        [DataRow(SeekDirection.Backward, 2)]
        [DataRow(SeekDirection.Backward, 20)]
        public void SeekPreservesLayeredMergeAndSkip(SeekDirection direction, int skip)
        {
            using var store = new MemoryStore();
            StorageKey Key(byte i) => new() { Id = 1, Key = new byte[] { i } };
            foreach (byte i in new byte[] { 1, 3, 5, 7 })
                store.Put(Key(i).ToArray(), new byte[] { i });
            using var cache = new StoreCache(store, false);
            cache.Add(Key(6), new StorageItem(new byte[] { 6 }));
            cache.Add(Key(2), new StorageItem(new byte[] { 2 }));
            cache.Delete(Key(3));
            cache.GetAndChange(Key(5)).Value = new byte[] { 50 };
            var child = cache.CloneCache();
            child.Add(Key(4), new StorageItem(new byte[] { 4 }));
            child.Delete(Key(6));
            var expected = direction == SeekDirection.Forward
                ? new byte[] { 1, 2, 4, 50, 7 }
                : new byte[] { 7, 50, 4, 2, 1 };
            var start = Key(direction == SeekDirection.Forward ? (byte)0 : (byte)255).ToArray();
            var actual = child.Seek(start, direction, skip).Select(p => p.Value.Value.Span[0]).ToArray();
            Assert.AreSequenceEqual(expected.Skip(skip).ToArray(), actual);
        }
    }
}
