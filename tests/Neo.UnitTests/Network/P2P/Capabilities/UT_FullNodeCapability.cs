// Copyright (C) 2015-2024 The Neo Project.
//
// UT_FullNodeCapability.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Capabilities;

namespace Neo.UnitTests.Network.P2P.Capabilities
{
    [TestClass]
    public class UT_FullNodeCapability
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new FullNodeCapability() { StartHeight = 1 };
            test.Size.Should().Be(5);

            test = new FullNodeCapability(2);
            test.Size.Should().Be(5);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new FullNodeCapability() { StartHeight = uint.MaxValue };
            var buffer = test.ToArray();

            var br = new MemoryReader(buffer);
            var clone = (FullNodeCapability)NodeCapability.DeserializeFrom(ref br);

            Assert.AreEqual(test.StartHeight, clone.StartHeight);
        }
    }
}
