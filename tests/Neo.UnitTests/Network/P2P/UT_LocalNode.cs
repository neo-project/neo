// Copyright (C) 2015-2025 The Neo Project.
//
// UT_LocalNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using System.Collections.Immutable;
using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_LocalNode : TestKit
    {
        private static NeoSystem _system;

        [TestInitialize]
        public void Init()
        {
            _system = TestBlockchain.GetSystem();
        }

        [TestMethod]
        public void TestDefaults()
        {
            var senderProbe = CreateTestProbe();
            senderProbe.Send(_system.LocalNode, new ChannelsConfig()); // No Tcp
            senderProbe.Send(_system.LocalNode, new LocalNode.GetInstance());
            var localnode = senderProbe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);

            Assert.AreEqual(0, localnode.ListenerTcpPort);
            Assert.AreEqual(3, localnode.Config.MaxConnectionsPerAddress);
            Assert.AreEqual(10, localnode.Config.MinDesiredConnections);
            Assert.AreEqual(40, localnode.Config.MaxConnections);
            Assert.AreEqual(0, localnode.UnconnectedCount);

            CollectionAssert.AreEqual(Array.Empty<RemoteNode>(), localnode.GetRemoteNodes().ToArray());
            CollectionAssert.AreEqual(Array.Empty<IPEndPoint>(), localnode.GetUnconnectedPeers().ToArray());
        }

        [TestMethod]
        public void ProcessesTcpConnectedAfterConfigArrives()
        {
            var connectionProbe = CreateTestProbe();
            var remote = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 20333);
            var local = new IPEndPoint(IPAddress.Loopback, 20334);

            connectionProbe.Send(_system.LocalNode, new Tcp.Connected(remote, local));
            connectionProbe.ExpectNoMsg(TimeSpan.FromMilliseconds(200), cancellationToken: CancellationToken.None);

            var configProbe = CreateTestProbe();
            configProbe.Send(_system.LocalNode, new ChannelsConfig());

            connectionProbe.ExpectMsg<Tcp.Register>(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void OnTimerDoesNotDrainUnconnectedPeersWhenConnectingCapacityIsZero()
        {
            var config = new ChannelsConfig
            {
                MinDesiredConnections = 2,
                MaxConnections = 8,
                MaxConnectionsPerAddress = 2
            };

            var probe = CreateTestProbe();
            probe.Send(_system.LocalNode, config);
            probe.Send(_system.LocalNode, new LocalNode.GetInstance());
            var localNode = probe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);

            var unconnectedEndpoints = Enumerable.Range(0, 4)
                .Select(i => new IPEndPoint(IPAddress.Parse("203.0.113.10"), 20000 + i))
                .ToArray();
            var connectingEndpoints = Enumerable.Range(0, 8)
                .Select(i => new IPEndPoint(IPAddress.Parse("198.51.100.20"), 30000 + i))
                .ToArray();

            var unconnected = ImmutableHashSet.CreateRange(unconnectedEndpoints);
            var connecting = ImmutableHashSet.CreateRange(connectingEndpoints);

            typeof(Peer).GetField("UnconnectedPeers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(localNode, unconnected);
            typeof(Peer).GetField("ConnectingPeers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(localNode, connecting);

            typeof(Peer).GetMethod("OnTimer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(localNode, null);

            var updatedUnconnected = (ImmutableHashSet<IPEndPoint>)typeof(Peer)
                .GetField("UnconnectedPeers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(localNode);
            var updatedConnecting = (ImmutableHashSet<IPEndPoint>)typeof(Peer)
                .GetField("ConnectingPeers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(localNode);

            Assert.IsTrue(unconnected.SetEquals(updatedUnconnected));
            Assert.IsTrue(connecting.SetEquals(updatedConnecting));
        }
    }
}
