// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NetworkAddressWithTime.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using System;
using System.Net;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_NetworkAddressWithTime
    {
        [TestMethod]
        public void SizeAndEndPoint_Get()
        {
            var test = new NetworkAddressWithTime() { Capabilities = new NodeCapability[0], Address = IPAddress.Any, Timestamp = 1 };
            test.Size.Should().Be(21);

            Assert.AreEqual(test.EndPoint.Port, 0);

            test = NetworkAddressWithTime.Create(IPAddress.Any, 1, new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
            test.Size.Should().Be(24);

            Assert.AreEqual(test.EndPoint.Port, 22);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = NetworkAddressWithTime.Create(IPAddress.Any, 1, new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
            var clone = test.ToArray().AsSerializable<NetworkAddressWithTime>();

            CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());
            Assert.AreEqual(test.EndPoint.ToString(), clone.EndPoint.ToString());
            Assert.AreEqual(test.Timestamp, clone.Timestamp);
            Assert.AreEqual(test.Address, clone.Address);

            Assert.ThrowsException<FormatException>(() => NetworkAddressWithTime.Create(IPAddress.Any, 1,
                new NodeCapability[] {
                    new ServerCapability(NodeCapabilityType.TcpServer, 22) ,
                    new ServerCapability(NodeCapabilityType.TcpServer, 22)
                }).ToArray().AsSerializable<NetworkAddressWithTime>());
        }
    }
}
