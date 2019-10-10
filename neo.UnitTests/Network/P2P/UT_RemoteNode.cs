using Akka.IO;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System.Linq;
using System.Net;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    [NotReRunnable]
    public class UT_RemoteNode : TestKit
    {
        private static NeoSystem testBlockchain;

        public UT_RemoteNode()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}" +
                $"protocol-handler-mailbox {{ mailbox-type: \"{typeof(ProtocolHandlerMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            testBlockchain = TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void RemoteNode_Test_Abort_DifferentMagic()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));

            connectionTestProbe.ExpectMsg<Tcp.Write>();

            var payload = new VersionPayload()
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
            };

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, payload);

            var tcpWrite = connectionTestProbe.ExpectMsg<Tcp.Write>();
            Message message = tcpWrite.Data.ToArray().AsSerializable<Message>();
            message.Command.Should().Be(MessageCommand.Disconnect);

            DisconnectionPayload disconnectionPayload = (DisconnectionPayload)message.Payload;
            disconnectionPayload.Reason.Should().Be(DisconnectionReason.MagicNumberIncompatible);

            connectionTestProbe.ExpectMsg<Tcp.Close>();
        }

        [TestMethod]
        public void RemoteNode_Test_Accept_IfSameMagic()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));

            connectionTestProbe.ExpectMsg<Tcp.Write>();

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

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, payload);

            var verackMessage = connectionTestProbe.ExpectMsg<Tcp.Write>();

            //Verack
            verackMessage.Data.Count.Should().Be(3);
        }

        [TestMethod]
        public void RemoteNode_Test_Received_MaxConnectionReached_Disconnection()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));

            connectionTestProbe.ExpectMsg<Tcp.Write>();     // remote node will send version message
            LocalNode.Singleton.GetUnconnectedPeers().Count().Should().Be(0);

            //send MaxConnectionReached disconnection
            NetworkAddressWithTime[] addressWithTimes = new NetworkAddressWithTime[]
            {
                new NetworkAddressWithTime()
                {
                    Timestamp = 0,
                    Address = IPAddress.Parse("192.168.255.255"),
                    Capabilities = new NodeCapability[]
                    {
                        new ServerCapability(NodeCapabilityType.TcpServer, 8080)
                    }
                }
            };
            var payload = DisconnectionPayload.Create(DisconnectionReason.MaxConnectionReached, "The maximum number of connections reached!", addressWithTimes.ToByteArray());
            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, payload);

            connectionTestProbe.ExpectNoMsg();
            LocalNode.Singleton.GetUnconnectedPeers().Count().Should().Be(1);
            var iPEndPoint = LocalNode.Singleton.GetUnconnectedPeers().First();
            iPEndPoint.Should().Be(new IPEndPoint(addressWithTimes[0].Address, 8080));
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

            // send to remote node A
            var remoteNodeActorA = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbeA, Remote, null));
            connectionTestProbeA.ExpectMsg<Tcp.Write>(); // remote node A will send version message

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActorA, payload);

            // set remote node B with the same address
            var connectionTestProbeB = CreateTestProbe();
            var remoteNodeActorB = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbeB, Remote, null));
            connectionTestProbeB.ExpectMsg<Tcp.Write>();    // remote node B will send version message

            var testProbeB = CreateTestProbe();
            testProbeB.Send(remoteNodeActorB, payload); // send a version message to remote node B, and B will disconnect with `DuplicateConnection`

            var tcpWrite = connectionTestProbeB.ExpectMsg<Tcp.Write>();
            var message = tcpWrite.Data.ToArray().AsSerializable<Message>();
            message.Command.Should().Be(MessageCommand.Disconnect);

            var disconnectionPayload = (DisconnectionPayload)message.Payload;
            disconnectionPayload.Reason.Should().Be(DisconnectionReason.DuplicateConnection);

            connectionTestProbeB.ExpectMsg<Tcp.Close>();
        }

        [TestMethod]
        public void RemoteNode_Test_Received_Invalid_Data()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));
            connectionTestProbe.ExpectMsg<Tcp.Write>(); // remote node will send version message

            // The `length` field of this message is 0x300000000000, which is more than PayloadMaxSize = 0x02000000
            // Message.cs#TryDeserialize will throw a FormatException
            Tcp.Received received = new Tcp.Received(ByteString.FromBytes(new byte[] { 0x02, 0x02, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03 }));
            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, received); // remote node will parse failed

            var tcpWrite = connectionTestProbe.ExpectMsg<Tcp.Write>();
            var message = tcpWrite.Data.ToArray().AsSerializable<Message>();
            message.Command.Should().Be(MessageCommand.Disconnect);

            var disconnectionPayload = (DisconnectionPayload)message.Payload;
            disconnectionPayload.Reason.Should().Be(DisconnectionReason.FormatExcpetion);

            connectionTestProbe.ExpectMsg<Tcp.Close>();
        }

        [TestMethod]
        public void Connection_Test_Timeout()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));
            connectionTestProbe.ExpectMsg<Tcp.Write>(); // remote node will send version message

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, Connection.Timer.Instance); // remote node will disconnect with `ConnectionTimeout`

            var tcpWrite = connectionTestProbe.ExpectMsg<Tcp.Write>();
            var message = tcpWrite.Data.ToArray().AsSerializable<Message>();
            message.Command.Should().Be(MessageCommand.Disconnect);

            var disconnectionPayload = (DisconnectionPayload)message.Payload;
            disconnectionPayload.Reason.Should().Be(DisconnectionReason.ConnectionTimeout);

            connectionTestProbe.ExpectMsg<Tcp.Close>();
        }
    }
}
