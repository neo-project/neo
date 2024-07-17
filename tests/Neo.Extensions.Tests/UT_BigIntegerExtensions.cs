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
        }

        [TestMethod]
        public void TestToByteArrayStandard()
        {
            BigInteger number = BigInteger.Zero;
            Assert.AreEqual("", number.ToByteArrayStandard().ToHexString());

            number = BigInteger.One;
            Assert.AreEqual("01", number.ToByteArrayStandard().ToHexString());
        }
    }
}
