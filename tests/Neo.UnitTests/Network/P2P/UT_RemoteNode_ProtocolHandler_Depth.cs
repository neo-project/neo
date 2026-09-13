// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RemoteNode_ProtocolHandler_Depth.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using Akka.TestKit;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.Net;
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    /// <summary>
    /// Additional ProtocolHandler coverage beyond UT_RemoteNode_ProtocolHandler (handshake/ping/filters).
    /// </summary>
    [TestClass]
    public class UT_RemoteNode_ProtocolHandler_Depth : TestKit
    {
        private static NeoSystem s_system;
        private static uint s_nextNonce = 0xD00D;

        public UT_RemoteNode_ProtocolHandler_Depth()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            s_system = TestBlockchain.GetSystem();
        }

        private static VersionPayload MakeVersion(uint startHeight = 0)
        {
            var nonce = s_nextNonce++;
            if (nonce == LocalNode.Nonce)
                nonce = s_nextNonce++;

            return new VersionPayload
            {
                UserAgent = "ProtocolHandlerDepthUT",
                Nonce = nonce,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 1,
                Version = LocalNode.ProtocolVersion,
                Capabilities =
                [
                    new FullNodeCapability(startHeight),
                    new ServerCapability(NodeCapabilityType.TcpServer, 20333)
                ]
            };
        }

        private static Tcp.Received AsReceived(Message message) =>
            new((ByteString)message.ToArray());

        private (TestActorRef<RemoteNode> remote, TestProbe connection) SpawnRemoteNode()
        {
            var connection = CreateTestProbe("conn-depth");
            var remoteEp = new IPEndPoint(IPAddress.Parse("192.0.2.20"), 20333);
            var localEp = new IPEndPoint(IPAddress.Loopback, 20335);
            var remote = ActorOfAsTestActorRef(() =>
                new RemoteNode(
                    s_system,
                    new LocalNode(s_system),
                    connection,
                    remoteEp,
                    localEp,
                    new ChannelsConfig()));
            return (remote, connection);
        }

        private void CompleteHandshake(TestActorRef<RemoteNode> remote, TestProbe connection, TestProbe sender = null)
        {
            sender ??= CreateTestProbe();
            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Version, MakeVersion(0))));
            connection.ExpectMsg<Tcp.Write>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);
            sender.Send(remote, Connection.Ack.Instance);
            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Verack)));
            DrainOutbound(remote, connection, sender);
        }

        private static void DrainOutbound(TestActorRef<RemoteNode> remote, TestProbe connection, TestProbe sender, TimeSpan? quiet = null)
        {
            var idle = quiet ?? TimeSpan.FromMilliseconds(400);
            while (true)
            {
                var next = connection.ReceiveOne(idle, cancellationToken: CancellationToken.None);
                if (next is null) return;
                if (next is Tcp.Write)
                {
                    sender.Send(remote, Connection.Ack.Instance);
                    continue;
                }
                Assert.Fail($"Unexpected message while draining: {next.GetType().Name}");
            }
        }

        private static Message ExpectOutboundCommand(
            TestActorRef<RemoteNode> remote,
            TestProbe connection,
            TestProbe sender,
            MessageCommand command,
            TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(3));
            while (DateTime.UtcNow < deadline)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                var next = connection.ReceiveOne(remaining, cancellationToken: CancellationToken.None);
                Assert.IsNotNull(next, $"Timed out waiting for outbound {command}");
                Assert.IsInstanceOfType<Tcp.Write>(next);
                sender.Send(remote, Connection.Ack.Instance);
                var length = Message.TryDeserialize(((Tcp.Write)next).Data, out var msg);
                Assert.IsTrue(length > 0);
                if (msg.Command == command)
                    return msg;
            }
            Assert.Fail($"Timed out waiting for outbound {command}");
            return null;
        }

        [TestMethod]
        public void AfterHandshake_GetBlocks_FromGenesis_SendsInvOrNothing()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender);

            var genesis = NativeContract.Ledger.GetBlockHash(s_system.StoreView, 0);
            Assert.IsNotNull(genesis);

            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.GetBlocks,
                GetBlocksPayload.Create(genesis, 10))));

            // Only genesis exists: either no hashes after start, or empty → no Inv.
            // If more blocks exist in the test chain, Inv is acceptable.
            var next = connection.ReceiveOne(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
            if (next is Tcp.Write write)
            {
                sender.Send(remote, Connection.Ack.Instance);
                Message.TryDeserialize(write.Data, out var msg);
                Assert.IsTrue(msg.Command is MessageCommand.Inv or MessageCommand.NotFound);
            }
        }

        [TestMethod]
        public void AfterHandshake_GetBlockByIndex_Genesis_SendsBlock()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender);

            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.GetBlockByIndex,
                GetBlockByIndexPayload.Create(0, 1))));

            var msg = ExpectOutboundCommand(remote, connection, sender, MessageCommand.Block);
            Assert.IsInstanceOfType<Block>(msg.Payload);
            Assert.AreEqual(0u, ((Block)msg.Payload).Index);
        }

        [TestMethod]
        public void AfterHandshake_GetData_MissingBlock_SendsNotFound()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender);

            var missing = new UInt256(Crypto.Hash256([9, 8, 7, 6]));
            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.GetData,
                InvPayload.Create(InventoryType.Block, missing))));

            var msg = ExpectOutboundCommand(remote, connection, sender, MessageCommand.NotFound);
            var inv = (InvPayload)msg.Payload;
            Assert.AreEqual(InventoryType.Block, inv.Type);
            Assert.AreEqual(missing, inv.Hashes[0]);
        }

        [TestMethod]
        public void AfterHandshake_GetData_GenesisBlock_SendsBlock()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender);

            var genesisHash = NativeContract.Ledger.GetBlockHash(s_system.StoreView, 0);
            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.GetData,
                InvPayload.Create(InventoryType.Block, genesisHash))));

            var msg = ExpectOutboundCommand(remote, connection, sender, MessageCommand.Block);
            Assert.AreEqual(genesisHash, ((Block)msg.Payload).Hash);
        }

        [TestMethod]
        public void AfterHandshake_Inv_Tx_DoesNotFault()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender);

            var hash = new UInt256(Crypto.Hash256([1, 1, 1, 1]));
            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.Inv,
                InvPayload.Create(InventoryType.TX, hash))));

            // Inv is forwarded to TaskManager; may produce delayed outbound frames — drain, stay alive.
            DrainOutbound(remote, connection, sender, TimeSpan.FromMilliseconds(300));
            Assert.IsNotNull(remote.UnderlyingActor.Version);
        }

        [TestMethod]
        public void AfterHandshake_GetAddr_WithNoPeers_DoesNotFault()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender);

            sender.Send(remote, AsReceived(Message.Create(MessageCommand.GetAddr)));
            // With no peers, GetAddr should not produce Addr; ignore residual TaskManager frames.
            DrainOutbound(remote, connection, sender, TimeSpan.FromMilliseconds(300));
            Assert.IsNotNull(remote.UnderlyingActor.Version);
        }

        [TestMethod]
        public void AfterHandshake_Headers_UpdatesLastBlockIndex()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender);

            var header = NativeContract.Ledger.GetHeader(s_system.StoreView, 0);
            Assert.IsNotNull(header);

            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.Headers,
                HeadersPayload.Create([header]))));

            // Headers are forwarded to Blockchain; peer LastBlockIndex updates from last header.
            Assert.AreEqual(header.Index, remote.UnderlyingActor.LastBlockIndex);
        }

        [TestMethod]
        public void PreHandshake_NonVersion_Aborts()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();

            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Ping, PingPayload.Create(0, 1))));
            connection.ExpectMsg<Tcp.Abort>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);
        }
    }
}
