using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;

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
        public void ProtocolHandler_Test_SendDisconnection_TellParent()
        {
            var senderProbe = CreateTestProbe();
            var parent = CreateTestProbe();
            var protocolActor = ActorOfAsTestActorRef(() => new ProtocolHandler(testBlockchain), parent);

            // send version
            var versionPayload = new VersionPayload()
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

            senderProbe.Send(protocolActor, Message.Create(MessageCommand.Version, versionPayload));
            parent.ExpectMsg<VersionPayload>();

            // send verack
            senderProbe.Send(protocolActor, Message.Create(MessageCommand.Verack));
            parent.ExpectMsg<MessageCommand>();

            // send disconnection
            var disconnectionPayload = DisconnectPayload.Create(DisconnectReason.ConnectionTimeout, "test message");
            senderProbe.Send(protocolActor, Message.Create(MessageCommand.Disconnect, disconnectionPayload));
            parent.ExpectMsg<DisconnectPayload>();
        }
    }
}
