using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.Linq;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass, NotReRunnable]
    public class UT_ProtocolHandler : TestKit
    {
        private Store testStore;
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
            testStore = TestBlockchain.GetStore();

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
        public void Test_OnGetDataMessageReceived_Consensus()
        {
            var entries = new ConsensusPayload[]
            {
                TestUtils.CreateConsensusPayload(),
                TestUtils.CreateConsensusPayload()
            };

            // Fill cache
            Blockchain.Singleton.ConsensusRelayCache.AddRange(entries);

            // Test
            senderProbe.Send(protocolActor, Message.Create(MessageCommand.GetData, new InvPayload()
            {
                Hashes = entries.Select(u => u.Hash).ToArray(),
                Type = InventoryType.Consensus
            }));

            var msg = parent.ExpectMsg<Message>();
            Assert.AreEqual(MessageCommand.BulkInv, msg.Command);
            Assert.IsInstanceOfType(msg.Payload, typeof(BulkInvPayload));
            Assert.AreEqual(InventoryType.Consensus, ((BulkInvPayload)msg.Payload).Type);
            Assert.AreEqual(2, ((BulkInvPayload)msg.Payload).Values.Length);

            // Clean cache after the use
            Blockchain.Singleton.ConsensusRelayCache.Clear();
        }

        [TestMethod]
        public void Test_OnGetDataMessageReceived_Transaction()
        {
            var entries = new Transaction[]
            {
                TestUtils.CreateTransaction(),
                TestUtils.CreateTransaction()
            };

            // Fill mempool
            foreach (var entry in entries)
            {
                Blockchain.Singleton.MemPool.TryAdd(entry.Hash, entry);
            }

            // Test
            senderProbe.Send(protocolActor, Message.Create(MessageCommand.GetData, new InvPayload()
            {
                Hashes = entries.Select(u => u.Hash).ToArray(),
                Type = InventoryType.TX
            }));

            var msg = parent.ExpectMsg<Message>();
            Assert.AreEqual(MessageCommand.BulkInv, msg.Command);
            Assert.IsInstanceOfType(msg.Payload, typeof(BulkInvPayload));
            Assert.AreEqual(InventoryType.TX, ((BulkInvPayload)msg.Payload).Type);
            Assert.AreEqual(2, ((BulkInvPayload)msg.Payload).Values.Length);

            // Clean mempool
            foreach (var entry in entries)
            {
                Blockchain.Singleton.MemPool.TryRemoveUnVerified(entry.Hash, out var item);
            }
        }

        [TestMethod]
        public void Test_OnGetDataMessageReceived_Block()
        {
            var entries = new Block[]
            {
                TestUtils.CreateBlock(1),
                TestUtils.CreateBlock(2),
                TestUtils.CreateBlock(3)
            };

            // Fake blocks
            TestDataCache<UInt256, TrimmedBlock> blocks = (TestDataCache<UInt256, TrimmedBlock>)testStore.GetBlocks();
            foreach (var entry in entries)
            {
                blocks.Add(entry.Hash, entry.Trim());
            }

            // Test
            senderProbe.Send(protocolActor, Message.Create(MessageCommand.GetData, new InvPayload()
            {
                Hashes = entries.Select(u => u.Hash).ToArray(),
                Type = InventoryType.Block
            }));

            var msg = parent.ExpectMsg<Message>();
            Assert.AreEqual(MessageCommand.BulkInv, msg.Command);
            Assert.IsInstanceOfType(msg.Payload, typeof(BulkInvPayload));
            Assert.AreEqual(InventoryType.Block, ((BulkInvPayload)msg.Payload).Type);
            Assert.AreEqual(3, ((BulkInvPayload)msg.Payload).Values.Length);

            // Clean fake blocks
            foreach (var entry in entries)
            {
                blocks.Delete(entry.Hash);
            }
        }
    }
}
