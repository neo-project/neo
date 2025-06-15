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

namespace Neo.Extensions.Tests.Factories
{
    [TestClass]
    public class UT_RandomNumberFactory
    {
        [TestMethod]
        public void CheckNextSByteInRange()
        {
            sbyte expectedMax = 100;
            sbyte expectedMin = -100;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextSByte(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextSByte(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextByteInRange()
        {
            byte expectedMax = 100;
            byte expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextByte(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextByte(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt16InRange()
        {
            short expectedMax = 100;
            short expectedMin = -100;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt16(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextUInt16InRange()
        {
            ushort expectedMax = 100;
            ushort expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt16(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextUInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt32InRange()
        {
            var expectedMax = 100;
            var expectedMin = -100;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt32(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt32(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextUInt32InRange()
        {
            uint expectedMax = 100;
            uint expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt32(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextUInt32(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextInt64InRange()
        {
            long expectedMax = 100;
            long expectedMin = -100;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextInt64(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextInt64(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }

        [TestMethod]
        public void CheckNextUInt64InRange()
        {
            ulong expectedMax = 100;
            ulong expectedMin = 0;

            Assert.AreEqual(expectedMax, RandomNumberFactory.NextUInt64(expectedMax, expectedMax));

            var actualValue = RandomNumberFactory.NextUInt64(expectedMin, expectedMax);

            Assert.IsTrue(actualValue >= expectedMin && actualValue <= expectedMax);
        }
    }
}
