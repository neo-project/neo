using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

using BenchmarkDotNet.Attributes;

namespace Neo.Benchmarks
{
    [TestClass]
    public class BenchmarkUInt256 : BenchmarkBase
    {
        private const int MAX_TESTS = 10;

        byte[][] base_32_1;
        byte[][] base_32_2;

        private Random random;

        [TestInitialize]
        public void TestSetup()
        {
            // this is a Test class and also Benchmark class
            // it is supposed to run and verify unit tests, but also provide benchmarking
            Setup();
        }

        [GlobalSetup]
        public override void Setup()
        {
            int SEED = 123456789;
            random = new Random(SEED);

            base_32_1 = new byte[MAX_TESTS][];
            base_32_2 = new byte[MAX_TESTS][];

            for (var i = 0; i < MAX_TESTS; i++)
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
            }

            base.Setup();
        }

        private byte[] RandomBytes(int count)
        {
            byte[] randomBytes = new byte[count];
            random.NextBytes(randomBytes);
            return randomBytes;
        }

        [Benchmark(Baseline = true)]
        public void Benchmark_Official_UInt256()
        {
            // testing "official UInt256 version"
            UInt256[] uut_32_1 = new UInt256[MAX_TESTS];
            UInt256[] uut_32_2 = new UInt256[MAX_TESTS];

            for (var i = 0; i < MAX_TESTS; i++)
            {
                uut_32_1[i] = new UInt256(base_32_1[i]);
                uut_32_2[i] = new UInt256(base_32_2[i]);
            }

            for (var i = 0; i < MAX_TESTS; i++)
            {
                uut_32_1[i].CompareTo(uut_32_2[i]);
            }
        }

        [Benchmark]
        public void Benchmark_Code1_UInt256()
        {
            // testing "official UInt256 version"
            UInt256[] uut_32_1 = new UInt256[MAX_TESTS];
            UInt256[] uut_32_2 = new UInt256[MAX_TESTS];

            for (var i = 0; i < MAX_TESTS; i++)
            {
                uut_32_1[i] = new UInt256(base_32_1[i]);
                uut_32_2[i] = new UInt256(base_32_2[i]);
            }

            for (var i = 0; i < MAX_TESTS; i++)
            {
                code1_UInt256CompareTo(base_32_1[i], base_32_2[i]);
            }
        }

        [Benchmark]
        public void Benchmark_Code2_UInt256()
        {
            // testing "official UInt256 version"
            UInt256[] uut_32_1 = new UInt256[MAX_TESTS];
            UInt256[] uut_32_2 = new UInt256[MAX_TESTS];

            for (var i = 0; i < MAX_TESTS; i++)
            {
                uut_32_1[i] = new UInt256(base_32_1[i]);
                uut_32_2[i] = new UInt256(base_32_2[i]);
            }

            for (var i = 0; i < MAX_TESTS; i++)
            {
                code2_UInt256CompareTo(base_32_1[i], base_32_2[i]);
            }
        }

        [Benchmark]
        public void Benchmark_Code3_UInt256()
        {
            // testing "official UInt256 version"
            UInt256[] uut_32_1 = new UInt256[MAX_TESTS];
            UInt256[] uut_32_2 = new UInt256[MAX_TESTS];

            for (var i = 0; i < MAX_TESTS; i++)
            {
                uut_32_1[i] = new UInt256(base_32_1[i]);
                uut_32_2[i] = new UInt256(base_32_2[i]);
            }

            for (var i = 0; i < MAX_TESTS; i++)
            {
                code3_UInt256CompareTo(base_32_1[i], base_32_2[i]);
            }
        }


        [TestMethod]
        public void Benchmark_UInt256_IsCorrect_Self_CompareTo()
        {
            for (var i = 0; i < MAX_TESTS; i++)
            {
                code1_UInt256CompareTo(base_32_1[i], base_32_1[i]).Should().Be(0);
                code2_UInt256CompareTo(base_32_1[i], base_32_1[i]).Should().Be(0);
                code3_UInt256CompareTo(base_32_1[i], base_32_1[i]).Should().Be(0);
            }
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
                for (int i = 256 / 32 - 1; i >= 0; i--)
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
                for (int i = 256 / 64 - 1; i >= 0; i--)
                {
                    if (lpx[i] > lpy[i])
                        return 1;
                    if (lpx[i] < lpy[i])
                        return -1;
                }
            }
            return 0;
        }
        private int code1_UInt160CompareTo(byte[] b1, byte[] b2)
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

        private unsafe int code2_UInt160CompareTo(byte[] b1, byte[] b2)
        {
            fixed (byte* px = b1, py = b2)
            {
                uint* lpx = (uint*)px;
                uint* lpy = (uint*)py;
                for (int i = 160 / 32 - 1; i >= 0; i--)
                {
                    if (lpx[i] > lpy[i])
                        return 1;
                    if (lpx[i] < lpy[i])
                        return -1;
                }
            }
            return 0;
        }

        private unsafe int code3_UInt160CompareTo(byte[] b1, byte[] b2)
        {
            // LSB -----------------> MSB
            // --------------------------
            // | 8B      | 8B      | 4B |
            // --------------------------
            //   0l        1l        4i
            // --------------------------
            fixed (byte* px = b1, py = b2)
            {
                uint* ipx = (uint*)px;
                uint* ipy = (uint*)py;
                if (ipx[4] > ipy[4])
                    return 1;
                if (ipx[4] < ipy[4])
                    return -1;

                ulong* lpx = (ulong*)px;
                ulong* lpy = (ulong*)py;
                if (lpx[1] > lpy[1])
                    return 1;
                if (lpx[1] < lpy[1])
                    return -1;
                if (lpx[0] > lpy[0])
                    return 1;
                if (lpx[0] < lpy[0])
                    return -1;
            }
            return 0;
        }

    }
}
