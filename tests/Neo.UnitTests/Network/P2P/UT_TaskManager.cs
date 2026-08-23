// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TaskManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.IO;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_TaskManager : TestKit
    {
        private static NeoSystem s_system;

        [ClassInitialize]
        public static void ClassSetup(TestContext _)
        {
            s_system = TestBlockchain.GetSystem();
        }

        private static VersionPayload MakeVersion(uint startHeight)
        {
            return new VersionPayload
            {
                UserAgent = "TaskManagerUT",
                Nonce = 0xBEEF,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 1,
                Version = LocalNode.ProtocolVersion,
                Capabilities = [new FullNodeCapability(startHeight)]
            };
        }

        public UT_TaskManager()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        private static Block CreateBlock(uint index, ulong timestamp = 1)
        {
            return new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = MerkleTree.ComputeRoot([]),
                    Timestamp = timestamp,
                    Index = index,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness()
                },
                Transactions = []
            };
        }

        private static Transaction CreateTransaction(uint nonce = 1)
        {
            return new Transaction
            {
                Nonce = nonce,
                ValidUntilBlock = uint.MaxValue,
                Signers =
                [
                    new Signer { Account = UInt160.Zero }
                ],
                Attributes = [],
                Script = new[] { (byte)OpCode.NOP },
                Witnesses =
                [
                    new Witness()
                ]
            };
        }

        private Akka.TestKit.TestProbe RegisterPeer(Akka.TestKit.TestActorRef<TaskManager> taskManager, uint startHeight)
        {
            var peer = CreateTestProbe();

            peer.Send(
                taskManager,
                new TaskManager.Register(new VersionPayload
                {
                    UserAgent = "local-test",
                    Capabilities =
                    [
                        new FullNodeCapability(startHeight)
                    ]
                }));

            return peer;
        }

        private static Dictionary<IActorRef, TaskSession> GetSessions(Akka.TestKit.TestActorRef<TaskManager> taskManager)
        {
            var sessionsField = typeof(TaskManager).GetField("sessions", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (Dictionary<IActorRef, TaskSession>)sessionsField.GetValue(taskManager.UnderlyingActor)!;
        }

        [TestMethod]
        public void UnsolicitedInWindowBlock_IsTrackedByHashOnly()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));

            var peer = CreateTestProbe();

            peer.Send(
                taskManager,
                new TaskManager.Register(new VersionPayload
                {
                    UserAgent = "local-test",
                    Capabilities =
                    [
                        new FullNodeCapability(currentHeight)
                    ]
                }));

            var block = CreateBlock(currentHeight + 1);
            var session = GetSessions(taskManager)[peer.Ref];

            // No InvTasks/IndexTasks entry is registered: the block is unsolicited.
            peer.Send(taskManager, block);

            Assert.IsTrue(session.ReceivedBlockHashes.TryGetValue(block.Index, out var storedHash),
                "An unsolicited block within the synchronization window must still be tracked by hash.");
            Assert.AreEqual(block.Hash, storedHash);

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void UnsolicitedFarFutureBlock_IsNotTrackedByTaskManager()
        {
            const int TransactionCount = 510;

            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            // Large but extremely compressible script
            var script = new byte[ushort.MaxValue];
            script[0] = (byte)OpCode.NOP;

            var transactions = Enumerable.Range(1, TransactionCount)
                .Select(nonce => new Transaction
                {
                    Nonce = (uint)nonce,
                    ValidUntilBlock = uint.MaxValue,
                    Signers =
                    [
                        new Signer { Account = UInt160.Zero }
                    ],
                    Attributes = [],
                    Script = script,
                    Witnesses =
                    [
                        new Witness()
                    ]
                })
                .ToArray();

            var block = new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = MerkleTree.ComputeRoot([.. transactions.Select(tx => tx.Hash)]),
                    Timestamp = 1,

                    // Height far above the 500 block limit
                    Index = checked(currentHeight + InvPayload.MaxHashesCount + 10_000),

                    NextConsensus = UInt160.Zero,
                    Witness = new Witness()
                },
                Transactions = transactions
            };

            // Serialization and compression through the real P2P path
            var outbound = Message.Create(MessageCommand.Block, block);

            var frame = outbound.ToArray(enablecompression: true);

            Assert.IsTrue(outbound.IsCompressed, "The message should have been compressed.");

            Assert.IsLessThanOrEqualTo(Message.PayloadMaxSize, block.Size, "The block must be within the payload limit.");

            // Decompression and deserialization through the real P2P path
            var consumed = Message.TryDeserialize(ByteString.FromBytes(frame), out var inbound);

            Assert.AreEqual(frame.Length, consumed);

            var parsedBlock = (Block)inbound.Payload;

            Assert.IsLessThan(200_000, frame.Length, $"Unexpected network size: {frame.Length:N0}");

            Assert.IsGreaterThan(33_000_000, parsedBlock.Size, $"Unexpected decompressed size: {parsedBlock.Size:N0}");

            var amplification = (double)parsedBlock.Size / frame.Length;

            Assert.IsGreaterThan(200, amplification, $"Insufficient amplification: {amplification:F1}x");

            // Create a real TaskManager and register a simulated peer
            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));

            var peer = CreateTestProbe();

            peer.Send(
                taskManager,
                new TaskManager.Register(new VersionPayload
                {
                    UserAgent = "local-test",
                    Capabilities =
                    [
                        new FullNodeCapability(currentHeight)
                    ]
                }));

            // Simulate the sending of the block by the peer
            peer.Send(taskManager, parsedBlock);

            // Verify that the deserialized block is not retained by the peer session
            var sessionsField = typeof(TaskManager).GetField("sessions", BindingFlags.Instance | BindingFlags.NonPublic)!;

            var sessions = (Dictionary<IActorRef, TaskSession>)sessionsField.GetValue(taskManager.UnderlyingActor)!;

            var session = sessions[peer.Ref];

            Assert.IsFalse(session.ReceivedBlockHashes.ContainsKey(parsedBlock.Index), "An unsolicited block outside the synchronization window must not be tracked.");

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void BlockRequestedByIndex_IsTrackedUsingOnlyItsHash()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));

            var peer = CreateTestProbe();

            peer.Send(
                taskManager,
                new TaskManager.Register(new VersionPayload
                {
                    UserAgent = "local-test",
                    Capabilities =
                    [
                        new FullNodeCapability(currentHeight)
                    ]
                }));

            var block = CreateBlock(currentHeight + 1);

            var session = GetSessions(taskManager)[peer.Ref];

            // Simulate an index-based request assigned to this peer
            session.IndexTasks.Add(block.Index, TimeProvider.Current.UtcNow);

            peer.Send(taskManager, block);

            Assert.IsFalse(session.IndexTasks.ContainsKey(block.Index), "The completed index task must be removed");
            Assert.IsTrue(session.ReceivedBlockHashes.TryGetValue(block.Index, out var storedHash), "A requested block must be tracked by its index");
            Assert.AreEqual(block.Hash, storedHash, "Only the block hash must be retained");

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void FarFutureBlock_DoesNotUpdateLastBlockIndex()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() =>
                new RemoteNode(neoSystem,
                    new LocalNode(neoSystem),
                    connectionTestProbe,
                    new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8080),
                    new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080),
                    new ChannelsConfig()));

            var remoteNode = remoteNodeActor.UnderlyingActor;

            var versionMessage = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "local-test",
                Nonce = 1,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 5,
                Version = 6,
                Capabilities =
                [
                    new FullNodeCapability(currentHeight)
                ]
            });

            var peer = CreateTestProbe();
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)versionMessage.ToArray()));
            connectionTestProbe.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)Message.Create(MessageCommand.Verack).ToArray()));

            Assert.AreEqual(currentHeight, remoteNode.LastBlockIndex);

            // A block outside the synchronization window must not update LastBlockIndex
            var farFutureBlock = CreateBlock(checked(currentHeight + InvPayload.MaxHashesCount + 10));
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)Message.Create(MessageCommand.Block, farFutureBlock).ToArray()));

            var nextHeight = checked(currentHeight + 1);
            var pong = Message.Create(MessageCommand.Pong, PingPayload.Create(nextHeight, 1));

            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)pong.ToArray()));

            AwaitAssert(
                () => Assert.AreEqual(
                    nextHeight,
                    remoteNode.LastBlockIndex,
                    "The out-of-window block must be ignored before processing the subsequent height update."),
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);

            Sys.Stop(remoteNodeActor);
        }

        [TestMethod]
        public void BlockRequestedByHash_IsTrackedUsingOnlyItsHash()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));

            var peer = CreateTestProbe();

            peer.Send(
                taskManager,
                new TaskManager.Register(new VersionPayload
                {
                    UserAgent = "local-test",
                    Capabilities =
                    [
                        new FullNodeCapability(currentHeight)
                    ]
                }));

            var block = CreateBlock(currentHeight + 1);
            var session = GetSessions(taskManager)[peer.Ref];

            session.InvTasks.Add(block.Hash, TimeProvider.Current.UtcNow);

            peer.Send(taskManager, block);

            Assert.IsFalse(session.InvTasks.ContainsKey(block.Hash));

            Assert.IsTrue(session.ReceivedBlockHashes.TryGetValue(block.Index, out var storedHash));

            Assert.AreEqual(block.Hash, storedHash);

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void DivergentBlockForSameIndex_AbortsPeer()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));
            var peer = RegisterPeer(taskManager, currentHeight);
            var session = GetSessions(taskManager)[peer.Ref];

            var blockA = CreateBlock(currentHeight + 1, timestamp: 1);
            session.IndexTasks.Add(blockA.Index, TimeProvider.Current.UtcNow);
            peer.Send(taskManager, blockA);

            Assert.IsTrue(session.ReceivedBlockHashes.ContainsKey(blockA.Index));

            // A second block for the same index with a different hash must abort the peer.
            var blockB = CreateBlock(currentHeight + 1, timestamp: 2);
            Assert.AreNotEqual(blockA.Hash, blockB.Hash);

            session.IndexTasks.Add(blockB.Index, TimeProvider.Current.UtcNow);
            peer.Send(taskManager, blockB);

            peer.FishForMessage(m => m is Tcp.Abort, TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void DuplicateBlockWithSameHash_IsNotAbortedAndStaysTracked()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));
            var peer = RegisterPeer(taskManager, currentHeight);
            var session = GetSessions(taskManager)[peer.Ref];

            var block = CreateBlock(currentHeight + 1);

            session.IndexTasks.Add(block.Index, TimeProvider.Current.UtcNow);
            peer.Send(taskManager, block);

            // Deliver the same block again for the same index.
            session.IndexTasks.Add(block.Index, TimeProvider.Current.UtcNow);
            peer.Send(taskManager, block);

            Assert.IsTrue(session.ReceivedBlockHashes.TryGetValue(block.Index, out var storedHash));
            Assert.AreEqual(block.Hash, storedHash);

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void Transaction_CompletesInvTask_AndRequestsMoreTasks()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));
            var peer = RegisterPeer(taskManager, currentHeight);
            var session = GetSessions(taskManager)[peer.Ref];

            var tx = CreateTransaction();
            session.InvTasks.Add(tx.Hash, TimeProvider.Current.UtcNow);

            peer.Send(taskManager, tx);

            Assert.IsFalse(session.InvTasks.ContainsKey(tx.Hash), "The completed inventory task must be removed.");
            Assert.IsEmpty(session.ReceivedBlockHashes, "Transactions must not be tracked as received blocks.");

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void InventoryFromUnregisteredPeer_IsIgnored()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));

            var unregisteredPeer = CreateTestProbe();

            // A block from an unregistered peer must be ignored by the task guard.
            unregisteredPeer.Send(taskManager, CreateBlock(currentHeight + 1));

            // A transaction from an unregistered peer completes global bookkeeping and returns.
            unregisteredPeer.Send(taskManager, CreateTransaction());

            Assert.IsFalse(GetSessions(taskManager).ContainsKey(unregisteredPeer.Ref));

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void UnsolicitedInWindowBlock_LaterFoundInvalid_AbortsPeer()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));
            var peer = RegisterPeer(taskManager, currentHeight);
            var session = GetSessions(taskManager)[peer.Ref];

            var block = CreateBlock(currentHeight + 1);

            // No InvTasks/IndexTasks entry: the block is unsolicited but still in-window.
            peer.Send(taskManager, block);

            Assert.IsTrue(session.ReceivedBlockHashes.ContainsKey(block.Index),
                "The unsolicited in-window block must be tracked by hash.");

            peer.Send(taskManager, new Blockchain.RelayResult(block, VerifyResult.Invalid));

            peer.FishForMessage(m => m is Tcp.Abort, TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void InvalidBlock_AbortsThePeerThatSuppliedIt()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));
            var peer = RegisterPeer(taskManager, currentHeight);
            var session = GetSessions(taskManager)[peer.Ref];

            var block = CreateBlock(currentHeight + 1);
            session.IndexTasks.Add(block.Index, TimeProvider.Current.UtcNow);
            peer.Send(taskManager, block);

            Assert.IsTrue(session.ReceivedBlockHashes.ContainsKey(block.Index));

            peer.Send(taskManager, new Blockchain.RelayResult(block, VerifyResult.Invalid));

            peer.FishForMessage(m => m is Tcp.Abort, TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void PersistCompleted_WithMatchingHash_RemovesTrackedBlock()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));
            var peer = RegisterPeer(taskManager, currentHeight);
            var session = GetSessions(taskManager)[peer.Ref];

            var block = CreateBlock(currentHeight + 1);
            session.IndexTasks.Add(block.Index, TimeProvider.Current.UtcNow);
            peer.Send(taskManager, block);

            Assert.IsTrue(session.ReceivedBlockHashes.ContainsKey(block.Index));

            peer.Send(taskManager, new Blockchain.PersistCompleted(block));

            Assert.IsEmpty(session.ReceivedBlockHashes, "The tracked hash must be removed once the height is persisted.");

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void PersistCompleted_WithDivergentHash_AbortsPeer()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var taskManager = ActorOfAsTestActorRef(() => new TaskManager(neoSystem));
            var peer = RegisterPeer(taskManager, currentHeight);
            var session = GetSessions(taskManager)[peer.Ref];

            var receivedBlock = CreateBlock(currentHeight + 1, timestamp: 1);
            session.IndexTasks.Add(receivedBlock.Index, TimeProvider.Current.UtcNow);
            peer.Send(taskManager, receivedBlock);

            Assert.IsTrue(session.ReceivedBlockHashes.ContainsKey(receivedBlock.Index));

            // The chain persists a different block at the same height.
            var persistedBlock = CreateBlock(currentHeight + 1, timestamp: 2);
            Assert.AreNotEqual(receivedBlock.Hash, persistedBlock.Hash);

            peer.Send(taskManager, new Blockchain.PersistCompleted(persistedBlock));

            peer.FishForMessage(m => m is Tcp.Abort, TimeSpan.FromSeconds(3), cancellationToken: CancellationToken.None);

            Sys.Stop(taskManager);
        }

        [TestMethod]
        public void RemoteNode_InWindowBlock_UpdatesLastBlockIndex_AndDuplicateIsIgnored()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() =>
                new RemoteNode(neoSystem,
                    new LocalNode(neoSystem),
                    connectionTestProbe,
                    new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8080),
                    new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080),
                    new ChannelsConfig()));

            var remoteNode = remoteNodeActor.UnderlyingActor;

            var versionMessage = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "local-test",
                Nonce = 1,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 5,
                Version = 6,
                Capabilities =
                [
                    new FullNodeCapability(currentHeight)
                ]
            });

            var peer = CreateTestProbe();
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)versionMessage.ToArray()));
            connectionTestProbe.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)Message.Create(MessageCommand.Verack).ToArray()));

            // An in-window block is accepted, forwarded and updates LastBlockIndex.
            var block = CreateBlock(currentHeight + 1);
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)Message.Create(MessageCommand.Block, block).ToArray()));

            AwaitAssert(
                () => Assert.AreEqual(currentHeight + 1, remoteNode.LastBlockIndex),
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);

            // The same block delivered again is ignored by the known-hash cache.
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)Message.Create(MessageCommand.Block, block).ToArray()));

            Assert.AreEqual(currentHeight + 1, remoteNode.LastBlockIndex);

            Sys.Stop(remoteNodeActor);
        }

        [TestMethod]
        public void RemoteNode_Transaction_IsRoutedForPreverification()
        {
            using var neoSystem = TestBlockchain.GetSystem();
            var currentHeight = NativeContract.Ledger.CurrentIndex(neoSystem.StoreView);

            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() =>
                new RemoteNode(neoSystem,
                    new LocalNode(neoSystem),
                    connectionTestProbe,
                    new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8080),
                    new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080),
                    new ChannelsConfig()));

            var remoteNode = remoteNodeActor.UnderlyingActor;

            var versionMessage = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "local-test",
                Nonce = 1,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 5,
                Version = 6,
                Capabilities =
                [
                    new FullNodeCapability(currentHeight)
                ]
            });

            var peer = CreateTestProbe();
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)versionMessage.ToArray()));
            connectionTestProbe.ExpectMsg<Tcp.Write>(cancellationToken: CancellationToken.None);
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)Message.Create(MessageCommand.Verack).ToArray()));

            var tx = CreateTransaction();
            peer.Send(remoteNodeActor, new Tcp.Received((ByteString)Message.Create(MessageCommand.Transaction, tx).ToArray()));

            // Transactions must not affect the peer's LastBlockIndex.
            Assert.AreEqual(currentHeight, remoteNode.LastBlockIndex);

            Sys.Stop(remoteNodeActor);
        }

        [TestMethod]
        public void Register_AtOrBelowLocalHeight_RequestsMempool()
        {
            var remote = CreateTestProbe("remote-mempool");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));

            var msg = remote.FishForMessage<Message>(
                m => m.Command == MessageCommand.Mempool,
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);
            Assert.IsNotNull(msg);
            Assert.AreEqual(MessageCommand.Mempool, msg.Command);
        }

        [TestMethod]
        public void Register_HigherPeer_RequestsWork()
        {
            var remote = CreateTestProbe("remote-headers");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height + 50)));

            var msg = remote.FishForMessage<Message>(
                m => m.Command is MessageCommand.GetHeaders or MessageCommand.GetBlockByIndex or MessageCommand.Mempool,
                TimeSpan.FromSeconds(3),
                cancellationToken: CancellationToken.None);
            Assert.IsNotNull(msg);
        }

        [TestMethod]
        public void Update_WithoutSession_IsIgnored()
        {
            var remote = CreateTestProbe("remote-orphan-update");
            remote.Send(s_system.TaskManager, new TaskManager.Update(123));
            remote.ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void Update_AfterRegister_DoesNotThrow()
        {
            var remote = CreateTestProbe("remote-update");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            remote.Send(s_system.TaskManager, new TaskManager.Update(height + 10));
            remote.ReceiveOne(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void NewTasks_TxInventory_DoesNotFaultActor()
        {
            var remote = CreateTestProbe("remote-newtasks");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var hash = UInt256.Parse("0x1111111111111111111111111111111111111111111111111111111111111111");
            remote.Send(s_system.TaskManager, new TaskManager.NewTasks(
                InvPayload.Create(InventoryType.TX, hash)));

            // TX NewTasks may produce GetData (or nothing); either is fine — actor must stay healthy.
            _ = remote.ReceiveOne(TimeSpan.FromMilliseconds(400), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void RestartTasks_DoesNotFaultActor()
        {
            var remote = CreateTestProbe("remote-restart");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var hash = UInt256.Parse("0x2222222222222222222222222222222222222222222222222222222222222222");
            remote.Send(s_system.TaskManager, new TaskManager.RestartTasks(
                InvPayload.Create(InventoryType.Block, hash)));

            remote.ExpectNoMsg(TimeSpan.FromMilliseconds(200), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void PersistCompleted_IsAccepted()
        {
            var remote = CreateTestProbe("remote-persist");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height + 5)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var block = NativeContract.Ledger.GetBlock(s_system.StoreView, 0);
            Assert.IsNotNull(block);
            s_system.TaskManager.Tell(new Blockchain.PersistCompleted(block), remote);
            remote.ReceiveOne(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void InventoryCompleted_IsAccepted()
        {
            var remote = CreateTestProbe("remote-inv-done");
            var height = NativeContract.Ledger.CurrentIndex(s_system.StoreView);
            remote.Send(s_system.TaskManager, new TaskManager.Register(MakeVersion(height)));
            remote.ReceiveOne(TimeSpan.FromSeconds(2), cancellationToken: CancellationToken.None);

            var block = NativeContract.Ledger.GetBlock(s_system.StoreView, 0);
            remote.Send(s_system.TaskManager, block);
            remote.ReceiveOne(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }
    }
}
