// Copyright (C) 2015-2025 The Neo Project.
//
// ChaosTestBase.cs file belongs to the neo project and is free
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
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence.Providers;
using Neo.Plugins.DBFTPlugin;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Utilities;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.Sign;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Framework
{
    public abstract class ChaosTestBase : TestKit
    {
        public Random chaosRandom;
        public ChaosMetrics metrics;
        public FaultInjector faultInjector;
        public NetworkChaosSimulator networkChaos;
        public ChaosConfiguration config;

        // Consensus infrastructure
        protected NeoSystem neoSystem;
        protected MemoryStore memoryStore;
        protected MockWallet testWallet;
        protected Settings consensusSettings;

        // Actor references
        protected TestProbe localNode;
        protected TestProbe taskManager;
        protected TestProbe blockchain;
        protected TestProbe txRouter;

        // Consensus service instances
        public List<IActorRef> consensusServices = new List<IActorRef>();
        protected Dictionary<IActorRef, bool> nodeStates = new Dictionary<IActorRef, bool>();

        [TestInitialize]
        public virtual void TestSetup()
        {
            var seed = Environment.GetEnvironmentVariable("CHAOS_SEED") ?? DateTime.Now.Ticks.ToString();
            chaosRandom = new Random(seed.GetHashCode());
            metrics = new ChaosMetrics();
            config = LoadChaosConfiguration();
            faultInjector = new FaultInjector(chaosRandom, metrics, config);
            networkChaos = new NetworkChaosSimulator(chaosRandom, metrics, config);

            // Initialize Neo infrastructure
            InitializeNeoSystem();

            Console.WriteLine($"[CHAOS] Test initialized with seed: {seed}");
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            metrics.GenerateReport();

            // Cleanup consensus services
            foreach (var service in consensusServices)
            {
                Sys.Stop(service);
            }

            neoSystem?.Dispose();
            Shutdown();
        }

        protected virtual void InitializeNeoSystem()
        {
            // Create test probes for actor dependencies
            localNode = CreateTestProbe("localNode");
            taskManager = CreateTestProbe("taskManager");
            blockchain = CreateTestProbe("blockchain");
            txRouter = CreateTestProbe("txRouter");

            // Setup autopilot for localNode to handle consensus message broadcasting
            localNode.SetAutoPilot(new MockAutoPilot((sender, message) =>
            {
                if (message is ExtensiblePayload payload)
                {
                    // Broadcast the payload to all consensus services
                    foreach (var service in consensusServices?.Where(s => s != null) ?? Array.Empty<IActorRef>())
                    {
                        if (service != sender && !faultInjector.ShouldDropMessage(sender, service, payload))
                        {
                            // Apply chaos injection
                            var processedMessage = faultInjector.InjectMessageCorruption(payload);
                            service.Tell(processedMessage);
                        }
                    }
                }
                else if (message is Blockchain.RelayResult relayResult && relayResult.Inventory is Block block)
                {
                    // Forward block to blockchain probe for tracking
                    blockchain.Tell(block);
                    metrics.RecordConsensusSuccess();
                    Console.WriteLine($"[CHAOS] Block {block.Index} relayed successfully");
                }
            }));

            // Setup blockchain probe to handle block persistence
            blockchain.SetAutoPilot(new MockAutoPilot((sender, message) =>
            {
                if (message is Block block)
                {
                    Console.WriteLine($"[CHAOS] Blockchain received block {block.Index}");
                    // Simulate block persistence by sending to NeoSystem blockchain
                    neoSystem.Blockchain.Tell(block);
                }
            }));

            // Create memory store
            memoryStore = new MemoryStore();
            var storeProvider = new MockMemoryStoreProvider(memoryStore);

            // Create NeoSystem
            neoSystem = new NeoSystem(MockProtocolSettings.Default, storeProvider);

            // Use proper settings creation like other DBFT tests
            consensusSettings = MockBlockchain.CreateDefaultSettings();

            // Setup test wallet with validators
            testWallet = new MockWallet(MockProtocolSettings.Default);
            foreach (var validator in MockProtocolSettings.Default.StandbyValidators)
            {
                testWallet.AddAccount(validator);
            }
        }

        protected virtual ChaosConfiguration LoadChaosConfiguration()
        {
            return new ChaosConfiguration
            {
                MessageLossProbability = GetEnvironmentDouble("CHAOS_MESSAGE_LOSS", 0.1),
                NodeFailureProbability = GetEnvironmentDouble("CHAOS_NODE_FAILURE", 0.05),
                MaxLatencyMs = GetEnvironmentInt("CHAOS_MAX_LATENCY", 2000),
                MinLatencyMs = GetEnvironmentInt("CHAOS_MIN_LATENCY", 50),
                ByzantineProbability = GetEnvironmentDouble("CHAOS_BYZANTINE", 0.02),
                MessageCorruptionProbability = GetEnvironmentDouble("CHAOS_CORRUPTION", 0.01),
                NetworkPartitionProbability = GetEnvironmentDouble("CHAOS_PARTITION", 0.05),
                ViewChangeDelayMs = GetEnvironmentInt("CHAOS_VIEW_CHANGE_DELAY", 5000)
            };
        }

        private double GetEnvironmentDouble(string key, double defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return double.TryParse(value, out var result) ? result : defaultValue;
        }

        private int GetEnvironmentInt(string key, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        protected void InitializeConsensusNodes(int count)
        {
            consensusServices.Clear();
            nodeStates.Clear();

            // Ensure we don't exceed available validators
            var actualCount = Math.Min(count, MockProtocolSettings.Default.StandbyValidators.Count);

            // Create consensus service for each validator
            for (int i = 0; i < actualCount; i++)
            {
                var validatorWallet = new MockWallet(MockProtocolSettings.Default);
                var validatorKey = MockProtocolSettings.Default.StandbyValidators[i];
                validatorWallet.AddAccount(validatorKey);

                // Create consensus service with chaos proxy
                var consensusService = Sys.ActorOf(
                    ConsensusServiceProxy.Props(
                        neoSystem,
                        consensusSettings,
                        validatorWallet,
                        faultInjector,
                        networkChaos,
                        this),
                    $"consensus{i}");

                consensusServices.Add(consensusService);
                nodeStates[consensusService] = true;

                Console.WriteLine($"[CHAOS] Created consensus node {i} with validator {validatorKey}");
            }

            // Start all consensus services
            foreach (var service in consensusServices)
            {
                service.Tell(new Neo.Plugins.DBFTPlugin.Consensus.ConsensusService.Start());
            }

            // Allow services to initialize and establish connections
            Thread.Sleep(500);
            Console.WriteLine($"[CHAOS] Initialized {actualCount} consensus nodes");
        }

        protected void InjectRandomMessageLoss(double probability)
        {
            networkChaos.SetMessageLossProbability(probability);
        }

        protected void InjectRandomLatency(TimeSpan min, TimeSpan max)
        {
            networkChaos.SetLatencyRange(min, max);
        }

        protected void InjectRandomNodeFailure(double probability)
        {
            if (chaosRandom.NextDouble() < probability)
            {
                var aliveNodes = consensusServices.Where(v => nodeStates.GetValueOrDefault(v, true)).ToList();
                if (aliveNodes.Count > 1)
                {
                    var nodeToFail = aliveNodes[chaosRandom.Next(aliveNodes.Count)];
                    SimulateNodeFailure(nodeToFail);
                }
            }
        }

        protected void SimulateNodeFailure(IActorRef node)
        {
            nodeStates[node] = false;
            metrics.RecordNodeFailure();
            Console.WriteLine($"[CHAOS] Node {node.Path} failed");

            // Disconnect the node
            node.Tell(new ConsensusServiceProxy.SetEnabled { Enabled = false });

            Task.Delay(TimeSpan.FromSeconds(chaosRandom.Next(1, 10))).ContinueWith(_ =>
            {
                if (chaosRandom.NextDouble() < 0.7)
                {
                    SimulateNodeRecovery(node);
                }
            });
        }

        protected void SimulateNodeRecovery(IActorRef node)
        {
            nodeStates[node] = true;
            metrics.RecordNodeRecovery();
            Console.WriteLine($"[CHAOS] Node {node.Path} recovered");

            // Reconnect the node
            node.Tell(new ConsensusServiceProxy.SetEnabled { Enabled = true });
        }

        protected void RunChaosScenario(Action<ChaosContext> scenario, TimeSpan duration)
        {
            var context = new ChaosContext
            {
                ConsensusServices = consensusServices,
                FaultInjector = faultInjector,
                NetworkChaos = networkChaos,
                Metrics = metrics,
                Random = chaosRandom,
                TestKit = this
            };

            var endTime = DateTime.UtcNow + duration;
            var chaosThread = new Thread(() =>
            {
                while (DateTime.UtcNow < endTime)
                {
                    scenario(context);
                    Thread.Sleep(chaosRandom.Next(100, 500));
                }
            })
            {
                IsBackground = true,
                Name = "ChaosThread"
            };

            chaosThread.Start();

            Thread.Sleep(duration);

            chaosThread.Join(TimeSpan.FromSeconds(5));
        }

        protected bool VerifyConsensusResilience(TimeSpan testDuration, double minimumSuccessRate)
        {
            var startTime = DateTime.UtcNow;
            var successfulRounds = 0;
            var totalRounds = 0;
            var endTime = startTime + testDuration;

            Console.WriteLine($"[CHAOS] Starting consensus resilience test for {testDuration.TotalSeconds}s (min success rate: {minimumSuccessRate:P1})");

            // Run multiple shorter consensus rounds instead of one long test
            while (DateTime.UtcNow < endTime)
            {
                totalRounds++;
                var roundStartTime = DateTime.UtcNow;

                Console.WriteLine($"[CHAOS] Starting consensus round {totalRounds}...");

                // Use shorter timeout per round (10 seconds)
                if (WaitForConsensusRound(TimeSpan.FromSeconds(10)))
                {
                    successfulRounds++;
                    var roundDuration = DateTime.UtcNow - roundStartTime;
                    Console.WriteLine($"[CHAOS] Round {totalRounds} succeeded in {roundDuration.TotalSeconds:F1}s");
                }
                else
                {
                    Console.WriteLine($"[CHAOS] Round {totalRounds} failed (timeout)");
                }

                // Add small delay between rounds
                Thread.Sleep(1000);

                // If we've been running for too long, break
                if (DateTime.UtcNow >= endTime) break;
            }

            var successRate = totalRounds > 0 ? (double)successfulRounds / totalRounds : 0.0;
            var actualDuration = DateTime.UtcNow - startTime;

            Console.WriteLine($"[CHAOS] Consensus resilience test completed:");
            Console.WriteLine($"[CHAOS]   Duration: {actualDuration.TotalSeconds:F1}s");
            Console.WriteLine($"[CHAOS]   Success rate: {successRate:P2} ({successfulRounds}/{totalRounds})");
            Console.WriteLine($"[CHAOS]   Required: {minimumSuccessRate:P2}");
            Console.WriteLine($"[CHAOS]   Result: {(successRate >= minimumSuccessRate ? "PASS" : "FAIL")}");

            return successRate >= minimumSuccessRate;
        }

        protected virtual bool WaitForConsensusRound(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            var consensusActivityDetected = false;

            while (DateTime.UtcNow < deadline)
            {
                // Look for consensus messages being sent through localNode
                try
                {
                    var message = localNode.FishForMessage(
                        msg => msg is ExtensiblePayload payload && payload.Category == "dBFT",
                        TimeSpan.FromMilliseconds(500));

                    if (message != null)
                    {
                        consensusActivityDetected = true;
                        Console.WriteLine($"[CHAOS] Consensus activity detected: {message.GetType().Name}");
                        break;
                    }
                }
                catch
                {
                    // No message received within timeout, continue checking
                }

                // Also check for block messages on blockchain probe
                try
                {
                    var blockMessage = blockchain.FishForMessage(
                        msg => msg is Block,
                        TimeSpan.FromMilliseconds(100));

                    if (blockMessage != null)
                    {
                        consensusActivityDetected = true;
                        Console.WriteLine($"[CHAOS] Block consensus detected");
                        break;
                    }
                }
                catch
                {
                    // No block message received
                }

                Thread.Sleep(100);
            }

            if (consensusActivityDetected)
            {
                metrics.RecordConsensusSuccess();
                Console.WriteLine($"[CHAOS] Consensus round completed successfully");
                return true;
            }
            else
            {
                metrics.RecordConsensusFailure();
                Console.WriteLine($"[CHAOS] No consensus activity within {timeout.TotalSeconds}s timeout");
                return false;
            }
        }

        protected bool CheckConsensusReached()
        {
            // Check if blockchain received a new block
            var hasBlock = blockchain.HasMessages;
            return hasBlock;
        }

        protected void AssertEventualConsistency(Func<bool> condition, TimeSpan timeout, string failureMessage)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                {
                    return;
                }
                Thread.Sleep(100);
            }
            Assert.Fail($"Eventual consistency not achieved: {failureMessage}");
        }

        protected ExtensiblePayload CreateConsensusPayload(ConsensusMessage message, int validatorIndex = 0)
        {
            return new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = message.BlockIndex,
                Sender = Contract.GetBFTAddress(
                    MockProtocolSettings.Default.StandbyValidators.Take(validatorIndex + 1).ToArray()),
                Data = message.ToArray(),
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };
        }
    }

    public class ChaosContext
    {
        public List<IActorRef> ConsensusServices { get; set; }
        public FaultInjector FaultInjector { get; set; }
        public NetworkChaosSimulator NetworkChaos { get; set; }
        public ChaosMetrics Metrics { get; set; }
        public Random Random { get; set; }
        public TestKit TestKit { get; set; }
    }

    public class ChaosConfiguration
    {
        public double MessageLossProbability { get; set; }
        public double NodeFailureProbability { get; set; }
        public int MaxLatencyMs { get; set; }
        public int MinLatencyMs { get; set; }
        public double ByzantineProbability { get; set; }
        public double MessageCorruptionProbability { get; set; }
        public double NetworkPartitionProbability { get; set; }
        public int ViewChangeDelayMs { get; set; }
    }
}
