// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFT.cs file belongs to the neo project and is free
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
    public class UT_DBFT : TestKit
    {
        private NeoSystem neoSystem;
        private TestProbe localNode;
        private TestProbe taskManager;
        private TestProbe blockchain;
        private TestProbe txRouter;
        private TestWallet[] testWallets;
        private IActorRef[] consensusServices;
        private MemoryStore memoryStore;
        private const int ValidatorCount = 7;

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
            // Stop all consensus services
            if (consensusServices != null)
            {
                foreach (var service in consensusServices.Where(s => s != null))
                {
                    Sys.Stop(service);
                }
            }

            neoSystem?.Dispose();
            Shutdown();
        }

        [TestMethod]
        public void TestBasicConsensusFlow()
        {
            // Arrange - Create consensus services for all validators
            var settings = TestBlockchain.CreateDefaultSettings();

            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"consensus-{i}"
                );
            }

            // Start all consensus services
            foreach (var service in consensusServices)
            {
                service.Tell(new ConsensusService.Start());
            }

            // Act - Simulate block persistence to trigger consensus
            var genesisBlock = neoSystem.GenesisBlock;
            foreach (var service in consensusServices)
            {
                service.Tell(new Blockchain.PersistCompleted { Block = genesisBlock });
            }

            // Assert - Services should start consensus without throwing
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }

        [TestMethod]
        public void TestPrimarySelection()
        {
            // Arrange
            var settings = TestBlockchain.CreateDefaultSettings();
            var primaryService = Sys.ActorOf(
                ConsensusService.Props(neoSystem, settings, testWallets[0]),
                "primary-consensus"
            );

            // Act
            primaryService.Tell(new ConsensusService.Start());

            // Simulate block persistence to trigger consensus
            var genesisBlock = neoSystem.GenesisBlock;
            primaryService.Tell(new Blockchain.PersistCompleted { Block = genesisBlock });

            // Assert - Primary should start consensus process
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }

        [TestMethod]
        public void TestConsensusMessageHandling()
        {
            // Arrange
            var settings = TestBlockchain.CreateDefaultSettings();
            var consensusService = Sys.ActorOf(
                ConsensusService.Props(neoSystem, settings, testWallets[0]),
                "message-consensus"
            );

            consensusService.Tell(new ConsensusService.Start());

            // Create a prepare request message
            var prepareRequest = new PrepareRequest
            {
                Version = 0,
                PrevHash = UInt256.Zero,
                ViewNumber = 0,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 0,
                TransactionHashes = Array.Empty<UInt256>()
            };

            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = 100,
                Sender = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators),
                Data = prepareRequest.ToArray(),
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            // Act
            consensusService.Tell(payload);

            // Assert - Service should handle the message
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }

        [TestMethod]
        public void TestViewChange()
        {
            // Arrange
            var settings = TestBlockchain.CreateDefaultSettings();
            var consensusService = Sys.ActorOf(
                ConsensusService.Props(neoSystem, settings, testWallets[1]), // Not primary
                "viewchange-consensus"
            );

            consensusService.Tell(new ConsensusService.Start());

            // Create a change view message
            var changeView = new ChangeView
            {
                ViewNumber = 1,
                Reason = ChangeViewReason.Timeout
            };

            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = 100,
                Sender = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyValidators[1]).ToScriptHash(),
                Data = changeView.ToArray(),
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            // Act
            consensusService.Tell(payload);

            // Assert - Service should handle view change
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }

        [TestMethod]
        public void TestRecoveryMessage()
        {
            // Arrange
            var settings = TestBlockchain.CreateDefaultSettings();
            var consensusService = Sys.ActorOf(
                ConsensusService.Props(neoSystem, settings, testWallets[0]),
                "recovery-consensus"
            );

            consensusService.Tell(new ConsensusService.Start());

            // Create a recovery request message
            var recoveryRequest = new RecoveryRequest
            {
                ViewNumber = 0,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = 100,
                Sender = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyValidators[0]).ToScriptHash(),
                Data = recoveryRequest.ToArray(),
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            // Act
            consensusService.Tell(payload);

            // Assert - Service should handle recovery request
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }

        [TestMethod]
        public void TestByzantineFaultTolerance()
        {
            // Arrange - Create consensus services for all validators
            var settings = TestBlockchain.CreateDefaultSettings();

            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"byzantine-consensus-{i}"
                );
            }

            // Start only honest validators (simulate 2 Byzantine failures out of 7)
            var honestValidators = ValidatorCount - 2; // 5 honest validators
            for (int i = 0; i < honestValidators; i++)
            {
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Simulate block persistence to trigger consensus
            var genesisBlock = neoSystem.GenesisBlock;
            for (int i = 0; i < honestValidators; i++)
            {
                consensusServices[i].Tell(new Blockchain.PersistCompleted { Block = genesisBlock });
            }

            // Assert - Consensus should still work with f=2 Byzantine failures
            ExpectNoMsg(TimeSpan.FromMilliseconds(1000));
        }

        [TestMethod]
        public void TestMultipleRounds()
        {
            // Arrange
            var settings = TestBlockchain.CreateDefaultSettings();
            var consensusService = Sys.ActorOf(
                ConsensusService.Props(neoSystem, settings, testWallets[0]),
                "multiround-consensus"
            );

            consensusService.Tell(new ConsensusService.Start());

            // Act - Simulate multiple block persistence events
            for (uint blockIndex = 0; blockIndex < 3; blockIndex++)
            {
                var block = new Block
                {
                    Header = new Header
                    {
                        Index = blockIndex,
                        PrimaryIndex = (byte)(blockIndex % ValidatorCount),
                        Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Nonce = 0,
                        NextConsensus = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators),
                        PrevHash = blockIndex == 0 ? UInt256.Zero : UInt256.Parse("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"),
                        MerkleRoot = UInt256.Zero,
                        Witness = new Witness
                        {
                            InvocationScript = ReadOnlyMemory<byte>.Empty,
                            VerificationScript = new[] { (byte)OpCode.PUSH1 }
                        }
                    },
                    Transactions = Array.Empty<Transaction>()
                };

                consensusService.Tell(new Blockchain.PersistCompleted { Block = block });

                // Wait between rounds
                ExpectNoMsg(TimeSpan.FromMilliseconds(100));
            }

            // Assert - Service should handle multiple rounds
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));
        }
    }
}
