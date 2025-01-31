// Copyright (C) 2015-2025 The Neo Project.
//
// UT_UnknownCapability.cs file belongs to the neo project and is free
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
    public class UT_UnknownCapability
    {
        [TestMethod]
        public void DeserializeUnknown()
        {
            var buffer = new byte[] { 0xff, 0x03, 0x01, 0x02, 0x03 }; // Type 0xff, three bytes of data.

            var br = new MemoryReader(buffer);
            var capab = (NodeCapability)NodeCapability.DeserializeFrom(ref br);

            Assert.IsTrue(capab is UnknownCapability);
            CollectionAssert.AreEqual(buffer, capab.ToArray());
        }
    }
}
