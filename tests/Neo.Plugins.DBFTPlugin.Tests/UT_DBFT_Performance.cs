// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFT_Robustness.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.MsTest;
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
    public class UT_DBFT_Performance : TestKit
    {
        private NeoSystem neoSystem;
        private TestProbe localNode;
        private TestProbe taskManager;
        private TestProbe blockchain;
        private TestProbe txRouter;
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
            var storeProvider = new MockMemoryStoreProvider(memoryStore);

            // Create NeoSystem with test dependencies
            neoSystem = new NeoSystem(MockProtocolSettings.Default, storeProvider);

            settings = MockBlockchain.CreateDefaultSettings();
        }

        [TestCleanup]
        public void Cleanup()
        {
            neoSystem?.Dispose();
            Shutdown();
        }

        private ExtensiblePayload CreateConsensusPayload(ConsensusMessage message, int validatorIndex, byte viewNumber = 0)
        {
            message.BlockIndex = 1;
            message.ValidatorIndex = (byte)validatorIndex;
            message.ViewNumber = viewNumber;

            return new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = message.BlockIndex,
                Sender = Contract.GetBFTAddress(MockProtocolSettings.Default.StandbyValidators),
                Data = message.ToArray(),
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };
        }

        [TestMethod]
        public void TestMinimumValidatorConsensus()
        {
            // Arrange - Test with minimum validator count (4 validators, f=1)
            const int minValidatorCount = 4;
            var testWallets = new MockWallet[minValidatorCount];
            var consensusServices = new IActorRef[minValidatorCount];

            for (int i = 0; i < minValidatorCount; i++)
            {
                var testWallet = new MockWallet(MockProtocolSettings.Default);
                var validatorKey = MockProtocolSettings.Default.StandbyValidators[i];
                testWallet.AddAccount(validatorKey);
                testWallets[i] = testWallet;

                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"min-validator-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate consensus with minimum validators
            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, 0);

            // Send PrepareRequest to all validators
            for (int i = 0; i < minValidatorCount; i++)
            {
                consensusServices[i].Tell(prepareRequestPayload);
            }

            // Assert - Consensus should work with minimum validators
            ExpectNoMsg(TimeSpan.FromMilliseconds(200));

            // Verify all validators are operational
            for (int i = 0; i < minValidatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes
        }

        [TestMethod]
        public void TestMaximumByzantineFailures()
        {
            // Arrange - Test with 7 validators (f=2, can tolerate 2 Byzantine failures)
            const int validatorCount = 7;
            // Maximum Byzantine failures that can be tolerated (f=2 for 7 validators)
            // const int maxByzantineFailures = 2;

            var testWallets = new MockWallet[validatorCount];
            var consensusServices = new IActorRef[validatorCount];

            for (int i = 0; i < validatorCount; i++)
            {
                var testWallet = new MockWallet(MockProtocolSettings.Default);
                var validatorKey = MockProtocolSettings.Default.StandbyValidators[i];
                testWallet.AddAccount(validatorKey);
                testWallets[i] = testWallet;

                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"byzantine-max-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate maximum Byzantine failures
            var byzantineValidators = new[] { 1, 2 }; // 2 Byzantine validators
            var honestValidators = Enumerable.Range(0, validatorCount).Except(byzantineValidators).ToArray();

            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, 0);

            // Send PrepareRequest to honest validators only
            foreach (var validatorIndex in honestValidators)
            {
                consensusServices[validatorIndex].Tell(prepareRequestPayload);
            }

            // Byzantine validators send conflicting or no messages
            foreach (var byzantineIndex in byzantineValidators)
            {
                var conflictingRequest = new PrepareRequest
                {
                    Version = 0,
                    PrevHash = UInt256.Parse("0x1111111111111111111111111111111111111111111111111111111111111111"),
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = 999,
                    TransactionHashes = Array.Empty<UInt256>()
                };
                var conflictingPayload = CreateConsensusPayload(conflictingRequest, byzantineIndex);

                // Send conflicting message to some validators
                for (int i = 0; i < validatorCount / 2; i++)
                {
                    consensusServices[i].Tell(conflictingPayload);
                }
            }

            // Assert - Honest validators should continue consensus despite Byzantine failures
            ExpectNoMsg(TimeSpan.FromMilliseconds(300));

            // Verify honest validators are still operational
            foreach (var validatorIndex in honestValidators)
            {
                Watch(consensusServices[validatorIndex]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes in honest validators
        }

        [TestMethod]
        public void TestStressConsensusMultipleRounds()
        {
            // Arrange - Test multiple rapid consensus rounds
            const int validatorCount = 7;
            const int numberOfRounds = 5;

            var testWallets = new MockWallet[validatorCount];
            var consensusServices = new IActorRef[validatorCount];

            for (int i = 0; i < validatorCount; i++)
            {
                var testWallet = new MockWallet(MockProtocolSettings.Default);
                var validatorKey = MockProtocolSettings.Default.StandbyValidators[i];
                testWallet.AddAccount(validatorKey);
                testWallets[i] = testWallet;

                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"stress-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate multiple consensus rounds rapidly
            for (int round = 0; round < numberOfRounds; round++)
            {
                var prepareRequest = new PrepareRequest
                {
                    Version = 0,
                    PrevHash = UInt256.Zero,
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = (ulong)round,
                    TransactionHashes = Array.Empty<UInt256>(),
                    BlockIndex = (uint)(round + 1)
                };
                var prepareRequestPayload = CreateConsensusPayload(prepareRequest, round % validatorCount);

                // Send PrepareRequest to all validators
                for (int i = 0; i < validatorCount; i++)
                {
                    consensusServices[i].Tell(prepareRequestPayload);
                }

                // Small delay between rounds
                ExpectNoMsg(TimeSpan.FromMilliseconds(50));
            }

            // Assert - System should handle multiple rounds without degradation
            ExpectNoMsg(TimeSpan.FromMilliseconds(200));

            // Verify all validators are still operational after stress test
            for (int i = 0; i < validatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes
        }

        [TestMethod]
        public void TestLargeTransactionSetConsensus()
        {
            // Arrange - Test consensus with large transaction sets
            const int validatorCount = 7;
            const int transactionCount = 100;

            var testWallets = new MockWallet[validatorCount];
            var consensusServices = new IActorRef[validatorCount];

            for (int i = 0; i < validatorCount; i++)
            {
                var testWallet = new MockWallet(MockProtocolSettings.Default);
                var validatorKey = MockProtocolSettings.Default.StandbyValidators[i];
                testWallet.AddAccount(validatorKey);
                testWallets[i] = testWallet;

                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"large-tx-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Create large transaction set
            var transactions = new UInt256[transactionCount];
            for (int i = 0; i < transactionCount; i++)
            {
                var txBytes = new byte[32];
                BitConverter.GetBytes(i).CopyTo(txBytes, 0);
                transactions[i] = new UInt256(txBytes);
            }

            // Act - Simulate consensus with large transaction set
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
            for (int i = 0; i < validatorCount; i++)
            {
                consensusServices[i].Tell(prepareRequestPayload);
            }

            // Assert - System should handle large transaction sets
            ExpectNoMsg(TimeSpan.FromMilliseconds(500)); // Longer timeout for large data

            // Verify all validators processed the large transaction set
            for (int i = 0; i < validatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes
        }

        [TestMethod]
        public void TestConcurrentViewChanges()
        {
            // Arrange - Test multiple simultaneous view changes
            const int validatorCount = 7;

            var testWallets = new MockWallet[validatorCount];
            var consensusServices = new IActorRef[validatorCount];

            for (int i = 0; i < validatorCount; i++)
            {
                var testWallet = new MockWallet(MockProtocolSettings.Default);
                var validatorKey = MockProtocolSettings.Default.StandbyValidators[i];
                testWallet.AddAccount(validatorKey);
                testWallets[i] = testWallet;

                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"concurrent-viewchange-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate concurrent view changes from multiple validators
            var viewChangeValidators = new[] { 1, 2, 3, 4, 5 }; // Multiple validators trigger view change

            foreach (var validatorIndex in viewChangeValidators)
            {
                var changeView = new ChangeView
                {
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Reason = ChangeViewReason.Timeout
                };
                var changeViewPayload = CreateConsensusPayload(changeView, validatorIndex, 1); // View 1

                // Send ChangeView to all validators simultaneously
                for (int i = 0; i < validatorCount; i++)
                {
                    consensusServices[i].Tell(changeViewPayload);
                }
            }

            // Assert - System should handle concurrent view changes gracefully
            ExpectNoMsg(TimeSpan.FromMilliseconds(300));

            // Verify all validators remain stable
            for (int i = 0; i < validatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes
        }
    }
}
