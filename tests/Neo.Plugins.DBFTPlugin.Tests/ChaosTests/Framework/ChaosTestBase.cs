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

            // Create memory store
            memoryStore = new MemoryStore();
            var storeProvider = new MockMemoryStoreProvider(memoryStore);

            // Create NeoSystem
            neoSystem = new NeoSystem(MockProtocolSettings.Default, storeProvider);
            // Create empty configuration section for Settings
            var configuration = new ConfigurationBuilder().Build();
            var section = configuration.GetSection("DBFTPlugin");
            consensusSettings = new Settings(section);

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

            // Create consensus service for each validator
            for (int i = 0; i < count && i < MockProtocolSettings.Default.StandbyValidators.Count; i++)
            {
                var validatorWallet = new MockWallet(MockProtocolSettings.Default);
                validatorWallet.AddAccount(MockProtocolSettings.Default.StandbyValidators[i]);

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

                // Start the consensus service
                consensusService.Tell(new Neo.Plugins.DBFTPlugin.Consensus.ConsensusService.Start());
            }

            // Allow services to initialize
            Thread.Sleep(100);
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

            while (DateTime.UtcNow - startTime < testDuration)
            {
                totalRounds++;
                if (WaitForConsensusRound(TimeSpan.FromSeconds(30)))
                {
                    successfulRounds++;
                }
            }

            var successRate = (double)successfulRounds / totalRounds;
            Console.WriteLine($"[CHAOS] Consensus success rate: {successRate:P2} ({successfulRounds}/{totalRounds})");

            return successRate >= minimumSuccessRate;
        }

        protected virtual bool WaitForConsensusRound(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline)
            {
                // Check if blockchain received a new block
                var blockMessage = blockchain.FishForMessage<Block>(
                    msg => msg is Block,
                    TimeSpan.FromMilliseconds(100));

                if (blockMessage != null)
                {
                    metrics.RecordConsensusSuccess();
                    return true;
                }

                Thread.Sleep(100);
            }

            metrics.RecordConsensusFailure();
            return false;
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
