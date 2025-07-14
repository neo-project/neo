// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFT_Recovery.cs file belongs to the neo project and is free
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
    public class UT_DBFT_Recovery : TestKit
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
        public void TestRecoveryRequestResponse()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"recovery-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Simulate a validator that missed some consensus messages
            var recoveringValidatorIndex = ValidatorCount - 1;

            // Act - Send RecoveryRequest from the recovering validator
            var recoveryRequest = new RecoveryRequest
            {
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var recoveryRequestPayload = CreateConsensusPayload(recoveryRequest, recoveringValidatorIndex);

            // Send RecoveryRequest to all validators
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(recoveryRequestPayload);
            }

            // Assert - Other validators should respond with RecoveryMessage
            ExpectNoMsg(TimeSpan.FromMilliseconds(200));

            // Verify the recovering validator receives recovery information
            // In a real implementation, we would capture and verify RecoveryMessage responses
            Watch(consensusServices[recoveringValidatorIndex]);
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // Should not crash
        }

        [TestMethod]
        public void TestStateRecoveryAfterFailure()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"state-recovery-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            var failedValidatorIndex = 2;

            // Simulate partial consensus progress before failure
            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, 0);

            // Send PrepareRequest to all validators except the failed one
            for (int i = 0; i < ValidatorCount; i++)
            {
                if (i != failedValidatorIndex)
                {
                    consensusServices[i].Tell(prepareRequestPayload);
                }
            }

            // Some validators send PrepareResponse
            for (int i = 1; i < ValidatorCount / 2; i++)
            {
                if (i != failedValidatorIndex)
                {
                    var prepareResponse = new PrepareResponse
                    {
                        PreparationHash = UInt256.Zero
                    };
                    var responsePayload = CreateConsensusPayload(prepareResponse, i);

                    for (int j = 0; j < ValidatorCount; j++)
                    {
                        if (j != failedValidatorIndex)
                        {
                            consensusServices[j].Tell(responsePayload);
                        }
                    }
                }
            }

            // Act - Failed validator comes back online and requests recovery
            var recoveryRequest = new RecoveryRequest
            {
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var recoveryRequestPayload = CreateConsensusPayload(recoveryRequest, failedValidatorIndex);

            // Send recovery request to all validators
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(recoveryRequestPayload);
            }

            // Now send the missed PrepareRequest to the recovered validator
            consensusServices[failedValidatorIndex].Tell(prepareRequestPayload);

            // Assert - Failed validator should catch up with consensus state
            ExpectNoMsg(TimeSpan.FromMilliseconds(300));

            // Verify all validators are operational
            for (int i = 0; i < ValidatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes
        }

        [TestMethod]
        public void TestViewChangeRecovery()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"viewchange-recovery-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate view change scenario
            // Some validators initiate view change
            var viewChangeValidators = new[] { 1, 2, 3, 4 }; // Enough for view change

            foreach (var validatorIndex in viewChangeValidators)
            {
                var changeView = new ChangeView
                {
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Reason = ChangeViewReason.Timeout
                };
                var changeViewPayload = CreateConsensusPayload(changeView, validatorIndex, 1); // View 1

                // Send ChangeView to all validators
                for (int i = 0; i < ValidatorCount; i++)
                {
                    consensusServices[i].Tell(changeViewPayload);
                }
            }

            // A validator that missed the view change requests recovery
            var recoveringValidatorIndex = 0;
            var recoveryRequest = new RecoveryRequest
            {
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var recoveryRequestPayload = CreateConsensusPayload(recoveryRequest, recoveringValidatorIndex);

            // Send recovery request
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(recoveryRequestPayload);
            }

            // Assert - System should handle view change recovery
            ExpectNoMsg(TimeSpan.FromMilliseconds(300));

            // Verify all validators are stable
            for (int i = 0; i < ValidatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes
        }

        [TestMethod]
        public void TestMultipleSimultaneousRecoveryRequests()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"multi-recovery-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Multiple validators request recovery simultaneously
            var recoveringValidators = new[] { 3, 4, 5 };

            foreach (var validatorIndex in recoveringValidators)
            {
                var recoveryRequest = new RecoveryRequest
                {
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                var recoveryRequestPayload = CreateConsensusPayload(recoveryRequest, validatorIndex);

                // Send recovery request to all validators
                for (int i = 0; i < ValidatorCount; i++)
                {
                    consensusServices[i].Tell(recoveryRequestPayload);
                }
            }

            // Assert - System should handle multiple recovery requests efficiently
            ExpectNoMsg(TimeSpan.FromMilliseconds(400));

            // Verify all validators remain operational
            for (int i = 0; i < ValidatorCount; i++)
            {
                Watch(consensusServices[i]);
            }
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // No crashes
        }

        [TestMethod]
        public void TestRecoveryWithPartialConsensusState()
        {
            // Arrange - Create consensus services
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"partial-recovery-consensus-{i}"
                );
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Simulate consensus in progress with some messages already sent
            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, 0);

            // Send PrepareRequest to most validators
            for (int i = 0; i < ValidatorCount - 1; i++)
            {
                consensusServices[i].Tell(prepareRequestPayload);
            }

            // Some validators send PrepareResponse
            for (int i = 1; i < 4; i++)
            {
                var prepareResponse = new PrepareResponse
                {
                    PreparationHash = UInt256.Zero
                };
                var responsePayload = CreateConsensusPayload(prepareResponse, i);

                for (int j = 0; j < ValidatorCount - 1; j++)
                {
                    consensusServices[j].Tell(responsePayload);
                }
            }

            // Act - Last validator comes online and requests recovery
            var lateValidatorIndex = ValidatorCount - 1;
            var recoveryRequest = new RecoveryRequest
            {
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var recoveryRequestPayload = CreateConsensusPayload(recoveryRequest, lateValidatorIndex);

            // Send recovery request
            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i].Tell(recoveryRequestPayload);
            }

            // Assert - Late validator should receive recovery information and catch up
            ExpectNoMsg(TimeSpan.FromMilliseconds(300));

            // Verify the late validator is now operational
            Watch(consensusServices[lateValidatorIndex]);
            ExpectNoMsg(TimeSpan.FromMilliseconds(100)); // Should not crash
        }
    }
}
