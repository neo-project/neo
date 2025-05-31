// Copyright (C) 2015-2025 The Neo Project.
//
// ChannelsConfig.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System.Net;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Represents the settings to start <see cref="LocalNode"/>.
    /// </summary>
    public class ChannelsConfig
    {
        /// <summary>
        /// The default value for enable compression.
        /// </summary>
        public const bool DefaultEnableCompression = true;

        /// <summary>
        /// The default minimum number of desired connections.
        /// </summary>
        public const int DefaultMinDesiredConnections = 10;

        /// <summary>
        /// The default maximum number of desired connections.
        /// </summary>
        public const int DefaultMaxConnections = DefaultMinDesiredConnections * 4;

        /// <summary>
        /// The default maximum allowed connections per address.
        /// </summary>
        public const int DefaultMaxConnectionsPerAddress = 3;

        /// <summary>
        /// The default maximum knwon hashes.
        /// </summary>
        public const int DefaultMaxKnownHashes = 1000;

        /// <summary>
        /// Tcp configuration.
        /// </summary>
        public IPEndPoint? Tcp { get; set; }

        /// <summary>
        /// Enable compression.
        /// </summary>
        public bool EnableCompression { get; set; } = DefaultEnableCompression;

        /// <summary>
        /// Minimum desired connections.
        /// </summary>
        public int MinDesiredConnections { get; set; } = DefaultMinDesiredConnections;

        /// <summary>
        /// Max allowed connections.
        /// </summary>
        public int MaxConnections { get; set; } = DefaultMaxConnections;

        /// <summary>
        /// Max allowed connections per address.
        /// </summary>
        public int MaxConnectionsPerAddress { get; set; } = DefaultMaxConnectionsPerAddress;

        /// <summary>
        /// Max known hashes
        /// </summary>
        public int MaxKnownHashes { get; set; } = DefaultMaxKnownHashes;
    }
}

#nullable disable
