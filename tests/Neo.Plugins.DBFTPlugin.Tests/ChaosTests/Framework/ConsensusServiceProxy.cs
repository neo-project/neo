// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusServiceProxy.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Sign;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Framework
{
    /// <summary>
    /// Wrapper actor that intercepts messages to/from ConsensusService to inject chaos
    /// </summary>
    public class ConsensusServiceProxy : UntypedActor
    {
        private readonly IActorRef actualConsensusService;
        private readonly FaultInjector faultInjector;
        private readonly NetworkChaosSimulator networkChaos;
        private readonly ChaosTestBase testBase;

        private bool isEnabled = true;
        private bool hasReachedConsensus = false;
        private readonly Dictionary<IActorRef, IActorRef> peerMapping = new Dictionary<IActorRef, IActorRef>();

        public bool HasReachedConsensus => hasReachedConsensus;

        public ConsensusServiceProxy(
            NeoSystem neoSystem,
            Settings dbftSettings,
            ISigner signer,
            FaultInjector faultInjector,
            NetworkChaosSimulator networkChaos,
            ChaosTestBase testBase)
        {
            this.faultInjector = faultInjector;
            this.networkChaos = networkChaos;
            this.testBase = testBase;

            // Create the actual consensus service as a child actor
            actualConsensusService = Context.ActorOf(
                Neo.Plugins.DBFTPlugin.Consensus.ConsensusService.Props(neoSystem, dbftSettings, signer),
                "consensus-actual");

            // Watch for consensus service failures
            Context.Watch(actualConsensusService);
        }

        protected override void OnReceive(object message)
        {
            if (!isEnabled && !(message is SetEnabled))
            {
                // Node is failed, drop all messages except re-enable
                return;
            }

            switch (message)
            {
                case ExtensiblePayload payload when payload.Category == "dBFT":
                    HandleIncomingConsensusPayload(payload);
                    break;

                case object rr when rr.GetType().Name == "RelayResult":
                    HandleRelayResult(rr);
                    break;

                case LocalNode.SendDirectly sendDirectly:
                    HandleSendDirectly(sendDirectly);
                    break;

                case Block block:
                    // Record successful consensus
                    hasReachedConsensus = true;
                    actualConsensusService.Forward(block);
                    break;

                case object timer when timer.GetType().Name == "Timer":
                    HandleTimer(timer);
                    break;

                case SetEnabled setEnabled:
                    SetEnabledState(setEnabled.Enabled);
                    break;

                case Terminated terminated when terminated.ActorRef.Equals(actualConsensusService):
                    // Consensus service terminated
                    Context.Stop(Self);
                    break;

                default:
                    // Forward all other messages directly
                    actualConsensusService.Forward(message);
                    break;
            }
        }

        private void HandleIncomingConsensusPayload(ExtensiblePayload payload)
        {
            // Find the sender proxy if it exists
            var senderProxy = testBase.consensusServices.FirstOrDefault(s =>
                peerMapping.ContainsKey(s) &&
                peerMapping[s].Equals(Sender));

            var effectiveSender = senderProxy ?? Sender;

            // Check if message should be dropped
            if (faultInjector.ShouldDropMessage(effectiveSender, Self, payload))
            {
                Console.WriteLine($"[CHAOS] Dropped incoming consensus message to {Self.Path}");
                return;
            }

            // Apply message corruption if configured
            var possiblyCorrupted = faultInjector.InjectMessageCorruption(payload);
            if (possiblyCorrupted != payload)
            {
                Console.WriteLine($"[CHAOS] Corrupted incoming consensus message to {Self.Path}");
                payload = possiblyCorrupted as ExtensiblePayload ?? payload;
            }

            // Apply network delay
            var delay = networkChaos.GetMessageDelay(effectiveSender, Self);
            if (delay > TimeSpan.Zero)
            {
                Context.System.Scheduler.ScheduleTellOnce(delay, actualConsensusService, payload, Sender);
            }
            else
            {
                actualConsensusService.Tell(payload, Sender);
            }
        }

        private void HandleRelayResult(object rr)
        {
            // Use reflection to access Inventory property
            var inventoryProperty = rr.GetType().GetProperty("Inventory");
            if (inventoryProperty?.GetValue(rr) is ExtensiblePayload payload && payload.Category == "dBFT")
            {
                HandleIncomingConsensusPayload(payload);
            }
            else
            {
                actualConsensusService.Forward(rr);
            }
        }

        private void HandleSendDirectly(LocalNode.SendDirectly sendDirectly)
        {
            // Intercept outgoing messages
            if (sendDirectly.Inventory is ExtensiblePayload payload && payload.Category == "dBFT")
            {
                // Apply chaos to outgoing messages
                foreach (var peer in testBase.consensusServices)
                {
                    if (peer != Self)
                    {
                        if (!faultInjector.ShouldDropMessage(Self, peer, payload))
                        {
                            var delay = networkChaos.GetMessageDelay(Self, peer);

                            // Send directly as ExtensiblePayload since RelayResult is internal
                            var messageToSend = payload;

                            if (delay > TimeSpan.Zero)
                            {
                                Context.System.Scheduler.ScheduleTellOnce(delay, peer, messageToSend, Self);
                            }
                            else
                            {
                                peer.Tell(messageToSend, Self);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CHAOS] Dropped outgoing message from {Self.Path} to {peer.Path}");
                        }
                    }
                }

                // Don't forward to actual localNode test probe
            }
            else
            {
                // Forward non-consensus messages normally
                actualConsensusService.Forward(sendDirectly);
            }
        }

        private void HandleTimer(object timer)
        {
            // Apply chaos to timer events (simulate clock skew)
            if (testBase.config.ViewChangeDelayMs > 0 && testBase.chaosRandom.NextDouble() < 0.1)
            {
                var delay = TimeSpan.FromMilliseconds(testBase.chaosRandom.Next(0, testBase.config.ViewChangeDelayMs));
                Context.System.Scheduler.ScheduleTellOnce(delay, actualConsensusService, timer, Sender);
            }
            else
            {
                actualConsensusService.Forward(timer);
            }
        }


        public void SetEnabledState(bool enabled)
        {
            isEnabled = enabled;
            if (!enabled)
            {
                hasReachedConsensus = false;
                Console.WriteLine($"[CHAOS] Node {Self.Path} disabled");
            }
            else
            {
                Console.WriteLine($"[CHAOS] Node {Self.Path} enabled");
            }
        }

        public class SetEnabled
        {
            public bool Enabled { get; set; }
        }

        public static Props Props(
            NeoSystem neoSystem,
            Settings dbftSettings,
            ISigner signer,
            FaultInjector faultInjector,
            NetworkChaosSimulator networkChaos,
            ChaosTestBase testBase)
        {
            return Akka.Actor.Props.Create(() =>
                new ConsensusServiceProxy(neoSystem, dbftSettings, signer, faultInjector, networkChaos, testBase));
        }
    }
}
