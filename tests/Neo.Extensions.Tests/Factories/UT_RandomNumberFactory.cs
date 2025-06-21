// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RandomNumberFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


// Copyright (C) 2015-2025 The Neo Project.
//
// RandomNumberFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.Factories;
using System;
using System.Numerics;

namespace Neo.Extensions.Tests.Factories
{
    [TestClass]
    public class UT_RandomNumberFactory
    {
        [TestMethod]
        public void CheckNextSByteInRange()
        {
            var expectedMax = sbyte.MaxValue;
            sbyte expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextSByte(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextSByte(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextSByte(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextSByte(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextSByteNegative()
        {
            sbyte expectedMax = 0;
            var expectedMin = sbyte.MinValue;

            var actualValue = RandomNumberFactory.NextSByte(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextSByteExceptions()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextSByte(-1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextSByte(-1, -2));
        }

        [TestMethod]
        public void CheckNextByteInRange()
        {
            var expectedMax = byte.MaxValue;
            var expectedMin = byte.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextByte(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextByte(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextByte(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextByte(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt16InRange()
        {
            var expectedMax = short.MaxValue;
            short expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt16(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextInt16(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextInt16(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextInt16(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt16InNegative()
        {
            short expectedMax = 0;
            var expectedMin = short.MinValue;

            var actualValue = RandomNumberFactory.NextInt16(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextInt16(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt16Exceptions()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt16(-1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt16(-1, -2));
        }

        [TestMethod]
        public void CheckNextUInt16InRange()
        {
            var expectedMax = ushort.MaxValue;
            var expectedMin = ushort.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt16(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextUInt16(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextUInt16(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextUInt16(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt32InRange()
        {
            var expectedMax = int.MaxValue;
            var expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt32(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextInt32(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextInt32(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextInt32(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt32InNegative()
        {
            var expectedMax = 0;
            var expectedMin = int.MinValue;

            var actualValue = RandomNumberFactory.NextInt32(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt32Exceptions()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt32(-1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt32(-1, -2));
        }

        [TestMethod]
        public void CheckNextUInt32InRange()
        {
            var expectedMax = uint.MaxValue;
            var expectedMin = uint.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt32(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextUInt32(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextUInt32(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextUInt32(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt64InRange()
        {
            var expectedMax = long.MaxValue;
            var expectedMin = 0L;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt64(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextInt64(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextInt64(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextInt64(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt64InNegative()
        {
            var expectedMax = 0L;
            var expectedMin = long.MinValue;

            var actualValue = RandomNumberFactory.NextInt64(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt64Exceptions()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt64(-1L));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt64(-1L, -2L));
        }

        [TestMethod]
        public void CheckNextUInt64InRange()
        {
            var expectedMax = ulong.MaxValue;
            var expectedMin = ulong.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt64(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextUInt64(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextUInt64(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextUInt64(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextBigIntegerSizeInBits()
        {
            var actualValue = RandomNumberFactory.NextBigInteger(byte.MaxValue);
            Assert.IsTrue(actualValue > BigInteger.Zero);

            actualValue = RandomNumberFactory.NextBigInteger(0);
            Assert.AreEqual(BigInteger.Zero, actualValue);

            Assert.ThrowsExactly<ArgumentException>(() => RandomNumberFactory.NextBigInteger(-1));
        }

        [TestMethod]
        public void CheckNextBigIntegerInRange()
        {
            var expectedMax = BigInteger.Parse("100000000000000000000000");
            var expectedMin = BigInteger.Zero;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextBigInteger(expectedMax, expectedMax));
            Assert.AreEqual(expectedMin, RandomNumberFactory.NextBigInteger(expectedMin, expectedMin));

            var actualValue = RandomNumberFactory.NextBigInteger(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);

            actualValue = RandomNumberFactory.NextBigInteger(expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextBigIntegerInNegative()
        {
            var expectedMax = BigInteger.Zero;
            var expectedMin = BigInteger.Pow(2, 100) * BigInteger.MinusOne;

            var actualValue = RandomNumberFactory.NextBigInteger(expectedMin, expectedMax);
            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
            Assert.IsTrue(actualValue.Sign < 0);
        }

        [TestMethod]
        public void CheckNextBigIntegerMaxNegative()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextBigInteger(BigInteger.MinusOne));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextBigInteger(BigInteger.MinusOne, -2));
        }
    }
}
