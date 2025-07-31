// Copyright (C) 2015-2025 The Neo Project.
//
// INetworkMetricsHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Network.P2P;
using System.Collections.Generic;

namespace Neo.IEventHandlers
{
    /// <summary>
    /// Interface for plugins that need to collect network metrics
    /// </summary>
    public interface INetworkMetricsHandler
    {
        /// <summary>
        /// Called when a peer connects to the network
        /// </summary>
        /// <param name="node">The local node instance</param>
        /// <param name="peer">The connected peer</param>
        void Network_PeerConnected_Handler(LocalNode node, IActorRef peer);

        /// <summary>
        /// Called when a peer disconnects from the network
        /// </summary>
        /// <param name="node">The local node instance</param>
        /// <param name="peer">The disconnected peer</param>
        void Network_PeerDisconnected_Handler(LocalNode node, IActorRef peer);

        /// <summary>
        /// Called periodically with network statistics
        /// </summary>
        /// <param name="node">The local node instance</param>
        /// <param name="stats">Current network statistics</param>
        void Network_StatsSnapshot_Handler(LocalNode node, NetworkStats stats);
    }
}
