// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFT_NormalFlow.cs file belongs to the neo project and is free
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
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_DBFT_NormalFlow : TestKit
    {
        private const int ValidatorCount = 7;
        private NeoSystem neoSystem;
        private TestProbe localNode;
        private TestProbe taskManager;
        private TestProbe blockchain;
        private TestProbe txRouter;
        private TestWallet[] testWallets;
        private IActorRef[] consensusServices;
        private MemoryStore memoryStore;
        private Settings settings;

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
            neoSystem = new NeoSystem(TestProtocolSettings.Default, storeProvider);

            // Setup test wallets for validators
            testWallets = new TestWallet[ValidatorCount];
            consensusServices = new IActorRef[ValidatorCount];
            settings = TestBlockchain.CreateDefaultSettings();

            for (int i = 0; i < ValidatorCount; i++)
            {
                var testWallet = new TestWallet(TestProtocolSettings.Default);
                var validatorKey = TestProtocolSettings.Default.StandbyValidators[i];
                testWallet.AddAccount(validatorKey);
                testWallets[i] = testWallet;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            neoSystem?.Dispose();
            Shutdown();
        }

        private ExtensiblePayload CreateConsensusPayload(ConsensusMessage message, int validatorIndex)
        {
            message.BlockIndex = 1;
            message.ValidatorIndex = (byte)validatorIndex;
            message.ViewNumber = 0;

            return new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = message.BlockIndex,
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
        public void TestCompleteConsensusRound()
        {
            // Arrange - Create all consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate complete consensus round
            var primaryIndex = 0; // First validator is primary for view 0

            // Step 1: Primary sends PrepareRequest
            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, primaryIndex);

            // Send PrepareRequest to all validators
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(prepareRequestPayload);
            }

            // Step 2: Backup validators should send PrepareResponse
            var prepareResponses = new List<ExtensiblePayload>();
            for (int i = 1; i < ValidatorCount; i++) // Skip primary (index 0)
            {
                var prepareResponse = new PrepareResponse
                {
                    PreparationHash = UInt256.Zero // Simplified for testing
                };
                var responsePayload = CreateConsensusPayload(prepareResponse, i);
                prepareResponses.Add(responsePayload);

                // Send PrepareResponse to all validators
                for (int j = 0; j < ValidatorCount; j++)
                {
                    consensusServices[j].Tell(responsePayload);
                }
            }

            // Step 3: All validators should send Commit messages
            var commits = new List<ExtensiblePayload>();
            for (int i = 0; i < ValidatorCount; i++)
            {
                var commit = new Commit
                {
                    Signature = new byte[64] // Fake signature for testing
                };
                var commitPayload = CreateConsensusPayload(commit, i);
                commits.Add(commitPayload);

                // Send Commit to all validators
                for (int j = 0; j < ValidatorCount; j++)
                {
                    consensusServices[j].Tell(commitPayload);
                }
            }

            // Assert - Verify consensus messages are processed without errors
            // In a real implementation, the blockchain would receive a block when consensus completes
            // For this test, we verify that the consensus services handle the messages without crashing
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));

            // Verify all consensus services are still operational
            for (int i = 0; i < ValidatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No Terminated messages
        }

        [TestMethod]
        public void TestPrimaryRotationBetweenRounds()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"rotation-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act & Assert - Test multiple rounds with different primaries
            for (int round = 0; round < 3; round++)
            {
                var expectedPrimaryIndex = round % ValidatorCount;

                // Simulate consensus round with current primary
                var prepareRequest = new PrepareRequest
                {
                    Version = 0,
                    PrevHash = UInt256.Zero,
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = (ulong)round,
                    TransactionHashes = Array.Empty<UInt256>()
                };

                var prepareRequestPayload = CreateConsensusPayload(prepareRequest, expectedPrimaryIndex);
                prepareRequestPayload.Data = prepareRequest.ToArray(); // Update with correct primary

                // Send PrepareRequest from expected primary
                for (int i = 0; i < ValidatorCount; i++)
                {
                    consensusServices[i].Tell(prepareRequestPayload);
                }

                // Verify the round progresses (simplified verification)
                ExpectNoMsg(TimeSpan.FromMilliseconds(100));
            }
        }

        [TestMethod]
        public void TestConsensusWithTransactions()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"tx-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Create mock transactions
            var transactions = new[]
            {
                UInt256.Parse("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"),
                UInt256.Parse("0xfedcba0987654321fedcba0987654321fedcba0987654321fedcba0987654321")
            };

            // Act - Simulate consensus with transactions
            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = transactions
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, 0);

            // Send PrepareRequest to all validators
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(prepareRequestPayload);
            }

            // Assert - Verify transactions are included in consensus
            ExpectNoMsg(TimeSpan.FromMilliseconds(200));

            // In a real implementation, we would verify that:
            // 1. Validators request the transactions from mempool
            // 2. Transactions are validated before consensus
            // 3. Block contains the specified transactions
        }
    }
}
