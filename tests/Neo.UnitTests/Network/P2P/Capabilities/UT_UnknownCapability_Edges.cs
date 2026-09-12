// Copyright (C) 2015-2026 The Neo Project.
//
// UT_UnknownCapability_Edges.cs file belongs to the neo project and is free
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
using System;

namespace Neo.UnitTests.Network.P2P.Capabilities
{
    /// <summary>
    /// Size/MaxDataSize edges beyond UT_UnknownCapability.DeserializeUnknown.
    /// </summary>
    [TestClass]
    public class UT_UnknownCapability_Edges
    {
        [TestMethod]
        public void MaxDataSize_Is1024()
        {
            Assert.AreEqual(1024, UnknownCapability.MaxDataSize);
        }

        [TestMethod]
        public void Size_IncludesVarBytesPrefix()
        {
            var cap = new UnknownCapability(NodeCapabilityType.Extension0)
            {
                Data = new byte[] { 1, 2, 3 }
            };
            Assert.AreEqual(1 + 1 + 3, cap.Size); // type + varlen + payload
            Assert.AreEqual(cap.Size, cap.ToArray().Length);
        }

        [TestMethod]
        public void EmptyData_SerializeRoundTrip()
        {
            var original = new UnknownCapability((NodeCapabilityType)0xF1)
            {
                Data = ReadOnlyMemory<byte>.Empty
            };
            // Force via polymorphic path after building wire bytes
            var bytes = original.ToArray();
            var reader = new Neo.IO.MemoryReader(bytes);
            var clone = (UnknownCapability)NodeCapability.DeserializeFrom(ref reader);
            Assert.AreEqual(original.Type, clone.Type);
            Assert.AreEqual(0, clone.Data.Length);
        }
    }
}
