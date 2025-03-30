// Copyright (C) 2015-2025 The Neo Project.
//
// MemoryReaderTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.Plugins.DBFTPlugin.Tests.TestUtils
{
    [TestClass]
    public class MemoryReaderTest
    {
        [TestMethod]
        public void TestMemoryReaderUsage()
        {
            // Create a simple byte array
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };

            // Use MemoryReader to read the data
            var reader = new MemoryReader(data);

            // Read some data
            byte b1 = reader.ReadByte();
            byte b2 = reader.ReadByte();

            // Verify the data was read correctly
            Assert.AreEqual(1, b1);
            Assert.AreEqual(2, b2);
        }
    }
}
