// Copyright (C) 2015-2025 The Neo Project.
//
// NetworkChaosSimulator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Framework
{
    public class NetworkChaosSimulator
    {
        private readonly Random random;
        private readonly ChaosMetrics metrics;
        private readonly ChaosConfiguration config;

        private double messageLossProbability;
        private TimeSpan minLatency;
        private TimeSpan maxLatency;
        private readonly ConcurrentDictionary<string, NetworkLink> networkLinks;
        private readonly ConcurrentDictionary<IActorRef, NodeNetworkState> nodeStates;

        public NetworkChaosSimulator(Random random, ChaosMetrics metrics, ChaosConfiguration config)
        {
            this.random = random;
            this.metrics = metrics;
            this.config = config;
            messageLossProbability = config.MessageLossProbability;
            minLatency = TimeSpan.FromMilliseconds(config.MinLatencyMs);
            maxLatency = TimeSpan.FromMilliseconds(config.MaxLatencyMs);
            networkLinks = new ConcurrentDictionary<string, NetworkLink>();
            nodeStates = new ConcurrentDictionary<IActorRef, NodeNetworkState>();
        }

        public void SetMessageLossProbability(double probability)
        {
            messageLossProbability = Math.Max(0, Math.Min(1, probability));
            Console.WriteLine($"[CHAOS] Message loss probability set to {messageLossProbability:P2}");
        }

        public void SetLatencyRange(TimeSpan min, TimeSpan max)
        {
            minLatency = min;
            maxLatency = max;
            Console.WriteLine($"[CHAOS] Latency range set to {min.TotalMilliseconds}ms - {max.TotalMilliseconds}ms");
        }

        public bool ShouldDropMessage(IActorRef from, IActorRef to)
        {
            var link = GetOrCreateLink(from, to);

            if (!IsNodeReachable(from) || !IsNodeReachable(to))
            {
                metrics.RecordMessageLoss();
                return true;
            }

            if (link.IsPartitioned)
            {
                metrics.RecordMessageLoss();
                return true;
            }

            if (random.NextDouble() < messageLossProbability || random.NextDouble() < link.PacketLoss)
            {
                metrics.RecordMessageLoss();
                Console.WriteLine($"[CHAOS] Dropped message from {from.Path} to {to.Path}");
                return true;
            }

            return false;
        }

        public TimeSpan GetMessageDelay(IActorRef from, IActorRef to)
        {
            var link = GetOrCreateLink(from, to);
            var baseLatency = GetRandomLatency();
            var linkLatency = link.GetLatency(random);
            var totalLatency = baseLatency + linkLatency;

            metrics.RecordLatency((int)totalLatency.TotalMilliseconds);
            return totalLatency;
        }

        private TimeSpan GetRandomLatency()
        {
            var range = maxLatency.TotalMilliseconds - minLatency.TotalMilliseconds;
            var latencyMs = minLatency.TotalMilliseconds + (random.NextDouble() * range);
            return TimeSpan.FromMilliseconds(latencyMs);
        }

        public void SendWithChaos(IActorRef from, IActorRef to, object message)
        {
            if (ShouldDropMessage(from, to))
            {
                return;
            }

            var delay = GetMessageDelay(from, to);

            if (delay > TimeSpan.Zero)
            {
                Task.Delay(delay).ContinueWith(_ => to.Tell(message, from));
            }
            else
            {
                to.Tell(message, from);
            }
        }

        public void SimulateNetworkPartition(List<IActorRef> partition1, List<IActorRef> partition2, TimeSpan duration)
        {
            Console.WriteLine($"[CHAOS] Simulating network partition for {duration.TotalSeconds} seconds");
            metrics.RecordNetworkPartition();

            foreach (var node1 in partition1)
            {
                foreach (var node2 in partition2)
                {
                    var link1 = GetOrCreateLink(node1, node2);
                    var link2 = GetOrCreateLink(node2, node1);
                    link1.IsPartitioned = true;
                    link2.IsPartitioned = true;
                }
            }

            Task.Delay(duration).ContinueWith(_ =>
            {
                Console.WriteLine("[CHAOS] Healing network partition");
                foreach (var node1 in partition1)
                {
                    foreach (var node2 in partition2)
                    {
                        var link1 = GetOrCreateLink(node1, node2);
                        var link2 = GetOrCreateLink(node2, node1);
                        link1.IsPartitioned = false;
                        link2.IsPartitioned = false;
                    }
                }
            });
        }

        public void SimulateAsymmetricPartition(IActorRef isolatedNode, List<IActorRef> otherNodes, bool canSend, bool canReceive)
        {
            Console.WriteLine($"[CHAOS] Simulating asymmetric partition for {isolatedNode.Path} (canSend={canSend}, canReceive={canReceive})");

            foreach (var other in otherNodes)
            {
                if (!canSend)
                {
                    var linkOut = GetOrCreateLink(isolatedNode, other);
                    linkOut.IsPartitioned = true;
                }

                if (!canReceive)
                {
                    var linkIn = GetOrCreateLink(other, isolatedNode);
                    linkIn.IsPartitioned = true;
                }
            }
        }

        public void SetLinkQuality(IActorRef from, IActorRef to, double packetLoss, TimeSpan additionalLatency)
        {
            var link = GetOrCreateLink(from, to);
            link.PacketLoss = Math.Max(0, Math.Min(1, packetLoss));
            link.AdditionalLatency = additionalLatency;

            Console.WriteLine($"[CHAOS] Link {from.Path} -> {to.Path}: packet loss={packetLoss:P2}, additional latency={additionalLatency.TotalMilliseconds}ms");
        }

        public void SimulateBandwidthThrottling(IActorRef node, int maxMessagesPerSecond)
        {
            var state = GetOrCreateNodeState(node);
            state.MaxMessagesPerSecond = maxMessagesPerSecond;
            Console.WriteLine($"[CHAOS] Node {node.Path} throttled to {maxMessagesPerSecond} messages/second");
        }

        public void SimulateJitter(IActorRef from, IActorRef to, TimeSpan maxJitter)
        {
            var link = GetOrCreateLink(from, to);
            link.MaxJitter = maxJitter;
            Console.WriteLine($"[CHAOS] Link {from.Path} -> {to.Path}: max jitter={maxJitter.TotalMilliseconds}ms");
        }

        public void SimulateNodeSlowdown(IActorRef node, double slowdownFactor)
        {
            var state = GetOrCreateNodeState(node);
            state.SlowdownFactor = Math.Max(1, slowdownFactor);
            Console.WriteLine($"[CHAOS] Node {node.Path} slowed down by factor of {slowdownFactor}");
        }

        public void DisconnectNode(IActorRef node)
        {
            var state = GetOrCreateNodeState(node);
            state.IsConnected = false;
            metrics.RecordNodeFailure();
            Console.WriteLine($"[CHAOS] Node {node.Path} disconnected from network");
        }

        public void ReconnectNode(IActorRef node)
        {
            var state = GetOrCreateNodeState(node);
            state.IsConnected = true;
            metrics.RecordNodeRecovery();
            Console.WriteLine($"[CHAOS] Node {node.Path} reconnected to network");
        }

        private bool IsNodeReachable(IActorRef node)
        {
            return nodeStates.TryGetValue(node, out var state) ? state.IsConnected : true;
        }

        private NetworkLink GetOrCreateLink(IActorRef from, IActorRef to)
        {
            var key = $"{from.Path}:{to.Path}";
            return networkLinks.GetOrAdd(key, _ => new NetworkLink());
        }

        private NodeNetworkState GetOrCreateNodeState(IActorRef node)
        {
            return nodeStates.GetOrAdd(node, _ => new NodeNetworkState());
        }

        public void Reset()
        {
            networkLinks.Clear();
            nodeStates.Clear();
            messageLossProbability = config.MessageLossProbability;
            minLatency = TimeSpan.FromMilliseconds(config.MinLatencyMs);
            maxLatency = TimeSpan.FromMilliseconds(config.MaxLatencyMs);
        }

        private class NetworkLink
        {
            public bool IsPartitioned { get; set; }
            public double PacketLoss { get; set; }
            public TimeSpan AdditionalLatency { get; set; }
            public TimeSpan MaxJitter { get; set; }
            private readonly object lockObj = new object();
            private readonly DateTime lastMessageTime = DateTime.MinValue;

            public TimeSpan GetLatency(Random random)
            {
                var jitter = MaxJitter.TotalMilliseconds > 0
                    ? TimeSpan.FromMilliseconds(random.NextDouble() * MaxJitter.TotalMilliseconds)
                    : TimeSpan.Zero;

                return AdditionalLatency + jitter;
            }
        }

        private class NodeNetworkState
        {
            public bool IsConnected { get; set; } = true;
            public double SlowdownFactor { get; set; } = 1.0;
            public int MaxMessagesPerSecond { get; set; } = int.MaxValue;
            private readonly SemaphoreSlim rateLimiter = new SemaphoreSlim(1);
            private DateTime windowStart = DateTime.UtcNow;
            private int messagesInWindow = 0;

            public async Task<bool> CanSendMessage()
            {
                await rateLimiter.WaitAsync();
                try
                {
                    var now = DateTime.UtcNow;
                    if (now - windowStart > TimeSpan.FromSeconds(1))
                    {
                        windowStart = now;
                        messagesInWindow = 0;
                    }

                    if (messagesInWindow >= MaxMessagesPerSecond)
                    {
                        return false;
                    }

                    messagesInWindow++;
                    return true;
                }
                finally
                {
                    rateLimiter.Release();
                }
            }
        }
    }
}
