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
        private TestProbe senderProbe;
        private TestProbe parent;
        private TestActorRef<ProtocolHandler> protocolActor;

        [TestCleanup]
        public void Cleanup()
        {
            Shutdown();
        }

        [TestInitialize]
        public void TestSetup()
        {
            testBlockchain = TestBlockchain.InitializeMockNeoSystem();

            senderProbe = CreateTestProbe();
            parent = CreateTestProbe();
            protocolActor = ActorOfAsTestActorRef(() => new ProtocolHandler(testBlockchain), parent);

            // Init protocol sending VersionPayload

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

            senderProbe.Send(protocolActor, Message.Create(MessageCommand.Verack));
            Assert.AreEqual(parent.ExpectMsg<MessageCommand>(), MessageCommand.Verack);
        }

        [TestMethod]
        public void Test_OnGetDataMessageReceived()
        {
            var consensus = new ConsensusPayload[2]
            {
                TestUtils.CreateConsensusPayload(),
                TestUtils.CreateConsensusPayload()
            };
            Blockchain.Singleton.ConsensusRelayCache.AddRange(consensus);

            senderProbe.Send(protocolActor, Message.Create(MessageCommand.GetData, new InvPayload()
            {
                Hashes = consensus.Select(u => u.Hash).ToArray(),
                Type = InventoryType.Consensus
            }));

            var msg = parent.ExpectMsg<Message>();
            Assert.AreEqual(MessageCommand.BulkInv, msg.Command);
            Assert.IsInstanceOfType(msg.Payload, typeof(BulkInvPayload));

            // Clean singleton after the use
            Blockchain.Singleton.ConsensusRelayCache.Clear();
        }
    }
}
