// Copyright (C) 2015-2024 The Neo Project.
//
// UT_VersionPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_VersionPayload
    {
        [TestMethod]
        public void SizeAndEndPoint_Get()
        {
            var test = new VersionPayload() { Capabilities = Array.Empty<NodeCapability>(), UserAgent = "neo3" };
            test.Size.Should().Be(22);

            test = VersionPayload.Create(123, 456, "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
            test.Size.Should().Be(25);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = VersionPayload.Create(123, 456, "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
            var clone = test.ToArray().AsSerializable<VersionPayload>();

            CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());
            Assert.AreEqual(test.UserAgent, clone.UserAgent);
            Assert.AreEqual(test.Nonce, clone.Nonce);
            Assert.AreEqual(test.Timestamp, clone.Timestamp);
            CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());

            Assert.ThrowsException<FormatException>(() => VersionPayload.Create(123, 456, "neo3",
                new NodeCapability[] {
                    new ServerCapability(NodeCapabilityType.TcpServer, 22) ,
                    new ServerCapability(NodeCapabilityType.TcpServer, 22)
                }).ToArray().AsSerializable<VersionPayload>());

            var buf = test.ToArray();
            buf[buf.Length - 2 - 1 - 1] += 3; // We've got 1 capability with 2 bytes, this adds three more to the array size.
            buf = buf.Concat(new byte[] { 0xfe, 0x00 }).ToArray(); // Type = 0xfe, zero bytes of data.
            buf = buf.Concat(new byte[] { 0xfd, 0x02, 0x00, 0x00 }).ToArray(); // Type = 0xfd, two bytes of data.
            buf = buf.Concat(new byte[] { 0x10, 0x01, 0x00, 0x00, 0x00 }).ToArray(); // FullNode capability, 0x01 index.

            clone = buf.AsSerializable<VersionPayload>();
            Assert.AreEqual(2, clone.Capabilities.Length);
            Assert.AreEqual(0, clone.Capabilities.OfType<UnknownCapability>().Count());
        }
    }
}
