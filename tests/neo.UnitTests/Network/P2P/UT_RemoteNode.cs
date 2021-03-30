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
    public class UT_RemoteNode : TestKit
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
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, new LocalNode(testBlockchain), connectionTestProbe, null, null));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Network = 2,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            });

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, new Tcp.Received((ByteString)msg.ToArray()));

            connectionTestProbe.ExpectMsg<Tcp.Abort>();
        }

        [TestMethod]
        public void RemoteNode_Test_Accept_IfSameMagic()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, new LocalNode(testBlockchain), connectionTestProbe, new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8080), new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080)));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload()
            {
                UserAgent = "Unit Test".PadLeft(1024, '0'),
                Nonce = 1,
                Network = ProtocolSettings.Default.Network,
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
    }
}
