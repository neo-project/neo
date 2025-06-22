// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DBFTRobustnessTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Scenarios
{
    [TestClass]
    public class UT_DBFTRobustnessTests : ChaosTestBase
    {
        [TestMethod]
        public void Test_BasicConsensusWithMinorChaos()
        {
            // Test basic consensus functionality with minimal chaos
            // This validates the framework works correctly
            InitializeConsensusNodes(4);

            // Apply light chaos (5% message loss, 100ms max latency)
            config.MessageLossProbability = 0.05;
            config.MaxLatencyMs = 100;

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(30), 0.90);
            Assert.IsTrue(success, "Consensus should handle minor chaos with >90% success rate");
        }

        [TestMethod]
        public void Test_FaultTolerance_SingleNodeFailure()
        {
            // DBFT should tolerate f=(n-1)/3 failures
            // With 4 nodes, can tolerate 1 failure
            InitializeConsensusNodes(4);

            // Simulate single node failure
            Task.Delay(1000).ContinueWith(_ =>
            {
                var nodeToFail = consensusServices[chaosRandom.Next(consensusServices.Count)];
                SimulateNodeFailure(nodeToFail);
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(45), 0.80);
            Assert.IsTrue(success, "Consensus should continue with 1 node failure (f=1, n=4)");
        }

        [TestMethod]
        public void Test_FaultTolerance_MaximumTolerableFailures()
        {
            // With 7 nodes, DBFT can tolerate up to 2 failures (f=2)
            InitializeConsensusNodes(7);

            // Fail exactly f nodes
            Task.Delay(1000).ContinueWith(_ =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var nodeToFail = consensusServices[i];
                    SimulateNodeFailure(nodeToFail);
                }
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.70);
            Assert.IsTrue(success, "Consensus should handle maximum tolerable failures (f=2, n=7)");
        }

        [TestMethod]
        public void Test_FaultTolerance_BeyondThreshold_ShouldFail()
        {
            // With 4 nodes, failing 2 nodes should break consensus (f+1 failures)
            InitializeConsensusNodes(4);

            // Fail more than f nodes
            Task.Delay(1000).ContinueWith(_ =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var nodeToFail = consensusServices[i];
                    SimulateNodeFailure(nodeToFail);
                }
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(30), 0.50);
            Assert.IsFalse(success, "Consensus should fail when f+1 nodes fail (f=1, n=4)");
        }

        [TestMethod]
        public void Test_NetworkPartition_MajorityCanContinue()
        {
            // Test network partition where majority can still reach consensus
            InitializeConsensusNodes(7);

            // Create partition: 4 nodes vs 3 nodes (majority vs minority)
            Task.Delay(1000).ContinueWith(_ =>
            {
                var partition1 = consensusServices.Take(4).ToList();
                var partition2 = consensusServices.Skip(4).ToList();

                // Isolate minority partition from majority
                faultInjector.CreateNetworkPartition(partition1, partition2);
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(45), 0.60);
            Assert.IsTrue(success, "Majority partition should continue consensus");
        }

        [TestMethod]
        public void Test_NetworkPartition_NoMajority_ShouldStall()
        {
            // Test network partition where no partition has majority
            InitializeConsensusNodes(6);

            // Create equal partitions: 3 vs 3 (no majority)
            Task.Delay(1000).ContinueWith(_ =>
            {
                var partition1 = consensusServices.Take(3).ToList();
                var partition2 = consensusServices.Skip(3).ToList();

                faultInjector.CreateNetworkPartition(partition1, partition2);
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(30), 0.30);
            Assert.IsFalse(success, "Equal partitions should not reach consensus");
        }

        [TestMethod]
        public void Test_ByzantineNode_SingleMalicious()
        {
            // Test with one byzantine node sending conflicting messages
            InitializeConsensusNodes(4);

            // Make one node byzantine
            Task.Delay(1000).ContinueWith(_ =>
            {
                var byzantineNode = consensusServices[0];
                faultInjector.EnableByzantineBehavior(byzantineNode, ByzantineType.ConflictingMessages);
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(45), 0.75);
            Assert.IsTrue(success, "Consensus should handle single byzantine node");
        }

        [TestMethod]
        public void Test_ByzantineNode_MaximumTolerable()
        {
            // With 7 nodes, can tolerate up to f=2 byzantine nodes
            InitializeConsensusNodes(7);

            // Make f nodes byzantine
            Task.Delay(1000).ContinueWith(_ =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var byzantineNode = consensusServices[i];
                    faultInjector.EnableByzantineBehavior(byzantineNode, ByzantineType.ConflictingMessages);
                }
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.60);
            Assert.IsTrue(success, "Consensus should handle f byzantine nodes");
        }

        [TestMethod]
        public void Test_MessageDelays_HighLatency()
        {
            // Test consensus under high network latency
            InitializeConsensusNodes(4);

            config.MinLatencyMs = 500;
            config.MaxLatencyMs = 2000;
            networkChaos.SetLatencyRange(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(2000));

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(90), 0.70);
            Assert.IsTrue(success, "Consensus should handle high latency");
        }

        [TestMethod]
        public void Test_MessageLoss_ModerateLevel()
        {
            // Test consensus with moderate message loss
            InitializeConsensusNodes(4);

            config.MessageLossProbability = 0.20; // 20% message loss
            InjectRandomMessageLoss(0.20);

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.65);
            Assert.IsTrue(success, "Consensus should handle 20% message loss");
        }

        [TestMethod]
        public void Test_MessageLoss_HighLevel()
        {
            // Test consensus with high message loss
            InitializeConsensusNodes(7); // More nodes to handle higher loss

            config.MessageLossProbability = 0.40; // 40% message loss
            InjectRandomMessageLoss(0.40);

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(90), 0.40);
            Assert.IsTrue(success, "Consensus should eventually succeed even with 40% message loss");
        }

        [TestMethod]
        public void Test_ViewChange_UnresponsivePrimary()
        {
            // Test view change when primary becomes unresponsive
            InitializeConsensusNodes(4);

            Task.Delay(2000).ContinueWith(_ =>
            {
                // Assume first node is primary initially
                var primaryNode = consensusServices[0];
                SimulateNodeFailure(primaryNode);
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(45), 0.75);
            Assert.IsTrue(success, "Consensus should trigger view change when primary fails");

            // Should see view change events in metrics
            var summary = metrics.GetSummary();
            Assert.IsTrue(summary.ViewChangeCount > 0, "Should have triggered view changes");
        }

        [TestMethod]
        public void Test_RecoveryAfterFailure_NodeRejoins()
        {
            // Test that consensus continues normally after failed node recovers
            InitializeConsensusNodes(4);

            IActorRef failedNode = null;

            // Fail a node, then recover it
            Task.Delay(2000).ContinueWith(_ =>
            {
                failedNode = consensusServices[1];
                SimulateNodeFailure(failedNode);
            }).ContinueWith(_ =>
            {
                Task.Delay(10000).ContinueWith(__ =>
                {
                    SimulateNodeRecovery(failedNode);
                });
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.70);
            Assert.IsTrue(success, "Consensus should handle node failure and recovery");
        }

        [TestMethod]
        public void Test_CombinedChaos_RealWorldScenario()
        {
            // Test with combination of failures that might happen in real world
            InitializeConsensusNodes(7);

            // Configure realistic chaos levels
            config.MessageLossProbability = 0.10;
            config.MaxLatencyMs = 1000;
            config.NodeFailureProbability = 0.02;

            InjectRandomMessageLoss(0.10);
            InjectRandomLatency(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1000));

            // Simulate gradual node failures and recoveries
            Task.Delay(5000).ContinueWith(_ =>
            {
                InjectRandomNodeFailure(0.15); // 15% chance of node failure
            });

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(120), 0.60);
            Assert.IsTrue(success, "Consensus should handle combined real-world chaos");
        }

        [TestMethod]
        public void Test_TimingAttack_DelayedMessages()
        {
            // Test consensus timing by delaying specific message types
            InitializeConsensusNodes(4);

            // Delay PrepareRequest messages specifically
            faultInjector.SetMessageTypeDelay(typeof(PrepareRequest), TimeSpan.FromSeconds(5));

            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.70);
            Assert.IsTrue(success, "Consensus should handle delayed PrepareRequest messages");
        }

        [TestMethod]
        public void Test_PerformanceUnderStress_ThroughputMaintenance()
        {
            // Test that consensus maintains reasonable throughput under stress
            InitializeConsensusNodes(7);

            // Apply moderate stress
            config.MessageLossProbability = 0.15;
            config.MaxLatencyMs = 500;

            var startTime = DateTime.UtcNow;
            var success = VerifyConsensusResilience(TimeSpan.FromSeconds(60), 0.65);
            var endTime = DateTime.UtcNow;

            Assert.IsTrue(success, "Consensus should maintain functionality under stress");

            var summary = metrics.GetSummary();
            var consensusRate = summary.ConsensusSuccessCount / (endTime - startTime).TotalSeconds;

            // Should maintain at least some consensus throughput
            Assert.IsTrue(consensusRate > 0.1, $"Should maintain minimum consensus rate, got {consensusRate:F2}/sec");
        }
    }
}
