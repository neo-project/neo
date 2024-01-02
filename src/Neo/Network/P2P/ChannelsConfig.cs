// Copyright (C) 2015-2024 The Neo Project.
//
// ChannelsConfig.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Net;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Represents the settings to start <see cref="LocalNode"/>.
    /// </summary>
    public class ChannelsConfig
    {
        /// <summary>
        /// Tcp configuration.
        /// </summary>
        public IPEndPoint Tcp { get; set; }

        /// <summary>
        /// Minimum desired connections.
        /// </summary>
        public int MinDesiredConnections { get; set; } = Peer.DefaultMinDesiredConnections;

        /// <summary>
        /// Max allowed connections.
        /// </summary>
        public int MaxConnections { get; set; } = Peer.DefaultMaxConnections;

        /// <summary>
        /// Max allowed connections per address.
        /// </summary>
        public int MaxConnectionsPerAddress { get; set; } = 3;
    }
}
