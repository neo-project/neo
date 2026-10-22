// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RemoteNode.cs file belongs to the neo project and is free
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
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_RemoteNode : TestKit
    {
        private static NeoSystem _system;

        public UT_RemoteNode()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            _system = TestBlockchain.GetSystem();
        }

        [TestMethod]
        public void RemoteNode_Test_Abort_DifferentNetwork()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(_system, new LocalNode(_system), connectionTestProbe, null, null, new ChannelsConfig()));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Network = 2,
                Timestamp = 5,
                Version = 6,
                Capabilities =
                [
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                ]
            });

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, new Tcp.Received((ByteString)msg.ToArray()));

            connectionTestProbe.ExpectMsg<Tcp.Abort>(cancellationToken: CancellationToken.None);
        }

        private static T Field<T>(RemoteNode node, string name) =>
            (T)typeof(RemoteNode).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node)!;

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Disconnect_ClearsQueuesAndReceiveBuffer(bool abort)
        {
            var connection = CreateTestProbe();
            var actor = ActorOfAsTestActorRef(() => new RemoteNode(_system, new LocalNode(_system), connection, null, null, new ChannelsConfig()));
            var node = actor.UnderlyingActor;
            actor.Tell(Message.Create(MessageCommand.Alert));
            actor.Tell(Message.Create(MessageCommand.Inv));
            actor.Tell(new Tcp.Received(ByteString.FromBytes(new byte[] { 0 })));
            Assert.IsGreaterThan(0L, Field<long>(node, "_queuedBytes"));
            Assert.AreEqual(1, Field<ByteString>(node, "_messageBuffer").Count);

            // Close is handled by Connection, which must dispatch to the override.
            actor.Tell(new Connection.Close { Abort = abort });
            if (abort)
                connection.ExpectMsg<Tcp.Abort>(cancellationToken: CancellationToken.None);
            else
                connection.ExpectMsg<Tcp.Close>(cancellationToken: CancellationToken.None);
            Assert.IsEmpty(Field<Queue<Message>>(node, "_messageQueueHigh"));
            Assert.IsEmpty(Field<Queue<Message>>(node, "_messageQueueLow"));
            Assert.AreEqual(0L, Field<long>(node, "_queuedBytes"));
            Assert.AreEqual(0, Field<ByteString>(node, "_messageBuffer").Count);
        }

        [TestMethod]
        public void OutboundQueue_CountLimitIsSharedAcrossPriorities()
        {
            var connection = CreateTestProbe();
            var actor = ActorOfAsTestActorRef(() => new RemoteNode(_system, new LocalNode(_system), connection, null, null, new ChannelsConfig()));
            var node = actor.UnderlyingActor;
            for (var i = 0; i < RemoteNode.MaxQueuedMessages; i++)
                actor.Tell(Message.Create(i % 2 == 0 ? MessageCommand.Alert : MessageCommand.Inv));
            Assert.HasCount(RemoteNode.MaxQueuedMessages / 2, Field<Queue<Message>>(node, "_messageQueueHigh"));
            Assert.HasCount(RemoteNode.MaxQueuedMessages / 2, Field<Queue<Message>>(node, "_messageQueueLow"));
            actor.Tell(Message.Create(MessageCommand.Inv));
            connection.ExpectMsg<Tcp.Abort>(cancellationToken: CancellationToken.None);
            Assert.AreEqual(0L, Field<long>(node, "_queuedBytes"));
            Assert.IsEmpty(Field<Queue<Message>>(node, "_messageQueueHigh"));
            Assert.IsEmpty(Field<Queue<Message>>(node, "_messageQueueLow"));
        }

        [TestMethod]
        public void OutboundQueue_ByteLimitCountsUncompressedPayload()
        {
            var connection = CreateTestProbe();
            var actor = ActorOfAsTestActorRef(() => new RemoteNode(_system, new LocalNode(_system), connection, null, null, new ChannelsConfig()));
            var node = actor.UnderlyingActor;
            var message = Message.Create(MessageCommand.Transaction, new Transaction
            {
                Signers = [],
                Attributes = [],
                Witnesses = [],
                Script = new byte[262144]
            });
            Assert.IsTrue(message.IsCompressed);
            Assert.IsGreaterThan(message.Size, message.QueueSize);
            var count = (int)(RemoteNode.MaxQueuedBytes / message.QueueSize);
            Assert.IsLessThan(RemoteNode.MaxQueuedMessages, count);
            for (var i = 0; i < count; i++) actor.Tell(message);
            Assert.AreEqual(count * message.QueueSize, Field<long>(node, "_queuedBytes"));
            actor.Tell(message);
            connection.ExpectMsg<Tcp.Abort>(cancellationToken: CancellationToken.None);
            Assert.AreEqual(0L, Field<long>(node, "_queuedBytes"));
        }

        [TestMethod]
        public void OutboundQueue_RepeatedWireRequestsAbortWithoutWriteAcknowledgements()
        {
            var connection = CreateTestProbe();
            var actor = ActorOfAsTestActorRef(() => new RemoteNode(_system, new LocalNode(_system), connection,
                new IPEndPoint(IPAddress.Loopback, 12345), new IPEndPoint(IPAddress.Loopback, 20333), new ChannelsConfig()));
            var node = actor.UnderlyingActor;
            var version = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "queue-test",
                Nonce = 1,
                Network = TestProtocolSettings.Default.Network,
                Version = LocalNode.ProtocolVersion,
                Capabilities = []
            });
            actor.Tell(new Tcp.Received(ByteString.FromBytes(version.ToArray())));
            connection.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);
            actor.Tell(new Tcp.Received(ByteString.FromBytes(Message.Create(MessageCommand.Verack).ToArray())));
            var request = Message.Create(MessageCommand.GetBlockByIndex, GetBlockByIndexPayload.Create(0, 500)).ToArray();
            // Include more frames after overflow in the same TCP delivery.
            var batch = Enumerable.Range(0, RemoteNode.MaxQueuedMessages + 20).SelectMany(_ => request).ToArray();
            actor.Tell(new Tcp.Received(ByteString.FromBytes(batch)));
            connection.ExpectMsg<Tcp.Abort>(cancellationToken: CancellationToken.None);
            Assert.IsEmpty(Field<Queue<Message>>(node, "_messageQueueLow"));
            Assert.AreEqual(0, Field<ByteString>(node, "_messageBuffer").Count);
        }

        [TestMethod]
        public void OutboundQueue_AckReleasesBudgetAndPreservesPriority()
        {
            var connection = CreateTestProbe();
            var actor = ActorOfAsTestActorRef(() => new RemoteNode(_system, new LocalNode(_system), connection, null, null, new ChannelsConfig()));
            var node = actor.UnderlyingActor;
            var low = Message.Create(MessageCommand.Inv);
            var high = Message.Create(MessageCommand.Alert);
            actor.Tell(low);
            actor.Tell(high);
            typeof(RemoteNode).GetField("_verack", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(node, true);
            actor.Tell(Connection.Ack.Instance);
            var write = connection.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);
            Assert.AreSequenceEqual(high.ToArray(), [.. write.Data]);
            Assert.AreEqual(low.QueueSize, Field<long>(node, "_queuedBytes"));
            actor.Tell(Connection.Ack.Instance);
            write = connection.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);
            Assert.AreSequenceEqual(low.ToArray(), [.. write.Data]);
            Assert.AreEqual(0L, Field<long>(node, "_queuedBytes"));
            Sys.Stop(actor);
        }

        [TestMethod]
        public void RemoteNode_Test_Accept_IfSameNetwork()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() =>
                new RemoteNode(_system,
                    new LocalNode(_system),
                    connectionTestProbe,
                    new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8080), new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080), new ChannelsConfig()));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload()
            {
                UserAgent = "Unit Test".PadLeft(1024, '0'),
                Nonce = 1,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 5,
                Version = 6,
                Capabilities =
                [
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                ]
            });

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, new Tcp.Received((ByteString)msg.ToArray()));

            var verackMessage = connectionTestProbe.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);

            //Verack
            Assert.HasCount(3, verackMessage.Data);
        }
    }
}
