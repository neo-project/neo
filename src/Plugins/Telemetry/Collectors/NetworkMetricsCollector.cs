// Copyright (C) 2015-2025 The Neo Project.
//
// NetworkMetricsCollector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.IEventHandlers;
using Neo.Network.P2P;
using Neo.Plugins.Telemetry.Metrics;
using System.Net;

namespace Neo.Plugins.Telemetry.Collectors
{
    /// <summary>
    /// Collects network-related metrics including peer connections and message statistics.
    /// </summary>
    public sealed class NetworkMetricsCollector : IMessageReceivedHandler, IDisposable
    {
        private readonly NeoSystem _system;
        private readonly string _nodeId;
        private readonly string _network;
        private LocalNode? _localNode;
        private bool _disposed;

        public NetworkMetricsCollector(NeoSystem system, string nodeId, string network)
        {
            _system = system ?? throw new ArgumentNullException(nameof(system));
            _nodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            _network = network ?? throw new ArgumentNullException(nameof(network));

            RemoteNode.MessageReceived += RemoteNode_MessageReceived_Handler;
            RemoteNode.MessageSent += RemoteNode_MessageSent_Handler;
            RemoteNode.ConnectionChanged += RemoteNode_ConnectionChanged_Handler;

            // Get LocalNode instance asynchronously
            InitializeLocalNode();
        }

        private async void InitializeLocalNode()
        {
            try
            {
                _localNode = await _system.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance());
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(NetworkMetricsCollector), LogLevel.Warning,
                    $"Failed to get LocalNode instance: {ex.Message}");
            }
        }

        public bool RemoteNode_MessageReceived_Handler(NeoSystem system, Message message)
        {
            if (_disposed) return true; // Must return true to allow message processing to continue

            try
            {
                // Track message received by type
                var messageType = message.Command.ToString();
                MetricsDefinitions.MessagesReceived.WithLabels(_nodeId, _network, messageType).Inc();

                // Track bytes received (approximate based on message size)
                var messageSize = message.Size;
                MetricsDefinitions.BytesReceived.WithLabels(_nodeId, _network).Inc(messageSize);
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(NetworkMetricsCollector), LogLevel.Debug,
                    $"Error tracking message metrics: {ex.Message}");
            }

            // Return true to allow the message to continue being processed
            // Returning false would stop message processing and break the P2P protocol!
            return true;
        }

        private void RemoteNode_MessageSent_Handler(NeoSystem system, Message message)
        {
            if (_disposed) return;

            try
            {
                var messageType = message.Command.ToString();
                MetricsDefinitions.MessagesSent.WithLabels(_nodeId, _network, messageType).Inc();
                MetricsDefinitions.BytesSent.WithLabels(_nodeId, _network).Inc(message.Size);
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(NetworkMetricsCollector), LogLevel.Debug,
                    $"Error tracking sent message metrics: {ex.Message}");
            }
        }

        private void RemoteNode_ConnectionChanged_Handler(NeoSystem system, IPEndPoint remote, string direction, bool connected, string reason)
        {
            if (_disposed) return;

            try
            {
                if (connected)
                {
                    MetricsDefinitions.PeerConnectionsTotal.WithLabels(_nodeId, _network, direction).Inc();
                }
                else
                {
                    MetricsDefinitions.PeerDisconnectionsTotal.WithLabels(_nodeId, _network, reason).Inc();
                }
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(NetworkMetricsCollector), LogLevel.Debug,
                    $"Error tracking connection change: {ex.Message}");
            }
        }

        /// <summary>
        /// Collects current network state metrics (called periodically).
        /// </summary>
        public void CollectCurrentState()
        {
            if (_disposed || _localNode == null) return;

            try
            {
                // Update connected peers count
                var connectedCount = _localNode.ConnectedCount;
                MetricsDefinitions.ConnectedPeers.WithLabels(_nodeId, _network).Set(connectedCount);

                // Update unconnected peers count
                var unconnectedCount = _localNode.UnconnectedCount;
                MetricsDefinitions.UnconnectedPeers.WithLabels(_nodeId, _network).Set(unconnectedCount);

                // Collect per-peer metrics if needed
                CollectPeerMetrics();
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(NetworkMetricsCollector), LogLevel.Debug,
                    $"Error collecting network state: {ex.Message}");
            }
        }

        private void CollectPeerMetrics()
        {
            if (_localNode == null) return;

            try
            {
                var remoteNodes = _localNode.GetRemoteNodes();
                foreach (var node in remoteNodes)
                {
                    // Could add per-peer metrics here if needed
                    // For now, we just count them in the aggregate metrics
                }
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(NetworkMetricsCollector), LogLevel.Debug,
                    $"Error collecting peer metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Records a peer connection event.
        /// </summary>
        /// <param name="direction">The direction of the connection (inbound/outbound).</param>
        public void RecordPeerConnection(string direction)
        {
            if (_disposed) return;
            MetricsDefinitions.PeerConnectionsTotal.WithLabels(_nodeId, _network, direction).Inc();
        }

        /// <summary>
        /// Records a peer disconnection event.
        /// </summary>
        /// <param name="reason">The reason for disconnection.</param>
        public void RecordPeerDisconnection(string reason)
        {
            if (_disposed) return;
            MetricsDefinitions.PeerDisconnectionsTotal.WithLabels(_nodeId, _network, reason).Inc();
        }

        /// <summary>
        /// Records bytes sent to the network.
        /// </summary>
        /// <param name="bytes">Number of bytes sent.</param>
        public void RecordBytesSent(long bytes)
        {
            if (_disposed) return;
            MetricsDefinitions.BytesSent.WithLabels(_nodeId, _network).Inc(bytes);
        }

        /// <summary>
        /// Records a message sent by type.
        /// </summary>
        /// <param name="messageType">The type of message sent.</param>
        public void RecordMessageSent(string messageType)
        {
            if (_disposed) return;
            MetricsDefinitions.MessagesSent.WithLabels(_nodeId, _network, messageType).Inc();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _localNode = null;
            RemoteNode.MessageReceived -= RemoteNode_MessageReceived_Handler;
            RemoteNode.MessageSent -= RemoteNode_MessageSent_Handler;
            RemoteNode.ConnectionChanged -= RemoteNode_ConnectionChanged_Handler;
        }
    }
}
