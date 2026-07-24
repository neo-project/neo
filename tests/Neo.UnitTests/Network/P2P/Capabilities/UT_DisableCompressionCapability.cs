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
using Neo.Network.P2P.Capabilities;

namespace Neo.UnitTests.Network.P2P.Capabilities
{
    [TestClass]
    public class UT_DisableCompressionCapability
    {
        [TestMethod]
        public void Size_And_Type()
        {
            var cap = new DisableCompressionCapability();
            Assert.AreEqual(2, cap.Size);
            Assert.AreEqual(NodeCapabilityType.DisableCompression, cap.Type);
        }

        [TestMethod]
        public void Serialize_WritesTypeAndZeroByte()
        {
            var bytes = new DisableCompressionCapability().ToArray();
            Assert.HasCount(2, bytes);
            Assert.AreEqual((byte)NodeCapabilityType.DisableCompression, bytes[0]);
            Assert.AreEqual(0, bytes[1]);
        }
    }
}
