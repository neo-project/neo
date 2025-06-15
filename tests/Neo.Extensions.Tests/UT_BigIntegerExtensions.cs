// Copyright (C) 2015-2025 The Neo Project.
//
// UT_BigIntegerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.Extensions.Tests
{
    [TestClass]
    public class UT_BigIntegerExtensions
    {
        [TestMethod]
        public void TestGetLowestSetBit()
        {
            var big1 = new BigInteger(0);
            Assert.AreEqual(-1, big1.GetLowestSetBit());

            var big2 = new BigInteger(512);
            Assert.AreEqual(9, big2.GetLowestSetBit());

            var big3 = new BigInteger(int.MinValue);
            Assert.AreEqual(31, big3.GetLowestSetBit());

            var big4 = new BigInteger(long.MinValue);
            Assert.AreEqual(63, big4.GetLowestSetBit());

            var big5 = new BigInteger(-18);
            Assert.AreEqual(1, big5.GetLowestSetBit());

            var big6 = BigInteger.Pow(2, 1000);
            Assert.AreEqual(1000, big6.GetLowestSetBit());
        }

        [TestMethod]
        public void TestGetLowestSetBit_EdgeCases()
        {
            Assert.AreEqual(0, BigInteger.MinusOne.GetLowestSetBit());
            Assert.AreEqual(0, BigInteger.One.GetLowestSetBit());
            Assert.AreEqual(0, new BigInteger(ulong.MaxValue).GetLowestSetBit());
            Assert.AreEqual(1000, (BigInteger.One << 1000).GetLowestSetBit());
        }

        [TestMethod]
        public void TestToByteArrayStandard()
        {
            BigInteger number = BigInteger.Zero;
            CollectionAssert.AreEqual(Array.Empty<byte>(), number.ToByteArrayStandard());

            number = BigInteger.One;
            CollectionAssert.AreEqual(new byte[] { 0x01 }, number.ToByteArrayStandard());

            number = new BigInteger(256); // Binary: 100000000
            CollectionAssert.AreEqual(new byte[] { 0x00, 0x01 }, number.ToByteArrayStandard());
        }

        [TestMethod]
        public void TestToByteArrayStandard_EdgeCases()
        {
            CollectionAssert.AreEqual(new byte[] { 0xFF }, BigInteger.MinusOne.ToByteArrayStandard());
            CollectionAssert.AreEqual(new byte[] { 0xFF, 0x00 }, new BigInteger(byte.MaxValue).ToByteArrayStandard());
            CollectionAssert.AreEqual(
                new byte[] { 0xFF, 0xFF, 0x00 },
                new BigInteger(ushort.MaxValue).ToByteArrayStandard()
            );
            CollectionAssert.AreEqual(
                new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0 },
                new BigInteger(JNumber.MIN_SAFE_INTEGER).ToByteArrayStandard()
            );
        }

        [TestMethod]
        public void TestMod()
        {
            var x = new BigInteger(-13);
            var y = new BigInteger(5);
            var result = x.Mod(y);
            Assert.AreEqual(2, result); // -13 % 5 is -3, but Mod method should return 2
        }

        [TestMethod]
        public void TestMod_EdgeCases()
        {
            // Test case 1: Mod of zero
            Assert.AreEqual(0, BigInteger.Zero.Mod(5), "Mod of zero should always be zero");

            // Test case 2: Mod of -1
            Assert.AreEqual(4, BigInteger.MinusOne.Mod(5), "Mod of -1 should return the modulus minus 1");

            // Test case 3: Mod with large numbers
            BigInteger minValue = new BigInteger(long.MinValue);
            BigInteger maxValue = new BigInteger(long.MaxValue);
            Assert.AreEqual(9223372036854775806, minValue.Mod(maxValue), "Mod with large numbers should be calculated correctly");

            // Test case 4: Comparing Mod with % operator
            BigInteger result = minValue.Mod(maxValue);
            Assert.AreNotEqual(long.MinValue % long.MaxValue, result, "Mod should always return non-negative values, unlike % operator");

            // Test case 5: Verifying % operator behavior
            Assert.AreEqual(long.MinValue % long.MaxValue, (long)(minValue % maxValue), "% operator should behave consistently for BigInteger and long");

            // Test case 6: Mod with prime numbers
            Assert.AreEqual(17, new BigInteger(17).Mod(19), "Mod with a larger prime should return the original number");

            // Test case 7: Mod with powers of 2
            Assert.AreEqual(0, new BigInteger(1024).Mod(16), "Mod with powers of 2 should utilize bitwise operations efficiently");
        }

        [TestMethod]
        public void TestModInverse()
        {
            var a = new BigInteger(3);
            var n = new BigInteger(11);
            var result = a.ModInverse(n);
            Assert.AreEqual(4, result); // 3 * 4 % 11 == 1

            a = new BigInteger(1);
            n = new BigInteger(11);
            result = a.ModInverse(n);
            Assert.AreEqual(1, result); // 1 * 1 % 11 == 1

            a = new BigInteger(13);
            n = new BigInteger(11);
            result = a.ModInverse(n);
            Assert.AreEqual(6, result); // 13 % 11 = 2, and 2 * 6 % 11 == 1

            a = new BigInteger(6);
            n = new BigInteger(12); // 6 and 12 are not coprime
            Assert.ThrowsExactly<ArithmeticException>(() => _ = a.ModInverse(n));
        }

        [TestMethod]
        public void TestModInverse_EdgeCases()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = BigInteger.Zero.ModInverse(11));

            Assert.AreEqual(1, BigInteger.One.ModInverse(2));

            Assert.ThrowsExactly<ArithmeticException>(() => _ = new BigInteger(2).ModInverse(4));

            Assert.AreEqual(long.MaxValue - 1, new BigInteger(long.MaxValue - 1).ModInverse(long.MaxValue));
        }

        [TestMethod]
        public void TestBit()
        {
            var bigInteger = new BigInteger(5); // Binary: 101
            Assert.IsTrue(bigInteger.TestBit(2)); // Bit at index 2 is set (1)

            bigInteger = new BigInteger(5); // Binary: 101
            Assert.IsFalse(bigInteger.TestBit(1)); // Bit at index 1 is not set (0)
        }

        [TestMethod]
        public void TestBit_EdgeCases()
        {
            Assert.IsFalse(BigInteger.Zero.TestBit(0));
            Assert.IsFalse(BigInteger.Zero.TestBit(100));
            Assert.IsTrue(BigInteger.MinusOne.TestBit(0));
            Assert.IsTrue(BigInteger.MinusOne.TestBit(1000));
            Assert.IsTrue((BigInteger.One << 1000).TestBit(1000));
            Assert.IsFalse((BigInteger.One << 1000).TestBit(999));
        }

        [TestMethod]
        public void TestSum()
        {
            var bigIntegers = new List<BigInteger> { 1, 2, 3, 4 };
            var result = bigIntegers.Sum();
            Assert.AreEqual(10, result);
        }

        [TestMethod]
        public void TestSum_EdgeCases()
        {
            Assert.AreEqual(0, new List<BigInteger>().Sum());
            Assert.AreEqual(0, new List<BigInteger> { JNumber.MIN_SAFE_INTEGER, JNumber.MAX_SAFE_INTEGER }.Sum());
            Assert.AreEqual(JNumber.MAX_SAFE_INTEGER * 2, new List<BigInteger> { JNumber.MAX_SAFE_INTEGER, JNumber.MAX_SAFE_INTEGER }.Sum());
        }

        [TestMethod]
        public void TestSqrtTest()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = BigInteger.MinusOne.Sqrt());

            Assert.AreEqual(BigInteger.Zero, BigInteger.Zero.Sqrt());
            Assert.AreEqual(new BigInteger(1), new BigInteger(1).Sqrt());
            Assert.AreEqual(new BigInteger(1), new BigInteger(2).Sqrt());
            Assert.AreEqual(new BigInteger(1), new BigInteger(3).Sqrt());
            Assert.AreEqual(new BigInteger(2), new BigInteger(4).Sqrt());
            Assert.AreEqual(new BigInteger(9), new BigInteger(81).Sqrt());
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            var byteValue = random.Next(0, 32);
            var value = new byte[byteValue];

            random.NextBytes(value);
            return value;
        }

        private void VerifyGetBitLength(BigInteger value, long expected)
        {
            var result = value.GetBitLength();
            Assert.AreEqual(expected, value.GetBitLength(), "Native method has not the expected result");
            Assert.AreEqual(result, BigIntegerExtensions.GetBitLength(value), "Result doesn't match");
            Assert.AreEqual(result, BigIntegerExtensions.BitLength(value), "Result doesn't match");
        }

        [TestMethod]
        public void TestGetBitLength()
        {
            var random = new Random();

            // Big Number (net standard didn't work)
            Assert.ThrowsExactly<OverflowException>(() => VerifyGetBitLength(BigInteger.One << 32 << int.MaxValue, 2147483680));

            // Trivial cases
            //                     sign bit|shortest two's complement
            //                              string w/o sign bit
            VerifyGetBitLength(0, 0);  // 0|
            VerifyGetBitLength(1, 1);  // 0|1
            VerifyGetBitLength(-1, 0); // 1|
            VerifyGetBitLength(2, 2);  // 0|10
            VerifyGetBitLength(-2, 1); // 1|0
            VerifyGetBitLength(3, 2);  // 0|11
            VerifyGetBitLength(-3, 2); // 1|01
            VerifyGetBitLength(4, 3);  // 0|100
            VerifyGetBitLength(-4, 2); // 1|00
            VerifyGetBitLength(5, 3);  // 0|101
            VerifyGetBitLength(-5, 3); // 1|011
            VerifyGetBitLength(6, 3);  // 0|110
            VerifyGetBitLength(-6, 3); // 1|010
            VerifyGetBitLength(7, 3);  // 0|111
            VerifyGetBitLength(-7, 3); // 1|001
            VerifyGetBitLength(8, 4);  // 0|1000
            VerifyGetBitLength(-8, 3); // 1|000

            // Random cases
            for (uint i = 0; i < 1000; i++)
            {
                var b = new BigInteger(GetRandomByteArray(random));
                Assert.AreEqual(b.GetBitLength(), BigIntegerExtensions.GetBitLength(b), message: $"Error comparing: {b}");
                Assert.AreEqual(b.GetBitLength(), BigIntegerExtensions.BitLength(b), message: $"Error comparing: {b}");
            }

            foreach (var bv in new[] { BigInteger.Zero, BigInteger.One, BigInteger.MinusOne, new(ulong.MaxValue), new(long.MinValue) })
            {
                Assert.AreEqual(bv.GetBitLength(), BigIntegerExtensions.GetBitLength(bv));
                Assert.AreEqual(bv.GetBitLength(), BigIntegerExtensions.BitLength(bv));
            }

            for (var i = 0; i < 1000; i++)
            {
                var b = new BigInteger(i);
                Assert.AreEqual(b.GetBitLength(), BigIntegerExtensions.GetBitLength(b), message: $"Error comparing: {b}");
                Assert.AreEqual(b.GetBitLength(), BigIntegerExtensions.BitLength(b), message: $"Error comparing: {b}");
            }
        }

        [TestMethod]
        public void TestModInverseTest()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = BigInteger.One.ModInverse(BigInteger.Zero));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = BigInteger.One.ModInverse(BigInteger.One));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = BigInteger.Zero.ModInverse(BigInteger.Zero));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = BigInteger.Zero.ModInverse(BigInteger.One));
            Assert.ThrowsExactly<ArithmeticException>(() => _ = new BigInteger(ushort.MaxValue).ModInverse(byte.MaxValue));
            Assert.AreEqual(new BigInteger(52), new BigInteger(19).ModInverse(141));
        }
    }
}
