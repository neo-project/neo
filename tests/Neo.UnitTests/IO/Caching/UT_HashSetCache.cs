using System;
using System.Collections;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;

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
            bucket.Count.Should().Be(100);

            int sum = 0;
            foreach (var ele in bucket)
            {
                sum += ele;
            }
            sum.Should().Be(5050);

            bucket.Add(101);
            bucket.Count.Should().Be(91);

            var items = new int[10];
            var value = 11;
            for (int i = 0; i < 10; i++)
            {
                items[i] = value;
                value += 2;
            }
            bucket.ExceptWith(items);
            bucket.Count.Should().Be(81);

            bucket.Contains(13).Should().BeFalse();
            bucket.Contains(50).Should().BeTrue();
        }

        [TestMethod]
        public void TestConstructor()
        {
            Action action1 = () => new HashSetCache<UInt256>(-1);
            action1.Should().Throw<ArgumentOutOfRangeException>();

            Action action2 = () => new HashSetCache<UInt256>(1, -1);
            action2.Should().Throw<ArgumentOutOfRangeException>();
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
            ie.GetEnumerator().Should().NotBeNull();
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
