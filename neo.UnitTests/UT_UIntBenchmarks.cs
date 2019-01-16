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
        int MAX_TESTS = 1000000; // 1 million

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
                base_32_2[i] = RandomBytes(32);
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
            Stopwatch sw0 = new Stopwatch();
            sw0.Start();
            int checksum0 = 0;
            for(var i=0; i<MAX_TESTS; i++)
            {
                checksum0 += uut_32_1[i].CompareTo(uut_32_2[i]);
            }
            sw0.Stop();
            TimeSpan time0 = sw0.Elapsed;
            Console.WriteLine("Elapsed={0} Sum={1}",time0, checksum0);

            // testing code1 algorithm
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            int checksum1 = 0;
            for(var i=0; i<MAX_TESTS; i++)
            {
                checksum1 += code1_UInt256CompareTo(base_32_1[i], base_32_2[i]);
            }
            sw1.Stop();
            TimeSpan time1 = sw1.Elapsed;
            Console.WriteLine("Elapsed={0} Sum={1}",time1, checksum1);

            // testing code2 algorithm
            Stopwatch sw2 = new Stopwatch();
            sw2.Start();
            int checksum2 = 0;
            for(var i=0; i<MAX_TESTS; i++)
            {
                checksum2 += code2_UInt256CompareTo(base_32_1[i], base_32_2[i]);
            }
            sw2.Stop();
            TimeSpan time2 = sw2.Elapsed;

            Console.WriteLine("Elapsed={0} Sum={1}",time2, checksum2);

            checksum0.Should().Be(checksum1);
            checksum1.Should().Be(checksum2);
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
                for (int i = 8; i >= 0; i--)
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
