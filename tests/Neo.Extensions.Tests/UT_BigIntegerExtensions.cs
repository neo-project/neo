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

using FluentAssertions;
using Neo.Extensions;
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
            big1.GetLowestSetBit().Should().Be(-1);

            var big2 = new BigInteger(512);
            big2.GetLowestSetBit().Should().Be(9);

            var big3 = new BigInteger(int.MinValue);
            big3.GetLowestSetBit().Should().Be(31);

            var big4 = new BigInteger(long.MinValue);
            big4.GetLowestSetBit().Should().Be(63);

            var big5 = new BigInteger(-18);
            big5.GetLowestSetBit().Should().Be(1);

            var big6 = BigInteger.Pow(2, 1000);
            big6.GetLowestSetBit().Should().Be(1000);
        }

        [TestMethod]
        public void TestGetLowestSetBit_EdgeCases()
        {
            BigInteger.MinusOne.GetLowestSetBit().Should().Be(0);
            BigInteger.One.GetLowestSetBit().Should().Be(0);
            new BigInteger(ulong.MaxValue).GetLowestSetBit().Should().Be(0);
            (BigInteger.One << 1000).GetLowestSetBit().Should().Be(1000);
        }

        [TestMethod]
        public void TestToByteArrayStandard()
        {
            BigInteger number = BigInteger.Zero;
            number.ToByteArrayStandard().Should().BeEmpty();

            number = BigInteger.One;
            number.ToByteArrayStandard().Should().Equal(new byte[] { 0x01 });

            number = new BigInteger(256); // Binary: 100000000
            number.ToByteArrayStandard().Should().Equal(new byte[] { 0x00, 0x01 });
        }

        [TestMethod]
        public void TestToByteArrayStandard_EdgeCases()
        {
            BigInteger.MinusOne.ToByteArrayStandard().Should().Equal(new byte[] { 0xFF });
            new BigInteger(byte.MaxValue).ToByteArrayStandard().Should().Equal(new byte[] { 0xFF, 0x00 });
            new BigInteger(ushort.MaxValue).ToByteArrayStandard().Should().Equal(new byte[] { 0xFF, 0xFF, 0x00 });
            new BigInteger(JNumber.MIN_SAFE_INTEGER).ToByteArrayStandard().Should().Equal(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE0 });
        }

        [TestMethod]
        public void TestMod()
        {
            var x = new BigInteger(-13);
            var y = new BigInteger(5);
            var result = x.Mod(y);
            result.Should().Be(2); // -13 % 5 is -3, but Mod method should return 2
        }

        [TestMethod]
        public void TestMod_EdgeCases()
        {
            // Test case 1: Mod of zero
            BigInteger.Zero.Mod(5).Should().Be(0, "Mod of zero should always be zero");

            // Test case 2: Mod of -1
            BigInteger.MinusOne.Mod(5).Should().Be(4, "Mod of -1 should return the modulus minus 1");

            // Test case 3: Mod with large numbers
            BigInteger minValue = new BigInteger(long.MinValue);
            BigInteger maxValue = new BigInteger(long.MaxValue);
            minValue.Mod(maxValue).Should().Be(9223372036854775806, "Mod with large numbers should be calculated correctly");

            // Test case 4: Comparing Mod with % operator
            BigInteger result = minValue.Mod(maxValue);
            result.Should().NotBe(long.MinValue % long.MaxValue, "Mod should always return non-negative values, unlike % operator");

            // Test case 5: Verifying % operator behavior
            (long.MinValue % long.MaxValue).Should().Be((long)(minValue % maxValue), "% operator should behave consistently for BigInteger and long");

            // Test case 6: Mod with prime numbers
            new BigInteger(17).Mod(19).Should().Be(17, "Mod with a larger prime should return the original number");

            // Test case 7: Mod with powers of 2
            new BigInteger(1024).Mod(16).Should().Be(0, "Mod with powers of 2 should utilize bitwise operations efficiently");
        }

        [TestMethod]
        public void TestModInverse()
        {
            var a = new BigInteger(3);
            var n = new BigInteger(11);
            var result = a.ModInverse(n);
            result.Should().Be(4); // 3 * 4 % 11 == 1

            a = new BigInteger(1);
            n = new BigInteger(11);
            result = a.ModInverse(n);
            result.Should().Be(1); // 1 * 1 % 11 == 1

            a = new BigInteger(13);
            n = new BigInteger(11);
            result = a.ModInverse(n);
            result.Should().Be(6); // 13 % 11 = 2, and 2 * 6 % 11 == 1

            a = new BigInteger(6);
            n = new BigInteger(12); // 6 and 12 are not coprime
            Action act = () => a.ModInverse(n);
            act.Should().Throw<ArithmeticException>()
               .WithMessage("No modular inverse exists for the given inputs.");
        }

        [TestMethod]
        public void TestModInverse_EdgeCases()
        {
            Action act = () => BigInteger.Zero.ModInverse(11);
            act.Should().Throw<ArithmeticException>();

            BigInteger.One.ModInverse(2).Should().Be(1);

            act = () => new BigInteger(2).ModInverse(4);
            act.Should().Throw<ArithmeticException>();

            new BigInteger(long.MaxValue - 1).ModInverse(long.MaxValue).Should().Be(long.MaxValue - 1);
        }

        [TestMethod]
        public void TestBit()
        {
            var bigInteger = new BigInteger(5); // Binary: 101
            var result = bigInteger.TestBit(2);
            result.Should().BeTrue(); // Bit at index 2 is set (1)

            bigInteger = new BigInteger(5); // Binary: 101
            result = bigInteger.TestBit(1);
            result.Should().BeFalse(); // Bit at index 1 is not set (0)
        }

        [TestMethod]
        public void TestBit_EdgeCases()
        {
            BigInteger.Zero.TestBit(0).Should().BeFalse();
            BigInteger.Zero.TestBit(100).Should().BeFalse();
            BigInteger.MinusOne.TestBit(0).Should().BeTrue();
            BigInteger.MinusOne.TestBit(1000).Should().BeTrue();
            (BigInteger.One << 1000).TestBit(1000).Should().BeTrue();
            (BigInteger.One << 1000).TestBit(999).Should().BeFalse();
        }

        [TestMethod]
        public void TestSum()
        {
            var bigIntegers = new List<BigInteger> { 1, 2, 3, 4 };
            var result = bigIntegers.Sum();
            result.Should().Be(10);
        }

        [TestMethod]
        public void TestSum_EdgeCases()
        {
            new List<BigInteger>().Sum().Should().Be(0);
            new List<BigInteger> { JNumber.MIN_SAFE_INTEGER, JNumber.MAX_SAFE_INTEGER }.Sum().Should().Be(0);
            new List<BigInteger> { JNumber.MAX_SAFE_INTEGER, JNumber.MAX_SAFE_INTEGER }.Sum().Should().Be(JNumber.MAX_SAFE_INTEGER * 2);
        }
    }
}
