using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System.Linq;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_ProtocolHandler : TestKit
    {
        private NeoSystem testBlockchain;

        private void SendVersion(TestProbe sender, TestProbe parent, TestActorRef<ProtocolHandler> protocolActor)
        {
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

            sender.Send(protocolActor, Message.Create(MessageCommand.Version, payload));
            parent.ExpectMsg<VersionPayload>();

            sender.Send(protocolActor, Message.Create(MessageCommand.Verack));
            Assert.AreEqual(parent.ExpectMsg<MessageCommand>(), MessageCommand.Verack);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestInitialize]
        public void TestSetup()
        {
            testBlockchain = TestBlockchain.InitializeMockNeoSystem();
        }

        [TestMethod]
        public void Test_SendVersion_TellParent()
        {
            var senderProbe = CreateTestProbe();
            var parent = CreateTestProbe();
            var protocolActor = ActorOfAsTestActorRef(() => new ProtocolHandler(testBlockchain), parent);

            SendVersion(senderProbe, parent, protocolActor);
        }

        [TestMethod]
        public void Test_OnGetDataMessageReceived()
        {
            var senderProbe = CreateTestProbe();
            var parent = CreateTestProbe();
            var protocolActor = ActorOfAsTestActorRef(() => new ProtocolHandler(testBlockchain), parent);

            // Init protocol
            SendVersion(senderProbe, parent, protocolActor);

            var consensus = new ConsensusPayload[2] { TestUtils.CreateConsensusPayload(), TestUtils.CreateConsensusPayload() };

            Blockchain.Singleton.ConsensusRelayCache.AddRange(consensus);

            senderProbe.Send(protocolActor, Message.Create(MessageCommand.GetData, new InvPayload()
            {
                Hashes = consensus.Select(u => u.Hash).ToArray(),
                Type = InventoryType.Consensus
            }));

            var msg = parent.ExpectMsg<Message>();
            Assert.AreEqual(MessageCommand.BulkInv, msg.Command);
            Assert.IsInstanceOfType(msg.Payload, typeof(BulkInvPayload));
        }
    }
}
