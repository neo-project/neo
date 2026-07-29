// Copyright (C) 2015-2026 The Neo Project.
//
// UT_DisableCompressionCapability.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Capabilities;
using System;

namespace Neo.UnitTests.Network.P2P.Capabilities
{
    [TestClass]
    public class UT_DisableCompressionCapability
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new DisableCompressionCapability();
            Assert.AreEqual(2, test.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new DisableCompressionCapability();
            var buffer = test.ToArray();

            var br = new MemoryReader(buffer);
            var clone = (DisableCompressionCapability)NodeCapability.DeserializeFrom(ref br);

            Assert.AreEqual(test.Type, clone.Type);
            Assert.AreEqual(NodeCapabilityType.DisableCompression, clone.Type);
            Assert.IsInstanceOfType<DisableCompressionCapability>(clone);
            Assert.AreSequenceEqual(buffer, clone.ToArray());

            // Non-zero payload must fail (empty VarBytes / string required).
            buffer[1] = 0x01;
            br = new MemoryReader(buffer);

            var exceptionHappened = false;
            // CS8175 prevents from using Assert.ThrowsException here
            try
            {
                NodeCapability.DeserializeFrom(ref br);
            }
            catch (FormatException)
            {
                exceptionHappened = true;
            }
            Assert.IsTrue(exceptionHappened);
        }

        [TestMethod]
        public void DeserializeFrom_DoesNotFallBackToUnknownCapability()
        {
            var buffer = new DisableCompressionCapability().ToArray();
            var br = new MemoryReader(buffer);
            var capability = NodeCapability.DeserializeFrom(ref br);

            Assert.IsNotInstanceOfType<UnknownCapability>(capability);
            Assert.IsInstanceOfType<DisableCompressionCapability>(capability);
        }
    }
}
