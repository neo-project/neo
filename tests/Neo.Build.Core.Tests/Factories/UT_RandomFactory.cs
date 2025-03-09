// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RandomFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Factories;

namespace Neo.Build.Core.Tests.Factories
{
    [TestClass]
    public class UT_RandomFactory
    {
        [TestMethod]
        public void IsNextSByteInBetweenRange()
        {
            sbyte expectedMax = 100;
            sbyte expectedMin = -100;

            Assert.AreEqual(expectedMax, RandomFactory.NextSByte(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextSByte(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void IsNextByteInBetweenRange()
        {
            byte expectedMax = 100;
            byte expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomFactory.NextByte(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextByte(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void IsNextInt16InBetweenRange()
        {
            short expectedMax = 100;
            short expectedMin = -100;

            Assert.AreEqual(expectedMax, RandomFactory.NextInt16(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void IsNextUInt16InBetweenRange()
        {
            ushort expectedMax = 100;
            ushort expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomFactory.NextUInt16(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextUInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void IsNextInt32InBetweenRange()
        {
            var expectedMax = 100;
            var expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomFactory.NextInt32(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextInt32(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void IsNextUInt32InBetweenRange()
        {
            uint expectedMax = 100;
            uint expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomFactory.NextUInt32(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextUInt32(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void IsNextInt64InBetweenRange()
        {
            long expectedMax = 100;
            long expectedMin = -100;

            Assert.AreEqual(expectedMax, RandomFactory.NextInt64(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextInt64(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void IsNextUInt64InBetweenRange()
        {
            ulong expectedMax = 100;
            ulong expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomFactory.NextUInt64(expectedMax, expectedMax));

            var actualValue = RandomFactory.NextUInt64(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }
    }
}
