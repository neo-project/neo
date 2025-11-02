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

using Akka.Actor;
using Akka.IO;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.UnitTests;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
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
        public void DisableCompressionCapabilityIsAdvertisedWhenCompressionDisabled()
        {
            var probe = CreateTestProbe();
            var config = new ChannelsConfig { EnableCompression = false };

            probe.Send(_system.LocalNode, config);
            probe.Send(_system.LocalNode, new LocalNode.GetInstance());

            var localnode = probe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);
            var capabilities = localnode.GetNodeCapabilities();

            Assert.IsTrue(capabilities.OfType<DisableCompressionCapability>().Any());
        }

        [TestMethod]
        public void DisableCompressionCapabilityIsOmittedWhenCompressionEnabled()
        {
            var probe = CreateTestProbe();
            var config = new ChannelsConfig { EnableCompression = true };

            probe.Send(_system.LocalNode, config);
            probe.Send(_system.LocalNode, new LocalNode.GetInstance());

            var localnode = probe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);
            var capabilities = localnode.GetNodeCapabilities();

            Assert.IsFalse(capabilities.OfType<DisableCompressionCapability>().Any());
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
        public void RespectsMaxConnectionsPerAddress()
        {
            var firstConnection = CreateTestProbe();
            var secondConnection = CreateTestProbe();
            var remote = new IPEndPoint(IPAddress.Parse("203.0.113.5"), 20000);
            var local = new IPEndPoint(IPAddress.Loopback, 20001);

            var configProbe = CreateTestProbe();
            configProbe.Send(_system.LocalNode, new ChannelsConfig { MaxConnectionsPerAddress = 1 });

            firstConnection.Send(_system.LocalNode, new Tcp.Connected(remote, local));
            firstConnection.ExpectMsg<Tcp.Register>(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None);

            secondConnection.Send(_system.LocalNode, new Tcp.Connected(remote, local));
            secondConnection.ExpectMsg<Tcp.Abort>(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void DoesNotAddAlreadyConnectedPeerToUnconnected()
        {
            var configProbe = CreateTestProbe();
            configProbe.Send(_system.LocalNode, new ChannelsConfig());

            var remote = new IPEndPoint(IPAddress.Parse("198.51.100.50"), 21001);
            var local = new IPEndPoint(IPAddress.Loopback, 21002);
            var connectionProbe = CreateTestProbe();
            connectionProbe.Send(_system.LocalNode, new Tcp.Connected(remote, local));
            connectionProbe.ExpectMsg<Tcp.Register>(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None);

            var another = new IPEndPoint(IPAddress.Parse("203.0.113.10"), 21003);
            configProbe.Send(_system.LocalNode, new Peer.Peers { EndPoints = new[] { remote, another } });
            configProbe.Send(_system.LocalNode, new LocalNode.GetInstance());
            var localnode = configProbe.ExpectMsg<LocalNode>(TimeSpan.FromSeconds(1), cancellationToken: CancellationToken.None);

            bool containsAnother = SpinWait.SpinUntil(
                () => localnode.GetUnconnectedPeers().Contains(another),
                TimeSpan.FromMilliseconds(500));

            var unconnected = localnode.GetUnconnectedPeers().ToArray();
            CollectionAssert.DoesNotContain(unconnected, remote);
            Assert.IsTrue(containsAnother, "Expected unconnected peers to include the new endpoint.");
        }

        [TestMethod]
        public void RelayDirectly_ForwardsNonBlockInventoryToAllRemotes()
        {
            var localNodeRef = ActorOfAsTestActorRef(() => new LocalNode(_system));
            localNodeRef.Tell(new ChannelsConfig());

            var instanceProbe = CreateTestProbe();
            instanceProbe.Send(localNodeRef, new LocalNode.GetInstance());
            var localNode = instanceProbe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);

            var remoteOne = CreateTestProbe();
            var remoteTwo = CreateTestProbe();
            AddRemote(localNode, remoteOne.Ref, 0);
            AddRemote(localNode, remoteTwo.Ref, 0);

            var tx = TestUtils.GetTransaction(UInt160.Zero);
            localNodeRef.Tell(new LocalNode.RelayDirectly { Inventory = tx });

            remoteOne.ExpectMsg<RemoteNode.Relay>(
                msg => ReferenceEquals(tx, msg.Inventory),
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: CancellationToken.None);
            remoteTwo.ExpectMsg<RemoteNode.Relay>(
                msg => ReferenceEquals(tx, msg.Inventory),
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: CancellationToken.None);

            localNode.RemoteNodes.Clear();
            ClearConnections(localNode);
            Sys.Stop(localNodeRef);
        }

        [TestMethod]
        public void RelayDirectly_SendsBlocksOnlyToLowerHeightRemotes()
        {
            var localNodeRef = ActorOfAsTestActorRef(() => new LocalNode(_system));
            localNodeRef.Tell(new ChannelsConfig());

            var instanceProbe = CreateTestProbe();
            instanceProbe.Send(localNodeRef, new LocalNode.GetInstance());
            var localNode = instanceProbe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);

            var remoteLow = CreateTestProbe();
            var remoteHigh = CreateTestProbe();
            AddRemote(localNode, remoteLow.Ref, 1);
            AddRemote(localNode, remoteHigh.Ref, 5);

            var block = new Block
            {
                Header = new Header
                {
                    Index = 3,
                    Version = 0,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Timestamp = 0,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty,
                },
                Transactions = Array.Empty<Transaction>()
            };

            localNodeRef.Tell(new LocalNode.RelayDirectly { Inventory = block });

            remoteLow.ExpectMsg<RemoteNode.Relay>(
                msg => ReferenceEquals(block, msg.Inventory),
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: CancellationToken.None);
#pragma warning disable MSTEST0049
            remoteHigh.ExpectNoMsg(TimeSpan.FromMilliseconds(100));
#pragma warning restore MSTEST0049

            localNode.RemoteNodes.Clear();
            ClearConnections(localNode);
            Sys.Stop(localNodeRef);
        }

        [TestMethod]
        public void SendDirectly_PublishesInventoryWithoutWrapping()
        {
            var localNodeRef = ActorOfAsTestActorRef(() => new LocalNode(_system));
            localNodeRef.Tell(new ChannelsConfig());

            var instanceProbe = CreateTestProbe();
            instanceProbe.Send(localNodeRef, new LocalNode.GetInstance());
            var localNode = instanceProbe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);

            var remote = CreateTestProbe();
            AddRemote(localNode, remote.Ref, 0);

            var tx = TestUtils.GetTransaction(UInt160.Zero);
            localNodeRef.Tell(new LocalNode.SendDirectly { Inventory = tx });

            remote.ExpectMsg<Transaction>(
                t => ReferenceEquals(tx, t),
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: CancellationToken.None);

            localNode.RemoteNodes.Clear();
            ClearConnections(localNode);
            Sys.Stop(localNodeRef);
        }

        [TestMethod]
        public void NeedMorePeers_BroadcastsGetAddrWhenConnected()
        {
            var localNodeRef = ActorOfAsTestActorRef(() => new LocalNode(_system));
            localNodeRef.Tell(new ChannelsConfig());

            var instanceProbe = CreateTestProbe();
            instanceProbe.Send(localNodeRef, new LocalNode.GetInstance());
            var localNode = instanceProbe.ExpectMsg<LocalNode>(cancellationToken: CancellationToken.None);

            var remoteProbe = CreateTestProbe();
            AddRemote(localNode, remoteProbe.Ref, 0);
            AddConnectedPeer(localNode, remoteProbe.Ref, new IPEndPoint(IPAddress.Parse("198.51.100.1"), 3000));

            TriggerTimer(localNodeRef);

            remoteProbe.ExpectMsg<Message>(
                msg => msg.Command == MessageCommand.GetAddr,
                timeout: TimeSpan.FromSeconds(10),
                cancellationToken: CancellationToken.None);

            localNode.RemoteNodes.Clear();
            ClearConnections(localNode);
            Sys.Stop(localNodeRef);
        }

        private static void AddRemote(LocalNode localNode, IActorRef actor, uint lastBlockIndex)
        {
#pragma warning disable SYSLIB0050
            var remote = (RemoteNode)FormatterServices.GetUninitializedObject(typeof(RemoteNode));
#pragma warning restore SYSLIB0050
            var field = typeof(RemoteNode).GetField("<LastBlockIndex>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(remote, lastBlockIndex);
            localNode.RemoteNodes.TryAdd(actor, remote);
        }

        private static void AddConnectedPeer(LocalNode localNode, IActorRef actor, IPEndPoint endpoint)
        {
            var connectedPeersField = typeof(Peer).GetField("ConnectedPeers", BindingFlags.Instance | BindingFlags.NonPublic);
            var connectedPeers = (ConcurrentDictionary<IActorRef, IPEndPoint>)connectedPeersField!.GetValue(localNode);
            connectedPeers.TryAdd(actor, endpoint);

            var connectedAddressesField = typeof(Peer).GetField("ConnectedAddresses", BindingFlags.Instance | BindingFlags.NonPublic);
            var connectedAddresses = (Dictionary<IPAddress, int>)connectedAddressesField!.GetValue(localNode);
            connectedAddresses.TryGetValue(endpoint.Address, out var count);
            connectedAddresses[endpoint.Address] = count + 1;
        }

        private static void ClearConnections(LocalNode localNode)
        {
            var connectedPeersField = typeof(Peer).GetField("ConnectedPeers", BindingFlags.Instance | BindingFlags.NonPublic);
            var connectedPeers = (ConcurrentDictionary<IActorRef, IPEndPoint>)connectedPeersField!.GetValue(localNode);
            connectedPeers.Clear();

            var connectedAddressesField = typeof(Peer).GetField("ConnectedAddresses", BindingFlags.Instance | BindingFlags.NonPublic);
            var connectedAddresses = (Dictionary<IPAddress, int>)connectedAddressesField!.GetValue(localNode);
            connectedAddresses.Clear();
        }

        private static void TriggerTimer(IActorRef peer)
        {
            var timerType = typeof(Peer).GetNestedType("Timer", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(timerType, "Peer.Timer type not found via reflection.");
            var timer = Activator.CreateInstance(timerType!);
            peer.Tell(timer!);
        }
    }
}
