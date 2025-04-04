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
using Neo.Extensions;
using Neo.IO.Caching;
using System;
using System.Collections;
using System.Linq;
using System.Numerics;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_HashSetUInt256Cache
    {
        private static UInt256 ToHash(byte num)
        {
            var hash = new byte[32];
            hash[0] = (byte)num;
            return new UInt256(hash);
        }

        [TestMethod]
        public void TestHashSetCache()
        {
            var bucket = new HashSetUInt256Cache(10);
            for (var i = 1; i <= 100; i++)
            {
                var hash = ToHash((byte)i);

                Assert.IsTrue(bucket.Add(hash));
                Assert.IsFalse(bucket.Add(hash));
            }
            Assert.AreEqual(100, bucket.Count);

            BigInteger sum = 0;
            foreach (var ele in bucket)
            {
                sum += new BigInteger(ele.ToArray());
            }
            Assert.AreEqual(5050, sum);

            bucket.Add(ToHash(101));
            Assert.AreEqual(91, bucket.Count);

            var items = new UInt256[10];
            byte value = 11;
            for (var i = 0; i < 10; i++)
            {
                items[i] = ToHash(value);
                value += 2;
            }
            bucket.ExceptWith(items);
            Assert.AreEqual(81, bucket.Count);

            Assert.IsFalse(bucket.Contains(ToHash(13)));
            Assert.IsTrue(bucket.Contains(ToHash(50)));
        }

        [TestMethod]
        public void TestConstructor()
        {
            Action action1 = () => new HashSetUInt256Cache(-1);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => action1());

            Action action2 = () => new HashSetUInt256Cache(1, -1);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => action2());
        }

        [TestMethod]
        public void TestAdd()
        {
            var a = new UInt256([
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            ]);
            var b = new UInt256([
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x02
            ]);
            var set = new HashSetUInt256Cache(1, 1)
            {
                a,
                b
            };
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { b });
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            var a = new UInt256([
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            ]);
            var b = new UInt256([
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x02
            ]);
            var set = new HashSetUInt256Cache(1, 1)
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
            var a = new UInt256([
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            ]);
            var b = new UInt256([
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x02
            ]);
            var c = new UInt256([
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x03
            ]);

            var set = new HashSetUInt256Cache(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith([b, c]);
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a });
            set.ExceptWith([a]);
            CollectionAssert.AreEqual(set.ToArray(), Array.Empty<UInt256>());

            set = new HashSetUInt256Cache(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith([a]);
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { b, c });

            set = new HashSetUInt256Cache(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith([c]);
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a, b });
        }
    }
}
