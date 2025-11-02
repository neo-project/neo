// Copyright (C) 2015-2025 The Neo Project.
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
using Akka.TestKit;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Extensions.Factories;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_RemoteNode : TestKit
    {
        private static NeoSystem _system;
        private static int s_accountSeed;

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

        [TestMethod]
        public void RemoteNode_SendsMerkleBlock_WhenBloomFilterIsLoaded()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var block = PersistBlock(testSystem, 2);

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(16, 5, 42);
            filter.Add(block.Transactions[0].Hash.ToArray());
            var filterLoad = Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter));
            remoteNode.Tell(new Tcp.Received((ByteString)filterLoad.ToArray()));

            var request = Message.Create(MessageCommand.GetData, InvPayload.Create(InventoryType.Block, block.Hash));
            remoteNode.Tell(new Tcp.Received((ByteString)request.ToArray()));

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.MerkleBlock);
            var payload = (MerkleBlockPayload)response.Payload;
            Assert.AreEqual(block.Hash, payload.Header.Hash);
            Assert.AreEqual(block.Transactions.Length, payload.TxCount);

            var flags = new BitArray(payload.Flags.ToArray());
            Assert.IsTrue(flags.Cast<bool>().Any(b => b));
            Assert.IsTrue(flags[0]);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_SendsBlock_WhenFilterCleared()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var blockWithFilter = PersistBlock(testSystem, 2);
            var blockAfterClear = PersistBlock(testSystem, 1);

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(16, 5, 21);
            filter.Add(blockWithFilter.Transactions[0].Hash.ToArray());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.GetData, InvPayload.Create(InventoryType.Block, blockWithFilter.Hash)).ToArray()));
            var firstResponse = ExpectCommand(connectionProbe, remoteNode, MessageCommand.MerkleBlock);
            connectionProbe.Send(remoteNode, Connection.Ack.Instance);

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterClear).ToArray()));

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.GetData, InvPayload.Create(InventoryType.Block, blockAfterClear.Hash)).ToArray()));
            var secondResponse = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Block);

            var returnedBlock = (Block)secondResponse.Payload;
            Assert.AreEqual(blockAfterClear.Hash, returnedBlock.Hash);
            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_FilterAdd_UpdatesBloomFilter()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var initialBlock = PersistBlock(testSystem, 2);
            var blockAfterAdd = PersistBlock(testSystem, 2);

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(16, 5, 7);
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.GetData, InvPayload.Create(InventoryType.Block, initialBlock.Hash)).ToArray()));
            var initialResponse = ExpectCommand(connectionProbe, remoteNode, MessageCommand.MerkleBlock);

            var initialFlags = new BitArray(((MerkleBlockPayload)initialResponse.Payload).Flags.ToArray());
            Assert.IsFalse(initialFlags.Cast<bool>().Any(b => b));
            connectionProbe.Send(remoteNode, Connection.Ack.Instance);

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterAdd, new FilterAddPayload { Data = blockAfterAdd.Transactions[0].Hash.ToArray() }).ToArray()));

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.GetData, InvPayload.Create(InventoryType.Block, blockAfterAdd.Hash)).ToArray()));
            var updatedResponse = ExpectCommand(connectionProbe, remoteNode, MessageCommand.MerkleBlock);

            var updatedFlags = new BitArray(((MerkleBlockPayload)updatedResponse.Payload).Flags.ToArray());
            Assert.IsTrue(updatedFlags.Cast<bool>().Any(b => b));
            Assert.IsTrue(updatedFlags[0]);
            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_DoesNotRelayTransactionsOutsideFilter()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var emptyFilter = FilterLoadPayload.Create(new BloomFilter(128, 5, 1234));
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, emptyFilter).ToArray()));

            remoteNode.Tell(new RemoteNode.Relay { Inventory = transaction });

            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Inv, TimeSpan.FromMilliseconds(100));
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_RelaysTransactionsMatchingFilter()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 5678);
            filter.Add(transaction.Hash.ToArray());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(new RemoteNode.Relay { Inventory = transaction });

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Inv);
            var invPayload = (InvPayload)response.Payload;
            Assert.AreEqual(InventoryType.TX, invPayload.Type);
            Assert.AreEqual(1, invPayload.Hashes.Length);
            Assert.AreEqual(transaction.Hash, invPayload.Hashes[0]);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_RelaysTransactionsMatchingSigner()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 8765);
            filter.Add(transaction.Signers[0].Account.ToArray());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(new RemoteNode.Relay { Inventory = transaction });

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Inv);
            var invPayload = (InvPayload)response.Payload;
            Assert.AreEqual(InventoryType.TX, invPayload.Type);
            Assert.AreEqual(1, invPayload.Hashes.Length);
            Assert.AreEqual(transaction.Hash, invPayload.Hashes[0]);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_DoesNotSendTransactionsOutsideFilter()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var emptyFilter = FilterLoadPayload.Create(new BloomFilter(128, 5, 4321));
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, emptyFilter).ToArray()));

            remoteNode.Tell(transaction);

            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Transaction, TimeSpan.FromMilliseconds(100));
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_SendsTransactionsMatchingFilter()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 2468);
            filter.Add(transaction.Hash.ToArray());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(transaction);

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Transaction);
            var forwarded = (Transaction)response.Payload;
            Assert.AreEqual(transaction.Hash, forwarded.Hash);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_SendsTransactionsMatchingSigner()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 9753);
            filter.Add(transaction.Signers[0].Account.ToArray());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(transaction);

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Transaction);
            var forwarded = (Transaction)response.Payload;
            Assert.AreEqual(transaction.Hash, forwarded.Hash);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_RelaysTransactionAfterFilterClear()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 3141);
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(new RemoteNode.Relay { Inventory = transaction });
            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Inv, TimeSpan.FromMilliseconds(100));

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterClear).ToArray()));
            remoteNode.Tell(new RemoteNode.Relay { Inventory = transaction });

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Inv);
            var payload = (InvPayload)response.Payload;
            Assert.AreEqual(transaction.Hash, payload.Hashes.Single());

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_SendsTransactionAfterFilterClear()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 2718);
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(transaction);
            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Transaction, TimeSpan.FromMilliseconds(100));

            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterClear).ToArray()));
            remoteNode.Tell(transaction);

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Transaction);
            var forwarded = (Transaction)response.Payload;
            Assert.AreEqual(transaction.Hash, forwarded.Hash);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_RelaysBlocksRegardlessOfFilter()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var block = PersistBlock(testSystem, 1);

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 1111);
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(new RemoteNode.Relay { Inventory = block });

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Inv);
            var payload = (InvPayload)response.Payload;
            Assert.AreEqual(InventoryType.Block, payload.Type);
            CollectionAssert.Contains(payload.Hashes, block.Hash);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_SendsBlocksRegardlessOfFilter()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var block = PersistBlock(testSystem, 1);

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var filter = new BloomFilter(128, 5, 2222);
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(block);

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Block);
            var payload = (Block)response.Payload;
            Assert.AreEqual(block.Hash, payload.Hash);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_DoesNotRelayBlocksWhenNotFullNode()
        {
            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe, includeFullNode: false);

            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var block = PersistBlock(testSystem, 1);
            remoteNode.Tell(new RemoteNode.Relay { Inventory = block });

            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Inv, TimeSpan.FromMilliseconds(100));
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_DoesNotSendBlocksWhenNotFullNode()
        {
            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe, includeFullNode: false);

            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();
            var block = PersistBlock(testSystem, 1);
            remoteNode.Tell(block);

            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Block, TimeSpan.FromMilliseconds(100));
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_DoesNotRelayWhenNotFullNode()
        {
            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe, includeFullNode: false);

            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());
            var filter = new BloomFilter(128, 5, 6006);
            filter.Add(transaction.Hash.ToArray());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(new RemoteNode.Relay { Inventory = transaction });
            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Inv, TimeSpan.FromMilliseconds(100));

            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_DoesNotSendWhenNotFullNode()
        {
            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe, includeFullNode: false);

            var transaction = TestUtils.GetTransaction(CreateUniqueAccount());
            var filter = new BloomFilter(128, 5, 7007);
            filter.Add(transaction.Hash.ToArray());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.FilterLoad, FilterLoadPayload.Create(filter)).ToArray()));

            remoteNode.Tell(transaction);
            AssertNoCommand(connectionProbe, remoteNode, MessageCommand.Transaction, TimeSpan.FromMilliseconds(100));

            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_RespondsToPingWithPong()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            var ping = PingPayload.Create(0, RandomNumberFactory.NextUInt32());
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.Ping, ping).ToArray()));

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.Pong);
            var pong = (PingPayload)response.Payload;
            Assert.AreEqual(ping.Nonce, pong.Nonce);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        [TestMethod]
        public void RemoteNode_ReturnsNotFoundWhenDataUnavailable()
        {
            var testSystem = (TestBlockchain.TestNeoSystem)_system;
            testSystem.ResetStore();

            var connectionProbe = CreateTestProbe();
            var remoteNode = SpawnRemoteNode(connectionProbe);
            PerformHandshake(remoteNode, connectionProbe);

            Span<byte> buffer = stackalloc byte[UInt256.Length];
            RandomNumberGenerator.Fill(buffer);
            var hash = new UInt256(buffer);
            var request = InvPayload.Create(InventoryType.Block, hash);
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.GetData, request).ToArray()));

            var response = ExpectCommand(connectionProbe, remoteNode, MessageCommand.NotFound);
            var payload = (InvPayload)response.Payload;
            Assert.AreEqual(InventoryType.Block, payload.Type);
            CollectionAssert.Contains(payload.Hashes, hash);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            Sys.Stop(remoteNode);
        }

        private TestActorRef<RemoteNode> SpawnRemoteNode(TestProbe connectionProbe)
        {
            return ActorOfAsTestActorRef(() =>
                new RemoteNode(_system,
                    new LocalNode(_system),
                    connectionProbe,
                    new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8080),
                    new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080),
                    new ChannelsConfig()));
        }

        private void PerformHandshake(TestActorRef<RemoteNode> remoteNode, TestProbe connectionProbe, bool includeFullNode = true)
        {
            remoteNode.Tell(new Tcp.Received((ByteString)CreateVersionMessage(includeFullNode).ToArray()));
            var verack = connectionProbe.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);
            var response = Deserialize(verack.Data);
            Assert.AreEqual(MessageCommand.Verack, response.Command);

            connectionProbe.Send(remoteNode, Connection.Ack.Instance);
            remoteNode.Tell(new Tcp.Received((ByteString)Message.Create(MessageCommand.Verack).ToArray()));
        }

        private static Message CreateVersionMessage(bool includeFullNode)
        {
            var nonce = unchecked(LocalNode.Nonce + 1);
            var capabilities = includeFullNode
                ? new NodeCapability[]
                {
                    new FullNodeCapability(1),
                    new ServerCapability(NodeCapabilityType.TcpServer, 20333)
                }
                : new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 20333)
                };
            var payload = VersionPayload.Create(
                TestProtocolSettings.Default.Network,
                nonce,
                "/unit-test/",
                capabilities);
            return Message.Create(MessageCommand.Version, payload);
        }

        private static Block PersistBlock(TestBlockchain.TestNeoSystem system, int transactionCount)
        {
            using var snapshot = system.GetSnapshotCache();
            var prevHash = NativeContract.Ledger.CurrentHash(snapshot);

            var header = TestUtils.MakeHeader(snapshot, prevHash);
            var transactions = new Transaction[transactionCount];
            for (int i = 0; i < transactionCount; i++)
            {
                transactions[i] = TestUtils.GetTransaction(CreateUniqueAccount());
            }

            var block = new Block
            {
                Header = header,
                Transactions = transactions
            };
            header.MerkleRoot = MerkleTree.ComputeRoot(block.Transactions.Select(p => p.Hash).ToArray());
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();
            return block;
        }

        private static UInt160 CreateUniqueAccount()
        {
            var value = Interlocked.Increment(ref s_accountSeed);
            return UInt160.Parse($"0x{value.ToString("x40")}");
        }

        private static Message Deserialize(ByteString data)
        {
            var consumed = Message.TryDeserialize(data, out var message);
            Assert.IsNotNull(message);
            Assert.AreNotEqual(0, consumed);
            return message;
        }

        private void AssertNoCommand(TestProbe connectionProbe, TestActorRef<RemoteNode> remoteNode, MessageCommand disallowed, TimeSpan duration)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < duration)
            {
                var remaining = duration - stopwatch.Elapsed;
                var timeout = remaining < TimeSpan.FromMilliseconds(10) ? remaining : TimeSpan.FromMilliseconds(10);
                if (timeout <= TimeSpan.Zero)
                    break;
                var message = connectionProbe.ReceiveOne(timeout);
                if (message is not Tcp.Write write)
                    continue;

                var parsed = Deserialize(write.Data);
                connectionProbe.Send(remoteNode, Connection.Ack.Instance);
                Assert.AreNotEqual(disallowed, parsed.Command);
            }
        }

        private Message ExpectCommand(TestProbe connectionProbe, TestActorRef<RemoteNode> remoteNode, MessageCommand command)
        {
            for (int i = 0; i < 20; i++)
            {
                var envelope = connectionProbe.ReceiveOne(TimeSpan.FromSeconds(1));
                if (envelope is not Tcp.Write write)
                {
                    if (envelope is null)
                        break;
                    continue;
                }

                var message = Deserialize(write.Data);
                connectionProbe.Send(remoteNode, Connection.Ack.Instance);
                if (message.Command == command)
                    return message;
            }

            Assert.Fail($"Expected to receive {command} but it was not observed.");
            return null;
        }
    }
}
