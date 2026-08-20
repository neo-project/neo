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
        public UT_TaskManager()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        private static Block CreateBlock(uint index)
        {
            return new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = MerkleTree.ComputeRoot([]),
                    Timestamp = 1,
                    Index = index,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness()
                },
                Transactions = []
            };
        }

        private static Dictionary<IActorRef, TaskSession> GetSessions(Akka.TestKit.TestActorRef<TaskManager> taskManager)
        {
            var sessionsField = typeof(TaskManager).GetField("sessions", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (Dictionary<IActorRef, TaskSession>)sessionsField.GetValue(taskManager.UnderlyingActor)!;
        }

        [TestMethod]
        public void UnsolicitedFarFutureBlock_IsNotRetainedByTaskManager()
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

            Assert.IsFalse(session.ReceivedBlockHashes.ContainsKey(parsedBlock.Index), "An unsolicited far-future block must not be retained.");

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
    }
}
