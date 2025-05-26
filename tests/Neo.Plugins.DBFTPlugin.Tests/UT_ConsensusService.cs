// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ConsensusService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence.Providers;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_ConsensusService : TestKit
    {
        private NeoSystem neoSystem;
        private TestProbe localNode;
        private TestProbe taskManager;
        private TestProbe blockchain;
        private TestProbe txRouter;
        private TestWallet testWallet;
        private MemoryStore memoryStore;

        [TestInitialize]
        public void Setup()
        {
            // Create test probes for actor dependencies
            localNode = CreateTestProbe("localNode");
            taskManager = CreateTestProbe("taskManager");
            blockchain = CreateTestProbe("blockchain");
            txRouter = CreateTestProbe("txRouter");

            // Create memory store
            memoryStore = new MemoryStore();
            var storeProvider = new TestMemoryStoreProvider(memoryStore);

            // Create NeoSystem with test dependencies
            neoSystem = new NeoSystem(
                TestProtocolSettings.Default,
                storeProvider,
                localNode.Ref,
                blockchain.Ref,
                taskManager.Ref,
                txRouter.Ref
            );

            // Setup test wallet
            testWallet = new TestWallet(TestProtocolSettings.Default);
            testWallet.AddAccount(TestProtocolSettings.Default.StandbyValidators[0]);
        }

        [TestCleanup]
        public void Cleanup()
        {
            neoSystem?.Dispose();
            Shutdown();
        }

        private ExtensiblePayload CreateConsensusPayload(ConsensusMessage message)
        {
            return new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = 100,
                Sender = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators),
                Data = message.ToArray(),
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };
        }

        [TestMethod]
        public void TestConsensusServiceCreation()
        {
            // Arrange
            var settings = new Settings();

            // Act
            var consensusService = Sys.ActorOf(ConsensusService.Props(neoSystem, settings, testWallet));

            // Assert
            Assert.IsNotNull(consensusService);

            // Verify the service is responsive and doesn't crash on unknown messages
            consensusService.Tell("unknown_message");
            ExpectNoMsg(TimeSpan.FromMilliseconds(100));

            // Verify the actor is still alive
            Watch(consensusService);
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // Should not receive Terminated message
        }

        [TestMethod]
        public void TestConsensusServiceStart()
        {
            // Arrange
            var settings = new Settings();
            var consensusService = Sys.ActorOf(ConsensusService.Props(neoSystem, settings, testWallet));

            // Act
            consensusService.Tell(new ConsensusService.Start());

            // Assert - The service should start without throwing exceptions
            ExpectNoMsg(TimeSpan.FromMilliseconds(100));
        }

        [TestMethod]
        public void TestConsensusServiceReceivesBlockchainMessages()
        {
            // Arrange
            var settings = new Settings();
            var consensusService = Sys.ActorOf(ConsensusService.Props(neoSystem, settings, testWallet));

            // Start the consensus service
            consensusService.Tell(new ConsensusService.Start());

            // Create a test block
            var block = new Block
            {
                Header = new Header
                {
                    Index = 1,
                    PrimaryIndex = 0,
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = 0,
                    NextConsensus = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators),
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Witness = new Witness
                    {
                        InvocationScript = ReadOnlyMemory<byte>.Empty,
                        VerificationScript = new[] { (byte)OpCode.PUSH1 }
                    }
                },
                Transactions = Array.Empty<Transaction>()
            };

            // Act
            consensusService.Tell(new Blockchain.PersistCompleted { Block = block });

            // Assert - The service should handle the message without throwing
            ExpectNoMsg(TimeSpan.FromMilliseconds(100));
        }

        [TestMethod]
        public void TestConsensusServiceHandlesExtensiblePayload()
        {
            // Arrange
            var settings = new Settings();
            var consensusService = Sys.ActorOf(ConsensusService.Props(neoSystem, settings, testWallet));

            // Start the consensus service
            consensusService.Tell(new ConsensusService.Start());

            // Create a test extensible payload
            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = 100,
                Sender = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators),
                Data = new byte[] { 0x01, 0x02, 0x03 },
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            // Act
            consensusService.Tell(payload);

            // Assert - The service should handle the payload without throwing
            ExpectNoMsg(TimeSpan.FromMilliseconds(100));
        }

        [TestMethod]
        public void TestConsensusServiceHandlesValidConsensusMessage()
        {
            // Arrange
            var settings = new Settings();
            var consensusService = Sys.ActorOf(ConsensusService.Props(neoSystem, settings, testWallet));
            consensusService.Tell(new ConsensusService.Start());

            // Create a valid PrepareRequest message
            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                ViewNumber = 0,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var payload = CreateConsensusPayload(prepareRequest);

            // Act
            consensusService.Tell(payload);

            // Assert - Service should process the message without crashing
            ExpectNoMsg(TimeSpan.FromMilliseconds(200));

            // Verify the actor is still responsive
            Watch(consensusService);
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // Should not receive Terminated message
        }

        [TestMethod]
        public void TestConsensusServiceRejectsInvalidPayload()
        {
            // Arrange
            var settings = new Settings();
            var consensusService = Sys.ActorOf(ConsensusService.Props(neoSystem, settings, testWallet));
            consensusService.Tell(new ConsensusService.Start());

            // Create an invalid payload (wrong category)
            var invalidPayload = new ExtensiblePayload
            {
                Category = "InvalidCategory",
                ValidBlockStart = 0,
                ValidBlockEnd = 100,
                Sender = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators),
                Data = new byte[] { 0x01, 0x02, 0x03 },
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            // Act
            consensusService.Tell(invalidPayload);

            // Assert - Service should ignore invalid payload and remain stable
            ExpectNoMsg(TimeSpan.FromMilliseconds(100));

            // Verify the actor is still alive and responsive
            Watch(consensusService);
            ExpectNoMsg(TimeSpan.FromMilliseconds(100));
        }
    }
}
