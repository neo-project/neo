// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NetworkAddressWithTime.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
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
            Assert.AreEqual(21, test.Size);

            Assert.AreEqual(test.EndPoint.Port, 0);

            test = NetworkAddressWithTime.Create(IPAddress.Any, 1, new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
            Assert.AreEqual(24, test.Size);

            Assert.AreEqual(test.EndPoint.Port, 22);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = NetworkAddressWithTime.Create(IPAddress.Any, 1, new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22), new UnknownCapability(NodeCapabilityType.Extension0), new UnknownCapability(NodeCapabilityType.Extension0) });
            var clone = test.ToArray().AsSerializable<NetworkAddressWithTime>();

            Assert.AreEqual(test.Address, clone.Address);
            Assert.AreEqual(test.EndPoint.ToString(), clone.EndPoint.ToString());
            Assert.AreEqual(test.Timestamp, clone.Timestamp);
            CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());

            Assert.ThrowsException<FormatException>(() => NetworkAddressWithTime.Create(IPAddress.Any, 1,
                new NodeCapability[] {
                    new ServerCapability(NodeCapabilityType.TcpServer, 22) ,
                    new ServerCapability(NodeCapabilityType.TcpServer, 22)
                }).ToArray().AsSerializable<NetworkAddressWithTime>());
        }
    }
}
