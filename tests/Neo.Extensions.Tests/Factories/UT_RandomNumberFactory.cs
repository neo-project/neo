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

            var actualValue = RandomNumberFactory.NextSByte(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextSByteNegative()
        {
            sbyte expectedMax = 0;
            var expectedMin = sbyte.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextSByte(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextSByte(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextSByteMaxNegative()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextSByte(-1));
        }

        [TestMethod]
        public void CheckNextByteInRange()
        {
            var expectedMax = byte.MaxValue;
            var expectedMin = byte.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextByte(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextByte(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt16InRange()
        {
            var expectedMax = short.MaxValue;
            short expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt16(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt16InNegative()
        {
            short expectedMax = 0;
            var expectedMin = short.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt16(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt16MaxNegative()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt16(-1));
        }

        [TestMethod]
        public void CheckNextUInt16InRange()
        {
            var expectedMax = ushort.MaxValue;
            var expectedMin = ushort.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt16(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextUInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt32InRange()
        {
            var expectedMax = int.MaxValue;
            var expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt32(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt32(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt32InNegative()
        {
            var expectedMax = 0;
            var expectedMin = int.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt32(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt32(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt32MaxNegative()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt32(-1));
        }

        [TestMethod]
        public void CheckNextUInt32InRange()
        {
            var expectedMax = uint.MaxValue;
            var expectedMin = uint.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt32(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextUInt32(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt64InRange()
        {
            var expectedMax = long.MaxValue;
            var expectedMin = 0L;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt64(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt64(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt64InNegative()
        {
            var expectedMax = 0L;
            var expectedMin = long.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt64(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt64(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt64MaxNegative()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RandomNumberFactory.NextInt64(-1L));
        }

        [TestMethod]
        public void CheckNextUInt64InRange()
        {
            var expectedMax = ulong.MaxValue;
            var expectedMin = ulong.MinValue;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt64(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextUInt64(expectedMin, expectedMax);

            Assert.IsTrue(actualValue > expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextBigInteger()
        {
            var actual = RandomNumberFactory.NextBigInteger(byte.MaxValue + 1);
            Assert.AreNotEqual(0ul, actual);
            Assert.AreEqual(byte.MaxValue, actual.BitLength());
        }
    }
}
