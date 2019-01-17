using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Ledger;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System;
//using System.Runtime.CompilerServices.Unsafe;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_UIntBenchmarks
    {
        int MAX_TESTS = 10000000; // 1 million

        byte[][] base_32_1;
        byte[][] base_32_2;
        byte[][] base_20_1;
        byte[][] base_20_2;

        private Random random;

        [TestInitialize]
        public void TestSetup()
        {
            int SEED = 123456789;
            random = new Random(SEED);

            base_32_1 = new byte[MAX_TESTS][];
            base_32_2 = new byte[MAX_TESTS][];
            base_20_1 = new byte[MAX_TESTS][];
            base_20_2 = new byte[MAX_TESTS][];

            for(var i=0; i<MAX_TESTS; i++)
            {
                base_32_1[i] = RandomBytes(32);
                if (i % 2 == 0)
                {
                    base_32_2[i] = RandomBytes(32);
                }
                else
                {
                    base_32_2[i] = new byte[32];
                    Buffer.BlockCopy(base_32_1[i], 0, base_32_2[i], 0, 32);
                }
                base_20_1[i] = RandomBytes(20);
                base_20_2[i] = RandomBytes(20);
            }
        }

        private byte[] RandomBytes(int count)
        {
            byte[] randomBytes = new byte[count];
            random.NextBytes(randomBytes);
            return randomBytes;
        }

        public delegate Object BenchmarkMethod();

        public (TimeSpan, Object) Benchmark(BenchmarkMethod method)
        {
            Stopwatch sw0 = new Stopwatch();
            sw0.Start();
            var result = method();
            sw0.Stop();
            TimeSpan elapsed = sw0.Elapsed;
            Console.WriteLine($"Elapsed={elapsed} Sum={result}");
            return (elapsed, result);
        }

        /* Could do this also so just pass the method to benchmark, but overhead of delegate call might affect benchmark
        public delegate int ComparisonMethod(byte[] b1, byte[] b2);

        public int BechmarkComparisonMethod(ComparisonMethod compareMethod)
        {
        }
        */


        [TestMethod]
        public void Benchmark_CompareTo()
        {
            // testing "official version"
            UInt256[] uut_32_1 = new UInt256[MAX_TESTS];
            UInt256[] uut_32_2 = new UInt256[MAX_TESTS];

            for(var i=0; i<MAX_TESTS; i++)
            {
                uut_32_1[i] = new UInt256(base_32_1[i]);
                uut_32_2[i] = new UInt256(base_32_2[i]);
            }

            var checksum0 = Benchmark(() =>
            {
                var checksum = 0;
                for(var i=0; i<MAX_TESTS; i++)
                {
                    checksum += uut_32_1[i].CompareTo(uut_32_2[i]);
                }

                return checksum;
            }).Item2;

            var checksum1 = Benchmark(() =>
            {
                var checksum = 0;
                for(var i=0; i<MAX_TESTS; i++)
                {
                    checksum += code1_UInt256CompareTo(base_32_1[i], base_32_2[i]);
                }

                return checksum;
            }).Item2;

            var checksum2 = Benchmark(() =>
            {
                var checksum = 0;
                for(var i=0; i<MAX_TESTS; i++)
                {
                    checksum += code2_UInt256CompareTo(base_32_1[i], base_32_2[i]);
                }

                return checksum;
            }).Item2;

            var checksum3 = Benchmark(() =>
            {
                var checksum = 0;
                for(var i=0; i<MAX_TESTS; i++)
                {
                    checksum += code3_UInt256CompareTo(base_32_1[i], base_32_2[i]);
                }

                return checksum;
            }).Item2;

            checksum0.Should().Be(checksum1);
            checksum0.Should().Be(checksum2);
            checksum0.Should().Be(checksum3);
        }

        private int code1_UInt256CompareTo(byte[] b1, byte[] b2)
        {
            byte[] x = b1;
            byte[] y = b2;
            for (int i = x.Length - 1; i >= 0; i--)
            {
                if (x[i] > y[i])
                    return 1;
                if (x[i] < y[i])
                    return -1;
            }
            return 0;
        }

        private unsafe int code2_UInt256CompareTo(byte[] b1, byte[] b2)
        {
            fixed (byte* px = b1, py = b2)
            {
                uint* lpx = (uint*)px;
                uint* lpy = (uint*)py;
                for (int i = 7; i >= 0; i--)
                {
                    if (lpx[i] > lpy[i])
                        return 1;
                    if (lpx[i] < lpy[i])
                        return -1;
                }
            }
            return 0;
        }

        private unsafe int code3_UInt256CompareTo(byte[] b1, byte[] b2)
        {
            fixed (byte* px = b1, py = b2)
            {
                ulong* lpx = (ulong*)px;
                ulong* lpy = (ulong*)py;
                for (int i = 3; i >= 0; i--)
                {
                    if (lpx[i] > lpy[i])
                        return 1;
                    if (lpx[i] < lpy[i])
                        return -1;
                }
            }
            return 0;
        }

    }
}
