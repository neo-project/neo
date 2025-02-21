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
        public void IsSharedSameObjects()
        {
            Assert.IsNotNull(RandomFactory.Shared);
            Assert.AreSame(RandomFactory.Shared, RandomFactory.Shared);
        }

        [TestMethod]
        public void IsCreateNewDifferentObjects()
        {
            var expectedRandomObject = RandomFactory.CreateNew();

            Assert.IsNotNull(expectedRandomObject);
            Assert.AreNotSame(expectedRandomObject, RandomFactory.CreateNew());
        }

        [TestMethod]
        public void IsNextByteInBetweenRange()
        {
            byte expectedMax = 3;
            byte expectedMin = 2;

            Assert.AreEqual(3, RandomFactory.NextByte(expectedMax, expectedMax));

            var actualByte = RandomFactory.NextByte(expectedMin, expectedMax);

            Assert.IsTrue(actualByte >= expectedMin && actualByte <= expectedMax);
        }

        [TestMethod]
        public void IsNextInt16InBetweenRange()
        {
            short expectedMax = 3;
            short expectedMin = 2;

            Assert.AreEqual(3, RandomFactory.NextInt16(expectedMax, expectedMax));

            var actualByte = RandomFactory.NextInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualByte >= expectedMin && actualByte <= expectedMax);
        }

        [TestMethod]
        public void IsNextUInt16InBetweenRange()
        {
            ushort expectedMax = 3;
            ushort expectedMin = 2;

            Assert.AreEqual(3, RandomFactory.NextUInt16(expectedMax, expectedMax));

            var actualByte = RandomFactory.NextUInt16(expectedMin, expectedMax);

            Assert.IsTrue(actualByte >= expectedMin && actualByte <= expectedMax);
        }

        [TestMethod]
        public void IsNextUInt32InBetweenRange()
        {
            Assert.AreNotSame(RandomFactory.NextUInt32(), RandomFactory.NextUInt32());
            Assert.AreNotSame(RandomFactory.NextUInt32(), RandomFactory.NextUInt32());
            Assert.AreNotSame(RandomFactory.NextUInt32(), RandomFactory.NextUInt32());
            Assert.AreNotSame(RandomFactory.NextUInt32(), RandomFactory.NextUInt32());
        }

        [TestMethod]
        public void IsNextInt64InBetweenRange()
        {
            Assert.AreNotSame(RandomFactory.NextInt64(), RandomFactory.NextInt64());
            Assert.AreNotSame(RandomFactory.NextInt64(), RandomFactory.NextInt64());
            Assert.AreNotSame(RandomFactory.NextInt64(), RandomFactory.NextInt64());
            Assert.AreNotSame(RandomFactory.NextInt64(), RandomFactory.NextInt64());
        }

        [TestMethod]
        public void IsNextUInt64InBetweenRange()
        {
            Assert.AreNotSame(RandomFactory.NextUInt64(), RandomFactory.NextUInt64());
            Assert.AreNotSame(RandomFactory.NextUInt64(), RandomFactory.NextUInt64());
            Assert.AreNotSame(RandomFactory.NextUInt64(), RandomFactory.NextUInt64());
            Assert.AreNotSame(RandomFactory.NextUInt64(), RandomFactory.NextUInt64());
        }
    }
}
