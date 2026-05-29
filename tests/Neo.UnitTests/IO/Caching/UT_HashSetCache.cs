// Copyright (C) 2015-2026 The Neo Project.
//
// UT_HashSetCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System;
using System.Collections;
using System.Linq;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_HashSetCache
    {
        [TestMethod]
        public void TestHashSetCache()
        {
            var bucket = new HashSetCache<int>(100);
            for (var i = 1; i <= 100; i++)
            {
                Assert.IsTrue(bucket.TryAdd(i));
                Assert.IsFalse(bucket.TryAdd(i));
            }
            Assert.HasCount(100, bucket);

            var sum = 0;
            foreach (var ele in bucket)
            {
                sum += ele;
            }
            Assert.AreEqual(5050, sum);

            bucket.TryAdd(101);
            Assert.HasCount(100, bucket);

            var items = new int[10];
            var value = 11;
            for (var i = 0; i < 10; i++)
            {
                items[i] = value;
                value += 2;
            }
            bucket.ExceptWith(items);
            Assert.HasCount(90, bucket);

            Assert.DoesNotContain(13, bucket);
            Assert.Contains(50, bucket);
        }

        [TestMethod]
        public void TestConstructor()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashSetCache<UInt256>(-1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new HashSetCache<UInt256>(-1));
        }

        [TestMethod]
        public void TestAdd()
        {
            var key1 = Enumerable.Repeat((byte)1, 32).ToArray();
            var a = new UInt256(key1);

            var key2 = Enumerable.Repeat((byte)1, 31).Append((byte)2).ToArray();
            var b = new UInt256(key2);

            var set = new HashSetCache<UInt256>(1);
            Assert.IsTrue(set.TryAdd(a));
            Assert.IsTrue(set.TryAdd(b));
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { b });
        }

        [TestMethod]
        public void TestCopyTo()
        {
            var key1 = Enumerable.Repeat((byte)1, 32).ToArray();
            var a = new UInt256(key1);

            var key2 = Enumerable.Repeat((byte)1, 31).Append((byte)2).ToArray();
            var b = new UInt256(key2);

            var set = new HashSetCache<UInt256>(1);
            Assert.IsTrue(set.TryAdd(a));
            Assert.IsTrue(set.TryAdd(b));

            var array = new UInt256[1];
            set.CopyTo(array, 0);

            CollectionAssert.AreEqual(array, new UInt256[] { b });
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            var key1 = Enumerable.Repeat((byte)1, 32).ToArray();
            var a = new UInt256(key1);

            var key2 = Enumerable.Repeat((byte)1, 31).Append((byte)2).ToArray();
            var b = new UInt256(key2);

            var set = new HashSetCache<UInt256>(1);
            set.TryAdd(a);
            set.Add(b);
            IEnumerable ie = set;
            Assert.IsNotNull(ie.GetEnumerator());
        }

        [TestMethod]
        public void TestExceptWith()
        {
            var key1 = Enumerable.Repeat((byte)1, 32).ToArray();
            var a = new UInt256(key1);

            var key2 = Enumerable.Repeat((byte)1, 31).Append((byte)2).ToArray();
            var b = new UInt256(key2);

            var key3 = Enumerable.Repeat((byte)1, 31).Append((byte)3).ToArray();
            var c = new UInt256(key3);

            var set = new HashSetCache<UInt256>(10);
            set.TryAdd(a);
            set.TryAdd(b);
            set.TryAdd(c);
            set.ExceptWith([b, c]);
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a });

            set.Remove(a);
            CollectionAssert.AreEqual(set.ToArray(), Array.Empty<UInt256>());

            set = new HashSetCache<UInt256>(10);
            set.TryAdd(a);
            set.TryAdd(b);
            set.TryAdd(c);
            set.ExceptWith([a]);
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { b, c });

            set = new HashSetCache<UInt256>(10);
            set.TryAdd(a);
            set.TryAdd(b);
            set.TryAdd(c);
            set.ExceptWith([c]);
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a, b });
        }

        [TestMethod]
        public void TestPrune()
        {
            var cache = new HashSetCache<int>(100, () => DateTime.UtcNow)
            {
                // Add elements at different timestamps
                1,
                2,
                3,
                4,
                5
            };

            // Wait to create a time difference
            System.Threading.Thread.Sleep(100);
            var pruneTime = DateTime.UtcNow;
            System.Threading.Thread.Sleep(100);

            // Add more elements after prune time
            cache.Add(6);
            cache.Add(7);
            cache.Add(8);

            Assert.HasCount(8, cache);

            // Prune old elements (first 5)
            cache.Prune(pruneTime);

            // Verify only elements added after prune time remain
            Assert.HasCount(3, cache);
            Assert.Contains(6, cache);
            Assert.Contains(7, cache);
            Assert.Contains(8, cache);
            Assert.DoesNotContain(1, cache);
            Assert.DoesNotContain(2, cache);
            Assert.DoesNotContain(3, cache);
            Assert.DoesNotContain(4, cache);
            Assert.DoesNotContain(5, cache);
        }

        [TestMethod]
        public void TestPruneAll()
        {
            var cache = new HashSetCache<int>(100, () => DateTime.UtcNow)
            {
                1,
                2,
                3
            };

            Assert.HasCount(3, cache);

            // Prune all elements (future date)
            cache.Prune(DateTime.UtcNow.AddHours(1));

            Assert.IsEmpty(cache);
        }

        [TestMethod]
        public void TestPruneNone()
        {
            var cache = new HashSetCache<int>(100, () => DateTime.UtcNow)
            {
                1,
                2,
                3
            };

            Assert.HasCount(3, cache);

            // Prune nothing (past date)
            cache.Prune(DateTime.UtcNow.AddHours(-1));

            Assert.HasCount(3, cache);
            Assert.Contains(1, cache);
            Assert.Contains(2, cache);
            Assert.Contains(3, cache);
        }
    }
}
