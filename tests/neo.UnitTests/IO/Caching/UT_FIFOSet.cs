using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_FIFOSet
    {
        [TestMethod]
        public void FIFOSetTest()
        {
            var a = UInt256.Zero;
            var b = new UInt256();
            var c = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01
            });

            var set = new FIFOSet<UInt256>(3);

            Assert.IsTrue(set.Add(a));
            Assert.IsFalse(set.Add(a));
            Assert.IsFalse(set.Add(b));
            Assert.IsTrue(set.Add(c));

            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a, c });

            var d = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x02
            });

            // Testing Fifo max size
            Assert.IsTrue(set.Add(d));
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a, c, d });

            var e = new UInt256(new byte[32] {
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x03
            });

            Assert.IsTrue(set.Add(e));
            Assert.IsFalse(set.Add(e));
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { c, d, e });
        }

        [TestMethod]
        public void TestConstructor()
        {
            Action action1 = () => new FIFOSet<UInt256>(-1);
            action1.Should().Throw<ArgumentOutOfRangeException>();

            Action action2 = () => new FIFOSet<UInt256>(1, -1);
            action2.Should().Throw<ArgumentOutOfRangeException>();

            Action action3 = () => new FIFOSet<UInt256>(1, 2);
            action3.Should().Throw<ArgumentOutOfRangeException>();
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
            var set = new FIFOSet<UInt256>(1, 1)
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
            var set = new FIFOSet<UInt256>(1, 1)
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

            var set = new FIFOSet<UInt256>(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith(new UInt256[] { b, c });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a });

            set = new FIFOSet<UInt256>(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith(new UInt256[] { a });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { b, c });

            set = new FIFOSet<UInt256>(10)
            {
                a,
                b,
                c
            };
            set.ExceptWith(new UInt256[] { c });
            CollectionAssert.AreEqual(set.ToArray(), new UInt256[] { a, b });
        }

        [TestMethod]
        public void TestFIFOSet()
        {
            Stopwatch stopwatch = new Stopwatch();
            var bucket = new FIFOSet<int>(150_000);
            stopwatch.Start();
            for (int i = 1; i <= 550_000; i++)
            {
                bucket.Add(i);
            }
            stopwatch.Stop();
            Console.WriteLine($"Add timespan: {stopwatch.Elapsed.TotalSeconds}s");
            stopwatch.Reset();
            var items = new int[10000];
            var value = 550_000;
            for (int i = 0; i <= 9999; i++)
            {
                items[i] = value;
                value -= 50;
            }
            stopwatch.Start();
            bucket.ExceptWith(items);
            stopwatch.Stop();
            Console.WriteLine($"except with timespan: {stopwatch.Elapsed.TotalSeconds}s");
            stopwatch.Reset();

            stopwatch.Start();
            var ret = bucket.Contains(140_000);
            stopwatch.Stop();
            Console.WriteLine($"contains with timespan: {stopwatch.Elapsed.TotalSeconds}s result: {ret}");
            stopwatch.Reset();

            stopwatch.Start();
            ret = bucket.Contains(545_001);
            stopwatch.Stop();
            Console.WriteLine($"contains with timespan: {stopwatch.Elapsed.TotalSeconds}s result: {ret}");
            stopwatch.Reset();
        }

        [TestMethod]
        public void TestHashSetCache()
        {
            Stopwatch stopwatch = new Stopwatch();
            var bucket = new HashSetCache<int>(15_000);
            stopwatch.Start();
            for (int i = 1; i <= 550_000; i++)
            {
                bucket.Add(i);
            }
            stopwatch.Stop();
            Console.WriteLine($"Add timespan: {stopwatch.Elapsed.TotalSeconds}s");
            stopwatch.Reset();
            var items = new int[10000];
            var value = 1;
            for (int i = 0; i <= 9999; i++)
            {
                items[i] = value;
                value -= 50;
            }

            stopwatch.Start();
            bucket.ExceptWith(items);
            stopwatch.Stop();
            Console.WriteLine($"except with timespan: {stopwatch.Elapsed.TotalSeconds}s");
            stopwatch.Reset();

            stopwatch.Start();
            var ret = bucket.Contains(140_000);
            stopwatch.Stop();
            Console.WriteLine($"contains with timespan: {stopwatch.Elapsed.TotalSeconds}s result: {ret}");
            stopwatch.Reset();

            stopwatch.Start();
            ret = bucket.Contains(545_001);
            stopwatch.Stop();
            Console.WriteLine($"contains with timespan: {stopwatch.Elapsed.TotalSeconds}s result: {ret}");
            stopwatch.Reset();
        }
    }
}
