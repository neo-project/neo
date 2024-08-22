// Copyright (C) 2015-2024 The Neo Project.
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
        public void TestMod()
        {
            var x = new BigInteger(-13);
            var y = new BigInteger(5);
            var result = x.Mod(y);
            result.Should().Be(2); // -13 % 5 is -3, but Mod method should return 2
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
        public void TestSum()
        {
            var bigIntegers = new List<BigInteger> { 1, 2, 3, 4 };
            var result = bigIntegers.Sum();
            result.Should().Be(10);
        }
    }
}
