// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RemoteNode_ProtocolHandler.cs file belongs to the neo project and is free
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
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    /// <summary>
    /// Behavioral coverage for <see cref="RemoteNode"/> protocol handling via Akka.TestKit.
    /// Drives framed <see cref="Tcp.Received"/> messages and asserts outbound <see cref="Tcp.Write"/> replies.
    /// </summary>
    [TestClass]
    public class UT_RemoteNode_ProtocolHandler : TestKit
    {
        private static NeoSystem s_system;
        private static uint s_nextNonce = 0xA11CE;

        public UT_RemoteNode_ProtocolHandler()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            s_system = TestBlockchain.GetSystem();
        }

        private static VersionPayload MakeVersion(uint startHeight = 7, ushort tcpPort = 20333)
        {
            // Nonce must not equal LocalNode.Nonce or AllowNewConnection rejects the peer.
            var nonce = s_nextNonce++;
            if (nonce == LocalNode.Nonce)
                nonce = s_nextNonce++;

            return new VersionPayload
            {
                UserAgent = "ProtocolHandlerUT",
                Nonce = nonce,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 1,
                Version = LocalNode.ProtocolVersion,
                Capabilities =
                [
                    new FullNodeCapability(startHeight),
                    new ServerCapability(NodeCapabilityType.TcpServer, tcpPort)
                ]
            };
        }

        private static Tcp.Received AsReceived(Message message)
        {
            return new Tcp.Received((ByteString)message.ToArray());
        }

        private (TestActorRef<RemoteNode> remote, TestProbe connection) SpawnRemoteNode()
        {
            var connection = CreateTestProbe("conn");
            var remoteEp = new IPEndPoint(IPAddress.Parse("192.0.2.10"), 20333);
            var localEp = new IPEndPoint(IPAddress.Loopback, 20334);
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

        /// <summary>
        /// Completes Version/Verack handshake. Leaves the connection ready for post-handshake commands.
        /// Default peer height is 0 so TaskManager only enqueues Mempool (not GetHeaders/GetBlocks).
        /// </summary>
        private void CompleteHandshake(
            TestActorRef<RemoteNode> remote,
            TestProbe connection,
            uint startHeight = 0,
            TestProbe sender = null)
        {
            sender ??= CreateTestProbe();
            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Version, MakeVersion(startHeight))));

            // Remote replies with Verack via Tcp.Write (ack token is Connection.Ack).
            connection.ExpectMsg<Tcp.Write>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);
            // Re-enable outbound queue (Tcp layer would normally deliver this ack).
            sender.Send(remote, Connection.Ack.Instance);

            // Peer sends Verack to finish handshake.
            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Verack)));

            Assert.IsNotNull(remote.UnderlyingActor.Version);
            Assert.IsTrue(remote.UnderlyingActor.IsFullNode);
            Assert.AreEqual(startHeight, remote.UnderlyingActor.LastBlockIndex);

            // OnVerack → TaskManager.Register → RequestTasks may Tell Mempool/GetHeaders/GetBlocks
            // back to this RemoteNode (cross NeoSystem actor system). Drain those first.
            DrainTaskManagerOutbound(remote, connection, sender);
        }

        /// <summary>
        /// Consumes outbound Tcp.Write frames produced by TaskManager after Register, acking each.
        /// </summary>
        private static void DrainTaskManagerOutbound(
            TestActorRef<RemoteNode> remote,
            TestProbe connection,
            TestProbe sender,
            TimeSpan? quiet = null)
        {
            var idle = quiet ?? TimeSpan.FromMilliseconds(400);
            while (true)
            {
                var next = connection.ReceiveOne(idle);
                if (next is null) return;
                if (next is Tcp.Write)
                {
                    sender.Send(remote, Connection.Ack.Instance);
                    continue;
                }
                // Unexpected non-Write; put back is not supported — fail loudly.
                Assert.Fail($"Unexpected message while draining TaskManager outbound: {next.GetType().Name}");
            }
        }

        /// <summary>
        /// Waits for an outbound wire message with the given command, acking intermediate writes
        /// (TaskManager noise or queue siblings).
        /// </summary>
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
                var next = connection.ReceiveOne(remaining);
                Assert.IsNotNull(next, $"Timed out waiting for outbound {command}");
                Assert.IsInstanceOfType<Tcp.Write>(next);
                var write = (Tcp.Write)next;
                sender.Send(remote, Connection.Ack.Instance);
                var msg = ParseOutbound(write);
                if (msg.Command == command)
                    return msg;
            }
            Assert.Fail($"Timed out waiting for outbound {command}");
            return null;
        }

        private static Message ParseOutbound(Tcp.Write write)
        {
            var length = Message.TryDeserialize(write.Data, out var msg);
            Assert.IsTrue(length > 0, "Outbound Tcp.Write did not contain a parseable Message");
            Assert.IsNotNull(msg);
            return msg;
        }

        [TestMethod]
        public void Handshake_Version_SetsCapabilities_And_SendsVerack()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();

            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Version, MakeVersion(42, 10333))));
            var write = connection.ExpectMsg<Tcp.Write>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);
            var reply = ParseOutbound(write);
            Assert.AreEqual(MessageCommand.Verack, reply.Command);

            Assert.IsTrue(remote.UnderlyingActor.IsFullNode);
            Assert.AreEqual(42u, remote.UnderlyingActor.LastBlockIndex);
            Assert.AreEqual(10333, remote.UnderlyingActor.ListenerTcpPort);
        }

        [TestMethod]
        public void AfterHandshake_Ping_RepliesWithPong_AndUpdatesLastBlockIndex()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender: sender);

            const uint peerHeight = 99;
            const uint nonce = 0xC0FFEE;
            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.Ping,
                PingPayload.Create(peerHeight, nonce))));

            var pong = ExpectOutboundCommand(remote, connection, sender, MessageCommand.Pong);
            var payload = (PingPayload)pong.Payload;
            Assert.AreEqual(nonce, payload.Nonce);
            Assert.AreEqual(peerHeight, remote.UnderlyingActor.LastBlockIndex);
        }

        [TestMethod]
        public void AfterHandshake_Pong_UpdatesLastBlockIndex_WithoutWrite()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender: sender);

            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.Pong,
                PingPayload.Create(55, 1))));

            connection.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
            Assert.AreEqual(55u, remote.UnderlyingActor.LastBlockIndex);
        }

        [TestMethod]
        public void MessageReceived_ReturningFalse_StopsProtocolHandling()
        {
            var received = new List<MessageCommand>();
            MessageReceivedHandler handler = (_, msg) =>
            {
                received.Add(msg.Command);
                return false; // stop further OnMessage handling
            };
            RemoteNode.MessageReceived += handler;
            try
            {
                var (remote, connection) = SpawnRemoteNode();
                var sender = CreateTestProbe();
                sender.Send(remote, AsReceived(Message.Create(MessageCommand.Version, MakeVersion())));

                connection.ExpectNoMsg(TimeSpan.FromMilliseconds(400), cancellationToken: CancellationToken.None);
                Assert.IsNull(remote.UnderlyingActor.Version);
                Assert.HasCount(1, received);
                Assert.AreEqual(MessageCommand.Version, received[0]);
            }
            finally
            {
                RemoteNode.MessageReceived -= handler;
            }
        }

        [TestMethod]
        public void MessageReceived_ReturningTrue_IsInvoked_AndAllowsHandshake()
        {
            var seen = new List<MessageCommand>();
            MessageReceivedHandler handler = (system, msg) =>
            {
                Assert.AreSame(s_system, system);
                seen.Add(msg.Command);
                return true;
            };
            RemoteNode.MessageReceived += handler;
            try
            {
                var (remote, connection) = SpawnRemoteNode();
                CompleteHandshake(remote, connection);

                Assert.IsTrue(seen.Contains(MessageCommand.Version));
                Assert.IsTrue(seen.Contains(MessageCommand.Verack));
            }
            finally
            {
                RemoteNode.MessageReceived -= handler;
            }
        }

        [TestMethod]
        public void AfterHandshake_GetHeaders_FromGenesis_ReturnsHeaders()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender: sender);

            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.GetHeaders,
                GetBlockByIndexPayload.Create(0, 1))));

            var msg = ExpectOutboundCommand(remote, connection, sender, MessageCommand.Headers);
            var headers = (HeadersPayload)msg.Payload;
            Assert.IsTrue(headers.Headers.Length >= 1);
            Assert.AreEqual(0u, headers.Headers[0].Index);
        }

        [TestMethod]
        public void AfterHandshake_GetData_MissingTx_SendsNotFound()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender: sender);

            var missing = new UInt256(Crypto.Hash256(new byte[] { 1, 2, 3, 4 }));
            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.GetData,
                InvPayload.Create(InventoryType.TX, missing))));

            var msg = ExpectOutboundCommand(remote, connection, sender, MessageCommand.NotFound);
            var inv = (InvPayload)msg.Payload;
            Assert.AreEqual(InventoryType.TX, inv.Type);
            Assert.HasCount(1, inv.Hashes);
            Assert.AreEqual(missing, inv.Hashes[0]);
        }

        [TestMethod]
        public void AfterHandshake_Mempool_WithEmptyPool_SendsNoInv()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender: sender);

            // TaskManager may already have marked MempoolSent during drain; peer-initiated Mempool
            // with an empty pool still produces no Inv writes.
            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Mempool)));
            connection.ExpectNoMsg(TimeSpan.FromMilliseconds(400), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void AfterHandshake_FilterLoad_And_FilterClear_DoNotThrow()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender: sender);

            var filter = new BloomFilter(256, 2, 0xDEADBEEF);
            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.FilterLoad,
                FilterLoadPayload.Create(filter))));
            sender.Send(remote, AsReceived(Message.Create(MessageCommand.FilterClear)));
            sender.Send(remote, AsReceived(Message.Create(
                MessageCommand.FilterAdd,
                new FilterAddPayload { Data = new byte[] { 9, 9, 9 } })));

            // Filter ops have no direct wire reply; ensure actor stays alive (no unexpected write).
            connection.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
            Assert.IsNotNull(remote.UnderlyingActor.Version);
        }

        [TestMethod]
        public void AfterHandshake_DuplicateVersion_IsProtocolViolation()
        {
            var (remote, connection) = SpawnRemoteNode();
            var sender = CreateTestProbe();
            CompleteHandshake(remote, connection, sender: sender);

            // Second Version after handshake throws ProtocolViolationException inside OnMessage;
            // Connection.OnReceived catches Exception and Disconnect(true) → Tcp.Abort.
            sender.Send(remote, AsReceived(Message.Create(MessageCommand.Version, MakeVersion())));
            connection.ExpectMsg<Tcp.Abort>(TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);
        }
    }
}
