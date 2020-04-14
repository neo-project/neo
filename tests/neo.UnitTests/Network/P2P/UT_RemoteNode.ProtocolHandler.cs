using Akka.IO;
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
    public partial class UT_RemoteNode
    {
        [TestMethod]
        public void ProtocolHandler_Test_SendVersion_ReturnVerack()
        {
            var senderProbe = CreateTestProbe();
            var parent = CreateTestProbe();
            var connectionTestProbe = CreateTestProbe();
            var protocolActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, IPEndPoint.Parse("192.168.1.2:8080"), IPEndPoint.Parse("192.168.1.1:8080")));

            var payload = new VersionPayload()
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Magic = ProtocolSettings.Default.Magic,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            };
            var message = Message.Create(MessageCommand.Version, payload);
            var tcpData = new Tcp.Received((ByteString)message.ToArray());

            senderProbe.Send(protocolActor, tcpData);
            var tcpWrite = connectionTestProbe.ExpectMsg<Tcp.Write>();
            var receivedMsg = tcpWrite.Data.ToArray().AsSerializable<Message>();
            receivedMsg.Command.Should().Be(MessageCommand.Verack);
        }

        [TestMethod]
        public void ProtocolHandler_Test_SendDisconnection_Tell_LocalNode()
        {
            // send disconnection message as max connection reached
            var addressWithTimes = new NetworkAddressWithTime[]
            {
                new NetworkAddressWithTime()
                {
                    Timestamp = 0,
                    Address = IPAddress.Parse("192.166.1.1"),
                    Capabilities = new NodeCapability[]
                    {
                        new ServerCapability(NodeCapabilityType.TcpServer, 8080)
                    }
                }
            };
            var connectionTestProbe = CreateTestProbe();
            var protocolActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, connectionTestProbe, null, null));
            var disconnectionPayload = DisconnectPayload.Create(DisconnectReason.MaxConnectionReached, addressWithTimes.ToByteArray());
            var tcpData = new Tcp.Received((ByteString)Message.Create(MessageCommand.Disconnect, disconnectionPayload).ToArray());
            var senderProbe = CreateTestProbe();
            senderProbe.Send(protocolActor, tcpData);

            senderProbe.ExpectNoMsg();
            LocalNode.Singleton.GetUnconnectedPeers().Any(p => p.Equals(new IPEndPoint(IPAddress.Parse("192.166.1.1"), 8080))).Should().BeTrue();
        }
    }
}
