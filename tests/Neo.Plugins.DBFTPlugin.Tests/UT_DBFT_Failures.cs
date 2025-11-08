// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFT_Failures.cs file belongs to the neo project and is free
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
using System.Threading;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_DBFT_Failures : TestKit
    {
        private const int ValidatorCount = 7;
        private NeoSystem neoSystem;
        private TestProbe localNode;
        private TestProbe taskManager;
        private TestProbe blockchain;
        private TestProbe txRouter;
        private MockWallet[] testWallets;
        private IActorRef[] consensusServices;
        private MemoryStore memoryStore;
        private DbftSettings settings;

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

            // Setup test wallets for validators
            testWallets = new MockWallet[ValidatorCount];
            consensusServices = new IActorRef[ValidatorCount];
            settings = MockBlockchain.CreateDefaultSettings();

            for (int i = 0; i < ValidatorCount; i++)
            {
                var testWallet = new MockWallet(MockProtocolSettings.Default);
                var validatorKey = MockProtocolSettings.Default.StandbyValidators[i];
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
        public void TestPrimaryFailureDuringConsensus()
        {
            // Arrange - Create all consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"primary-failure-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Primary index for reference (not used in this failure scenario)
            // var primaryIndex = 0;

            // Act - Primary fails to send PrepareRequest, backup validators should trigger view change
            // Simulate timeout by not sending PrepareRequest from primary

            // Backup validators should eventually send ChangeView messages
            for (int i = 1; i < ValidatorCount; i++) // Skip primary
            {
                var changeView = new ChangeView
                {
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Reason = ChangeViewReason.Timeout
                };
                var changeViewPayload = CreateConsensusPayload(changeView, i, 1); // View 1

                // Send ChangeView to all validators
                for (int j = 0; j < ValidatorCount; j++)
                {
                    consensusServices[j].Tell(changeViewPayload);
                }
            }

            // Assert - System should handle primary failure gracefully
            ExpectNoMsg(TimeSpan.FromMilliseconds(200), cancellationToken: CancellationToken.None);

            // Verify all actors are still alive
            for (int i = 0; i < ValidatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100), cancellationToken: CancellationToken.None); // No Terminated messages
        }

        [TestMethod]
        public void TestByzantineValidatorSendsConflictingMessages()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"byzantine-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            var byzantineValidatorIndex = 1;
            var primaryIndex = 0;

            // Act - Byzantine validator sends conflicting PrepareResponse messages
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

            // Byzantine validator sends conflicting PrepareResponse messages
            var prepareResponse1 = new PrepareResponse
            {
                PreparationHash = UInt256.Parse("0x1111111111111111111111111111111111111111111111111111111111111111")
            };
            var prepareResponse2 = new PrepareResponse
            {
                PreparationHash = UInt256.Parse("0x2222222222222222222222222222222222222222222222222222222222222222")
            };

            var conflictingPayload1 = CreateConsensusPayload(prepareResponse1, byzantineValidatorIndex);
            var conflictingPayload2 = CreateConsensusPayload(prepareResponse2, byzantineValidatorIndex);

            // Send conflicting messages to different validators
            for (int i = 0; i < ValidatorCount / 2; i++)
            {
                consensusServices[i].Tell(conflictingPayload1);
            }
            for (int i = ValidatorCount / 2; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(conflictingPayload2);
            }

            // Assert - System should handle Byzantine behavior
            ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);

            // Honest validators should continue operating
            for (int i = 0; i < ValidatorCount; i++)
            {
                if (i != byzantineValidatorIndex)
                {
                    Watch(consensusServices[i]);
                }
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100), cancellationToken: CancellationToken.None); // No Terminated messages from honest validators
        }

        [TestMethod]
        public void TestInvalidMessageHandling()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"invalid-msg-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Send various invalid messages

            // 1. Message with invalid validator index
            var invalidValidatorMessage = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };
            var invalidPayload = CreateConsensusPayload(invalidValidatorMessage, 255); // Invalid index

            // 2. Message with wrong block index
            var wrongBlockMessage = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>(),
                BlockIndex = 999 // Wrong block index
            };
            var wrongBlockPayload = CreateConsensusPayload(wrongBlockMessage, 0);

            // 3. Malformed payload
            var malformedPayload = new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = 1,
                Sender = Contract.GetBFTAddress(MockProtocolSettings.Default.StandbyValidators),
                Data = new byte[] { 0xFF, 0xFF, 0xFF }, // Invalid data
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            // Send invalid messages to all validators
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(invalidPayload);
                consensusServices[i].Tell(wrongBlockPayload);
                consensusServices[i].Tell(malformedPayload);
            }

            // Assert - Validators should reject invalid messages and continue operating
            ExpectNoMsg(TimeSpan.FromMilliseconds(200), cancellationToken: CancellationToken.None);

            // Verify all validators are still responsive
            for (int i = 0; i < ValidatorCount; i++)
            {
                Watch(consensusServices[i]);
                consensusServices[i].Tell("test_message");
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100), cancellationToken: CancellationToken.None); // No crashes
        }

        [TestMethod]
        public void TestNetworkPartitionScenario()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"partition-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate network partition where some validators can't communicate
            var partition1 = new[] { 0, 1, 2 }; // 3 validators
            var partition2 = new[] { 3, 4, 5, 6 }; // 4 validators

            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, 0);

            // Send PrepareRequest only to partition1 (simulating network partition)
            foreach (var validatorIndex in partition1)
            {
                consensusServices[validatorIndex].Tell(prepareRequestPayload);
            }

            // Partition2 doesn't receive the PrepareRequest (network partition)
            // They should eventually timeout and request view change

            // Assert - System should handle network partition
            ExpectNoMsg(TimeSpan.FromMilliseconds(300), cancellationToken: CancellationToken.None);

            // Both partitions should remain stable
            for (int i = 0; i < ValidatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100), cancellationToken: CancellationToken.None); // No crashes
        }
    }
}
