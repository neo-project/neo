// Copyright (C) 2015-2025 The Neo Project.
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
            var bucket = new HashSetCache<int>(10);
            for (int i = 1; i <= 100; i++)
            {
                Assert.IsTrue(bucket.Add(i));
                Assert.IsFalse(bucket.Add(i));
            }
            Assert.AreEqual(100, bucket.Count);

            int sum = 0;
            foreach (var ele in bucket)
            {
                sum += ele;
            }
            Assert.AreEqual(5050, sum);

            bucket.Add(101);
            Assert.AreEqual(91, bucket.Count);

            var items = new int[10];
            var value = 11;
            for (int i = 0; i < 10; i++)
            {
                items[i] = value;
                value += 2;
            }
            bucket.ExceptWith(items);
            Assert.AreEqual(81, bucket.Count);

            Assert.IsFalse(bucket.Contains(13));
            Assert.IsTrue(bucket.Contains(50));
        }

        [TestMethod]
        public void TestConstructor()
        {
            Action action1 = () => new HashSetCache<UInt256>(-1);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => action1());

            Action action2 = () => new HashSetCache<UInt256>(1, -1);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => action2());
        }

        [TestMethod]
        public void TestAdd()
        {
            var a = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            });
            var b = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x02
            });
            var set = new HashSetCache<UInt256>(1, 1)
            {
                a,
                b
            };
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { b });
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            var a = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            });
            var b = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x02
            });
            var set = new HashSetCache<UInt256>(1, 1)
            {
                a,
                b
            };
            IEnumerable ie = set;
            Assert.IsNotNull(ie.GetEnumerator());
        }

        [TestMethod]
        public void TestExceptWith()
        {
            var a = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            });
            var b = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x02
            });
            var c = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x03
            });

            var set = new HashSetCache<UInt256>(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith(new UInt256[] { b, c });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a });
            set.ExceptWith(new UInt256[] { a });
            CollectionAssert.AreEqual(set.ToArray(), Array.Empty<UInt256>());

            set = new HashSetCache<UInt256>(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith(new UInt256[] { a });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { b, c });

            set = new HashSetCache<UInt256>(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith(new UInt256[] { c });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a, b });
        }
    }
}
