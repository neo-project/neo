// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ArchivalNodeCapability.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
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
    public class UT_ArchivalNodeCapability
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new ArchivalNodeCapability();
            Assert.AreEqual(2, test.Size);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new ArchivalNodeCapability();
            var buffer = test.ToArray();

            var br = new MemoryReader(buffer);
            var clone = (ArchivalNodeCapability)NodeCapability.DeserializeFrom(ref br);

            Assert.AreEqual(test.Type, clone.Type);
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
    }
}
