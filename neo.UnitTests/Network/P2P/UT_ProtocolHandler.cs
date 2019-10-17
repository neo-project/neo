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
    public class UT_ProtocolHandler : TestKit
    {
        private static NeoSystem testBlockchain;

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            testBlockchain = TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void ProtocolHandler_Test_SendVersion_TellParent()
        {
            var senderProbe = CreateTestProbe();
            var parent = CreateTestProbe();
            var protocolActor = ActorOfAsTestActorRef(() => new ProtocolHandler(testBlockchain), parent);

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

            senderProbe.Send(protocolActor, Message.Create(MessageCommand.Version, payload));
            parent.ExpectMsg<VersionPayload>();
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
            var protocolActor = ActorOfAsTestActorRef(() => new ProtocolHandler(testBlockchain));
            var disconnectionPayload = DisconnectPayload.Create(DisconnectReason.MaxConnectionReached, addressWithTimes.ToByteArray());
            var senderProbe = CreateTestProbe();
            senderProbe.Send(protocolActor, Message.Create(MessageCommand.Disconnect, disconnectionPayload));

            senderProbe.ExpectNoMsg();
            LocalNode.Singleton.GetUnconnectedPeers().Any(p => p.Equals(new IPEndPoint(IPAddress.Parse("192.166.1.1"), 8080))).Should().BeTrue();
        }
    }
}
