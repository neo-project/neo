using Akka.IO;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System.Net;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public partial class UT_RemoteNode : TestKit
    {
        private static NeoSystem testBlockchain;

        public UT_RemoteNode()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            testBlockchain = TestBlockchain.TheNeoSystem;
        }

        [TestMethod]
        public void RemoteNode_Test_Abort_DifferentMagic()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Magic = 2,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            });

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, new Tcp.Received((ByteString)msg.ToArray()));

            var tcpWrite = connectionTestProbe.ExpectMsg<Tcp.Write>();
            Message message = tcpWrite.Data.ToArray().AsSerializable<Message>();
            message.Command.Should().Be(MessageCommand.Disconnect);

            var disconnectionPayload = (DisconnectPayload)message.Payload;
            disconnectionPayload.Reason.Should().Be(DisconnectReason.MagicNumberIncompatible);

            testProbe.Send(remoteNodeActor, RemoteNode.Ack.Instance);
            connectionTestProbe.ExpectMsg<Tcp.Abort>();
        }

        [TestMethod]
        public void RemoteNode_Test_Accept_IfSameMagic()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload()
            {
                UserAgent = "Unit Test".PadLeft(1024, '0'),
                Nonce = 1,
                Magic = ProtocolSettings.Default.Magic,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            });

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, new Tcp.Received((ByteString)msg.ToArray()));

            var verackMessage = connectionTestProbe.ExpectMsg<Tcp.Write>();

            //Verack
            verackMessage.Data.Count.Should().Be(3);
        }

        [TestMethod]
        public void RemoteNode_Test_Received_Duplicate_Connection()
        {
            var connectionTestProbeA = CreateTestProbe();
            var Remote = new IPEndPoint(IPAddress.Parse("192.168.255.255"), 8080);
            var payload = new VersionPayload()
            {
                UserAgent = "Unit Test".PadLeft(1024, '0'),
                Nonce = 1,
                Magic = ProtocolSettings.Default.Magic,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            };
            var tcpData = new Tcp.Received((ByteString)Message.Create(MessageCommand.Version, payload).ToArray());

            // send to remote node A
            var remoteNodeActorA = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbeA, Remote, null));
            remoteNodeActorA.Tell(new RemoteNode.StartProtocol());
            connectionTestProbeA.ExpectMsg<Tcp.Write>(); // remote node A will send version message

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActorA, tcpData);

            // set remote node B with the same address
            var connectionTestProbeB = CreateTestProbe();
            var remoteNodeActorB = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbeB, Remote, null));
            remoteNodeActorB.Tell(new RemoteNode.StartProtocol());
            connectionTestProbeB.ExpectMsg<Tcp.Write>();    // remote node B will send version message

            var testProbeB = CreateTestProbe();
            testProbeB.Send(remoteNodeActorB, tcpData); // send a version message to remote node B, and B will disconnect with `DuplicateConnection`

            var tcpWrite = connectionTestProbeB.ExpectMsg<Tcp.Write>();
            var message = tcpWrite.Data.ToArray().AsSerializable<Message>();
            message.Command.Should().Be(MessageCommand.Disconnect);

            var disconnectionPayload = (DisconnectPayload)message.Payload;
            disconnectionPayload.Reason.Should().Be(DisconnectReason.DuplicateNonce);

            testProbe.Send(remoteNodeActorB, RemoteNode.Ack.Instance);
            connectionTestProbeB.ExpectMsg<Tcp.Abort>();
        }
    }
}
