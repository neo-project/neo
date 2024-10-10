// Copyright (C) 2015-2024 The Neo Project.
//
// UT_PipeRemoteNodePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.Models.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Neo.Plugins.NamedPipeService.Tests.Payloads
{
    [TestClass]
    public class UT_PipeShowStatePayload
    {
        [TestMethod]
        public void IPipeMessage_ToArray_Null()
        {
            var payload1 = new PipeShowStatePayload();
            var expectedBytes = payload1.ToByteArray();

            var payload2 = new PipeShowStatePayload();
            var actualBytes = payload2.ToByteArray();
            var actualBytesWithoutHeader = actualBytes;

            CollectionAssert.AreEqual(expectedBytes, actualBytesWithoutHeader);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Null()
        {
            var payload1 = new PipeShowStatePayload();
            var expectedBytes = payload1.ToByteArray();

            var payload2 = new PipeShowStatePayload();
            payload2.FromByteArray(expectedBytes);

            var actualBytes = payload2.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(payload1.RemoteEndPoint, payload2.RemoteEndPoint);
            Assert.AreEqual(payload1.LastBlockIndex, payload2.LastBlockIndex);
            Assert.AreEqual(payload1.Version, payload2.Version);
        }

        [TestMethod]
        public void IPipeMessage_ToArray_Data()
        {
            var payload1 = new PipeShowStatePayload()
            {
                RemoteEndPoint = new(IPAddress.Loopback, 0),
                ListenerTcpPort = 999,
                ConnectedCount = 1,
                UnconnectedCount = 20,
                Height = 777u,
                HeaderHeight = 1000u,
                LastBlockIndex = 666u,
                Version = VersionPayload.Create(123u, 456u, "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) }),
            };
            var expectedBytes = payload1.ToByteArray();

            var payload2 = new PipeShowStatePayload()
            {
                RemoteEndPoint = new(IPAddress.Loopback, 0),
                ListenerTcpPort = 999,
                ConnectedCount = 1,
                UnconnectedCount = 20,
                Height = 777u,
                HeaderHeight = 1000u,
                LastBlockIndex = 666u,
                Version = VersionPayload.Create(123u, 456u, "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) }),
            };
            var actualBytes = payload2.ToByteArray();
            var actualBytesWithoutHeader = actualBytes;

            CollectionAssert.AreEqual(expectedBytes, actualBytesWithoutHeader);
        }

        [TestMethod]
        public void IPipeMessage_FromArray_Data()
        {
            var payload1 = new PipeShowStatePayload()
            {
                RemoteEndPoint = new(IPAddress.Loopback, 0),
                ListenerTcpPort = 999,
                ConnectedCount = 1,
                UnconnectedCount = 20,
                Height = 777u,
                HeaderHeight = 1000u,
                LastBlockIndex = 666u,
                Version = VersionPayload.Create(123u, 456u, "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) }),
            };
            var expectedBytes = payload1.ToByteArray();

            var payload2 = new PipeShowStatePayload();
            payload2.FromByteArray(expectedBytes);

            var actualBytes = payload2.ToByteArray();

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
            Assert.AreEqual(payload1.RemoteEndPoint.Address, payload2.RemoteEndPoint.Address);
            Assert.AreEqual(payload1.RemoteEndPoint.Port, payload2.RemoteEndPoint.Port);
            Assert.AreEqual(999, payload2.ListenerTcpPort);
            Assert.AreEqual(1, payload2.ConnectedCount);
            Assert.AreEqual(20, payload2.UnconnectedCount);
            Assert.AreEqual(777u, payload2.Height);
            Assert.AreEqual(1000u, payload2.HeaderHeight);
            Assert.AreEqual(666u, payload2.LastBlockIndex);
            Assert.AreEqual(payload1.LastBlockIndex, payload2.LastBlockIndex);
            Assert.AreEqual(payload1.Version.Version, payload2.Version.Version);
            Assert.AreEqual(payload1.Version.UserAgent, payload2.Version.UserAgent);
            Assert.AreEqual(payload1.Version.Nonce, payload2.Version.Nonce);
            Assert.AreEqual(payload1.Version.Timestamp, payload2.Version.Timestamp);
            Assert.IsInstanceOfType<ServerCapability>(payload1.Version.Capabilities[0]);
            Assert.AreEqual(payload1.Version.Capabilities[0].Type, payload2.Version.Capabilities[0].Type);
        }
    }
}
