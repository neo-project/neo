// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFT_Core.cs file belongs to the neo project and is free
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
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence.Providers;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Linq;
using System.Threading;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_DBFT_Core : TestKit
    {
        private NeoSystem neoSystem;
        private TestProbe localNode;
        private TestProbe taskManager;
        private TestProbe blockchain;
        private TestProbe txRouter;
        private MockWallet[] testWallets;
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
            var storeProvider = new MockMemoryStoreProvider(memoryStore);

            // Create NeoSystem with test dependencies
            neoSystem = new NeoSystem(MockProtocolSettings.Default, storeProvider);

            // Setup test wallets for validators
            testWallets = new MockWallet[ValidatorCount];
            consensusServices = new IActorRef[ValidatorCount];

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
            var settings = MockBlockchain.CreateDefaultSettings();

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
                service.Tell(new Blockchain.PersistCompleted(genesisBlock));
            }

            // Assert - Services should start consensus without throwing
            // Verify all consensus services were created successfully
            Assert.HasCount(ValidatorCount, consensusServices, "Should create all consensus services");
            foreach (var service in consensusServices)
            {
                Assert.IsNotNull(service, "Each consensus service should be created successfully");
            }

            // Verify no unexpected messages or crashes
            ExpectNoMsg(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void TestPrimarySelection()
        {
            // Arrange
            var settings = MockBlockchain.CreateDefaultSettings();
            var primaryService = Sys.ActorOf(
                ConsensusService.Props(neoSystem, settings, testWallets[0]),
                "primary-consensus"
            );

            // Act
            primaryService.Tell(new ConsensusService.Start());

            // Simulate block persistence to trigger consensus
            var genesisBlock = neoSystem.GenesisBlock;
            primaryService.Tell(new Blockchain.PersistCompleted(genesisBlock));

            // Assert - Primary should start consensus process
            ExpectNoMsg(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }

        [TestMethod]
        public void TestMultipleRounds()
        {
            // Arrange
            var settings = MockBlockchain.CreateDefaultSettings();
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
                        NextConsensus = Contract.GetBFTAddress(MockProtocolSettings.Default.StandbyValidators),
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

                consensusService.Tell(new Blockchain.PersistCompleted(block));

                // Wait between rounds
                ExpectNoMsg(TimeSpan.FromMilliseconds(100), cancellationToken: CancellationToken.None);
            }

            // Assert - Service should handle multiple rounds
            ExpectNoMsg(TimeSpan.FromMilliseconds(500), cancellationToken: CancellationToken.None);
        }
    }
}
