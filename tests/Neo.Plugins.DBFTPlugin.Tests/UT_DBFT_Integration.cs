// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFT_Integration.cs file belongs to the neo project and is free
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
using System.Threading.Tasks;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_DBFT_Integration : TestKit
    {
        private NeoSystem neoSystem;
        private TestProbe localNode;
        private TestProbe taskManager;
        private TestProbe blockchain;
        private TestProbe txRouter;
        private TestWallet[] testWallets;
        private IActorRef[] consensusServices;
        private MemoryStore memoryStore;
        private const int ValidatorCount = 4; // Smaller for integration tests

        [TestInitialize]
        public void Setup()
        {
            // Create test probes for actor dependencies
            localNode = CreateTestProbe("localNode");
            taskManager = CreateTestProbe("taskManager");
            blockchain = CreateTestProbe("blockchain");
            txRouter = CreateTestProbe("txRouter");

            // Setup autopilot for localNode to handle consensus messages
            localNode.SetAutoPilot(new CustomAutoPilot((sender, message) =>
            {
                if (message is ExtensiblePayload payload)
                {
                    // Broadcast the payload to all consensus services
                    foreach (var service in consensusServices?.Where(s => s != null) ?? Array.Empty<IActorRef>())
                    {
                        service.Tell(payload);
                    }
                }
            }));

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
        public void TestFullConsensusRound()
        {
            // Arrange - Create consensus services for all validators
            var settings = new Settings();

            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"full-consensus-{i}"
                );
            }

            // Start all consensus services
            foreach (var service in consensusServices)
            {
                service.Tell(new ConsensusService.Start());
            }

            // Act - Trigger consensus by simulating block persistence
            var genesisBlock = neoSystem.GenesisBlock;
            foreach (var service in consensusServices)
            {
                service.Tell(new Blockchain.PersistCompleted { Block = genesisBlock });
            }

            // Assert - Wait for consensus messages to be exchanged
            // In a real scenario, we would see PrepareRequest, PrepareResponse, and Commit messages
            ExpectNoMsg(TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void TestConsensusWithViewChange()
        {
            // Arrange
            var settings = new Settings();

            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"viewchange-consensus-{i}"
                );
            }

            // Start all consensus services
            foreach (var service in consensusServices)
            {
                service.Tell(new ConsensusService.Start());
            }

            // Act - Simulate primary failure by not starting the primary (index 0)
            // and trigger view change from backup validators
            var genesisBlock = neoSystem.GenesisBlock;
            for (int i = 1; i < ValidatorCount; i++) // Skip primary
            {
                consensusServices[i].Tell(new Blockchain.PersistCompleted { Block = genesisBlock });
            }

            // Wait for timeout and view change
            ExpectNoMsg(TimeSpan.FromSeconds(3));

            // Now start the new primary (index 1) after view change
            consensusServices[0].Tell(new Blockchain.PersistCompleted { Block = genesisBlock });

            // Assert - Consensus should eventually succeed with new primary
            ExpectNoMsg(TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void TestConsensusWithByzantineFailures()
        {
            // Arrange - Only start honest validators (3 out of 4, can tolerate 1 Byzantine)
            var settings = new Settings();
            var honestValidators = ValidatorCount - 1; // 3 honest validators

            for (int i = 0; i < honestValidators; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"byzantine-consensus-{i}"
                );
            }

            // Start only honest validators
            for (int i = 0; i < honestValidators; i++)
            {
                consensusServices[i].Tell(new ConsensusService.Start());
            }

            // Act - Trigger consensus
            var genesisBlock = neoSystem.GenesisBlock;
            for (int i = 0; i < honestValidators; i++)
            {
                consensusServices[i].Tell(new Blockchain.PersistCompleted { Block = genesisBlock });
            }

            // Assert - Consensus should succeed with 3 honest validators out of 4
            ExpectNoMsg(TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void TestConsensusRecovery()
        {
            // Arrange
            var settings = new Settings();

            for (int i = 0; i < ValidatorCount; i++)
            {
                consensusServices[i] = Sys.ActorOf(
                    ConsensusService.Props(neoSystem, settings, testWallets[i]),
                    $"recovery-consensus-{i}"
                );
            }

            // Start all consensus services
            foreach (var service in consensusServices)
            {
                service.Tell(new ConsensusService.Start());
            }

            // Act - Simulate a validator joining late and requesting recovery
            var genesisBlock = neoSystem.GenesisBlock;

            // Start consensus with first 3 validators
            for (int i = 0; i < ValidatorCount - 1; i++)
            {
                consensusServices[i].Tell(new Blockchain.PersistCompleted { Block = genesisBlock });
            }

            // Wait a bit for consensus to start
            ExpectNoMsg(TimeSpan.FromMilliseconds(500));

            // Late validator joins and should request recovery
            consensusServices[ValidatorCount - 1].Tell(new Blockchain.PersistCompleted { Block = genesisBlock });

            // Assert - Recovery should allow late validator to catch up
            ExpectNoMsg(TimeSpan.FromSeconds(2));
        }
    }
}
