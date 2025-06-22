// Copyright (C) 2015-2025 The Neo Project.
//
// FaultInjector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Utilities;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Framework
{
    public enum ByzantineType
    {
        ConflictingMessages,
        InvalidSignatures,
        IgnoreProtocol,
        OutOfOrderMessages,
        DoubleVoting,
        WrongViewNumber
    }

    public class FaultInjector
    {
        private readonly Random random;
        private readonly ChaosMetrics metrics;
        private readonly ChaosConfiguration config;
        private readonly Dictionary<IActorRef, HashSet<IActorRef>> networkPartitions = new Dictionary<IActorRef, HashSet<IActorRef>>();
        private readonly Dictionary<IActorRef, ByzantineType> byzantineNodes = new Dictionary<IActorRef, ByzantineType>();
        private readonly Dictionary<Type, TimeSpan> messageTypeDelays = new Dictionary<Type, TimeSpan>();

        public FaultInjector(Random random, ChaosMetrics metrics, ChaosConfiguration config)
        {
            this.random = random;
            this.metrics = metrics;
            this.config = config;
        }

        public bool ShouldDropMessage(IActorRef sender, IActorRef receiver, object message)
        {
            // Check network partitions
            if (networkPartitions.TryGetValue(sender, out var blockedReceivers) && blockedReceivers.Contains(receiver))
            {
                metrics.RecordMessageLoss();
                return true;
            }

            // Random message loss
            if (random.NextDouble() < config.MessageLossProbability)
            {
                metrics.RecordMessageLoss();
                Console.WriteLine($"[CHAOS] Dropping message from {sender.Path} to {receiver.Path}");
                return true;
            }

            return false;
        }

        public object InjectMessageCorruption(object message)
        {
            if (!(message is ExtensiblePayload payload))
                return message;

            if (random.NextDouble() < config.MessageCorruptionProbability)
            {
                metrics.RecordMessageCorruption();

                var corruptionType = random.Next(3);
                switch (corruptionType)
                {
                    case 0:
                        return CorruptMessageData(payload);
                    case 1:
                        return DuplicateMessage(payload);
                    case 2:
                        return DelayMessage(payload);
                    default:
                        return payload;
                }
            }
            return message;
        }

        private ExtensiblePayload CorruptMessageData(ExtensiblePayload payload)
        {
            Console.WriteLine("[CHAOS] Corrupting message data");

            try
            {
                // Create a corrupted copy by manipulating the serialized data
                var data = payload.ToArray();

                // Corrupt random bytes in the data
                var corruptionCount = random.Next(1, Math.Min(5, data.Length / 10));
                for (int i = 0; i < corruptionCount; i++)
                {
                    var position = random.Next(data.Length);
                    data[position] = (byte)random.Next(256);
                }

                // Try to deserialize the corrupted data
                try
                {
                    var corrupted = data.AsSerializable<ExtensiblePayload>();
                    return corrupted;
                }
                catch
                {
                    // If deserialization fails, return original
                    return payload;
                }
            }
            catch
            {
                return payload;
            }
        }

        private ExtensiblePayload DuplicateMessage(ExtensiblePayload payload)
        {
            Console.WriteLine("[CHAOS] Creating duplicate message");
            // Return the same message - the test framework will send it twice
            return payload;
        }

        private ExtensiblePayload DelayMessage(ExtensiblePayload payload)
        {
            Console.WriteLine("[CHAOS] Delaying message");
            // Add delay metadata - actual delay handled by NetworkChaosSimulator
            return payload;
        }

        public void InjectNetworkPartition(IActorRef node1, IActorRef node2)
        {
            if (!networkPartitions.ContainsKey(node1))
                networkPartitions[node1] = new HashSet<IActorRef>();
            if (!networkPartitions.ContainsKey(node2))
                networkPartitions[node2] = new HashSet<IActorRef>();

            networkPartitions[node1].Add(node2);
            networkPartitions[node2].Add(node1);

            metrics.RecordNetworkPartition();
            Console.WriteLine($"[CHAOS] Network partition between {node1.Path} and {node2.Path}");
        }

        public void HealNetworkPartition(IActorRef node1, IActorRef node2)
        {
            networkPartitions.GetValueOrDefault(node1)?.Remove(node2);
            networkPartitions.GetValueOrDefault(node2)?.Remove(node1);

            Console.WriteLine($"[CHAOS] Healed network partition between {node1.Path} and {node2.Path}");
        }

        public void InjectByzantineBehavior(IActorRef byzantineNode)
        {
            if (random.NextDouble() < config.ByzantineProbability)
            {
                metrics.RecordByzantineBehavior();
                var behaviorType = random.Next(4);

                switch (behaviorType)
                {
                    case 0:
                        Console.WriteLine($"[CHAOS] Byzantine node {byzantineNode.Path} sending conflicting messages");
                        break;
                    case 1:
                        Console.WriteLine($"[CHAOS] Byzantine node {byzantineNode.Path} sending invalid signatures");
                        break;
                    case 2:
                        Console.WriteLine($"[CHAOS] Byzantine node {byzantineNode.Path} ignoring protocol");
                        break;
                    case 3:
                        Console.WriteLine($"[CHAOS] Byzantine node {byzantineNode.Path} sending messages out of order");
                        break;
                }
            }
        }

        public bool ShouldDropMessageType(ConsensusMessageType messageType)
        {
            // Selective message type dropping for testing specific scenarios
            var dropProbabilities = new Dictionary<ConsensusMessageType, double>
            {
                { ConsensusMessageType.PrepareRequest, 0.05 },
                { ConsensusMessageType.PrepareResponse, 0.02 },
                { ConsensusMessageType.Commit, 0.02 },
                { ConsensusMessageType.ChangeView, 0.1 },
                { ConsensusMessageType.RecoveryRequest, 0.01 },
                { ConsensusMessageType.RecoveryMessage, 0.01 }
            };

            if (dropProbabilities.TryGetValue(messageType, out var probability))
            {
                return random.NextDouble() < probability * config.MessageLossProbability;
            }

            return false;
        }

        public void InjectPartialNetworkPartition(List<IActorRef> nodes, out List<IActorRef> partition1, out List<IActorRef> partition2)
        {
            // Randomly split nodes into two partitions
            partition1 = new List<IActorRef>();
            partition2 = new List<IActorRef>();

            foreach (var node in nodes)
            {
                if (random.NextDouble() < 0.5)
                    partition1.Add(node);
                else
                    partition2.Add(node);
            }

            // Ensure each partition has at least one node
            if (partition1.Count == 0 && nodes.Count > 0)
            {
                partition1.Add(partition2[0]);
                partition2.RemoveAt(0);
            }
            else if (partition2.Count == 0 && nodes.Count > 0)
            {
                partition2.Add(partition1[0]);
                partition1.RemoveAt(0);
            }

            // Create network partition between the two groups
            foreach (var node1 in partition1)
            {
                foreach (var node2 in partition2)
                {
                    InjectNetworkPartition(node1, node2);
                }
            }

            Console.WriteLine($"[CHAOS] Network partition created: Group1={partition1.Count} nodes, Group2={partition2.Count} nodes");
        }

        public ExtensiblePayload CreateByzantineMessage(ExtensiblePayload originalPayload, IActorRef byzantineNode)
        {
            Console.WriteLine($"[CHAOS] Creating Byzantine message from {byzantineNode.Path}");

            // Create a message with invalid or conflicting data
            var byzantineType = random.Next(3);

            switch (byzantineType)
            {
                case 0:
                    // Send messages with wrong view number
                    return CorruptMessageData(originalPayload);

                case 1:
                    // Send duplicate messages with different content
                    return DuplicateMessage(originalPayload);

                case 2:
                default:
                    // Send messages out of order or with invalid signatures
                    return originalPayload;
            }
        }

        public void EnableByzantineBehavior(IActorRef node, ByzantineType behaviorType)
        {
            byzantineNodes[node] = behaviorType;
            metrics.RecordByzantineBehavior();
            Console.WriteLine($"[CHAOS] Enabled Byzantine behavior {behaviorType} for node {node.Path}");
        }

        public void DisableByzantineBehavior(IActorRef node)
        {
            byzantineNodes.Remove(node);
            Console.WriteLine($"[CHAOS] Disabled Byzantine behavior for node {node.Path}");
        }

        public bool IsByzantineNode(IActorRef node)
        {
            return byzantineNodes.ContainsKey(node);
        }

        public ByzantineType GetByzantineBehavior(IActorRef node)
        {
            return byzantineNodes.GetValueOrDefault(node);
        }

        public void SetMessageTypeDelay(Type messageType, TimeSpan delay)
        {
            messageTypeDelays[messageType] = delay;
            Console.WriteLine($"[CHAOS] Set delay of {delay.TotalMilliseconds}ms for {messageType.Name} messages");
        }

        public TimeSpan GetMessageTypeDelay(Type messageType)
        {
            return messageTypeDelays.GetValueOrDefault(messageType, TimeSpan.Zero);
        }

        public void CreateNetworkPartition(List<IActorRef> partition1, List<IActorRef> partition2)
        {
            // Create bidirectional network partition between two groups
            foreach (var node1 in partition1)
            {
                foreach (var node2 in partition2)
                {
                    InjectNetworkPartition(node1, node2);
                }
            }

            Console.WriteLine($"[CHAOS] Created network partition: {partition1.Count} vs {partition2.Count} nodes");
        }

        public void HealNetworkPartition(List<IActorRef> partition1, List<IActorRef> partition2)
        {
            // Heal network partition between two groups
            foreach (var node1 in partition1)
            {
                foreach (var node2 in partition2)
                {
                    HealNetworkPartition(node1, node2);
                }
            }

            Console.WriteLine($"[CHAOS] Healed network partition between groups");
        }

        public bool ShouldInjectByzantineMessage(IActorRef sender, object message)
        {
            if (!byzantineNodes.ContainsKey(sender))
                return false;

            var behaviorType = byzantineNodes[sender];
            var injectProbability = behaviorType switch
            {
                ByzantineType.ConflictingMessages => 0.3,
                ByzantineType.InvalidSignatures => 0.2,
                ByzantineType.IgnoreProtocol => 0.1,
                ByzantineType.OutOfOrderMessages => 0.25,
                ByzantineType.DoubleVoting => 0.4,
                ByzantineType.WrongViewNumber => 0.3,
                _ => 0.1
            };

            return random.NextDouble() < injectProbability;
        }

        public object CreateByzantineMessage(IActorRef sender, object originalMessage)
        {
            if (!byzantineNodes.TryGetValue(sender, out var behaviorType))
                return originalMessage;

            if (!(originalMessage is ExtensiblePayload payload))
                return originalMessage;

            try
            {
                // Since ConsensusMessage is abstract, we'll work directly with the payload data
                switch (behaviorType)
                {
                    case ByzantineType.ConflictingMessages:
                        return CreateConflictingMessage(payload);

                    case ByzantineType.WrongViewNumber:
                        return CreateWrongViewMessage(payload);

                    case ByzantineType.DoubleVoting:
                        return CreateDoubleVotingMessage(payload);

                    default:
                        return CorruptMessageData(payload);
                }
            }
            catch
            {
                return originalMessage;
            }
        }

        private ExtensiblePayload CreateConflictingMessage(ExtensiblePayload original)
        {
            Console.WriteLine($"[CHAOS] Creating conflicting message");

            // Create message with conflicting data but same structure
            var corruptedData = original.Data.ToArray();

            // Modify specific bytes that would create conflicts
            if (corruptedData.Length > 10)
            {
                // Modify payload data to create conflicting content
                for (int i = 5; i < Math.Min(10, corruptedData.Length); i++)
                {
                    corruptedData[i] = (byte)(corruptedData[i] ^ 0xFF);
                }
            }

            return new ExtensiblePayload
            {
                Category = original.Category,
                ValidBlockStart = original.ValidBlockStart,
                ValidBlockEnd = original.ValidBlockEnd,
                Sender = original.Sender,
                Data = corruptedData,
                Witness = original.Witness
            };
        }

        private ExtensiblePayload CreateWrongViewMessage(ExtensiblePayload original)
        {
            Console.WriteLine($"[CHAOS] Creating wrong view message");

            // Modify view number in the message
            try
            {
                // Byzantine node sends message with wrong view number
                // This should be detected and rejected by honest nodes
                return CorruptMessageData(original);
            }
            catch
            {
                return original;
            }
        }

        private ExtensiblePayload CreateDoubleVotingMessage(ExtensiblePayload original)
        {
            Console.WriteLine($"[CHAOS] Creating double voting message");

            // For double voting, we need to create a different message
            // with same validator but different content (like different block hash)
            return CreateConflictingMessage(original);
        }

        public void SimulateSlowNode(IActorRef node, double slownessFactor = 2.0)
        {
            // Mark node as slow - this would be handled by NetworkChaosSimulator
            Console.WriteLine($"[CHAOS] Node {node.Path} slowed down by factor {slownessFactor}");
        }

        public void Reset()
        {
            networkPartitions.Clear();
            byzantineNodes.Clear();
            messageTypeDelays.Clear();
        }
    }
}
