// Copyright (C) 2015-2026 The Neo Project.
//
// EndpointKind.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Network.P2P;

/// <summary>
/// Describes how an overlay endpoint was learned and how it should be used.
/// </summary>
[Flags]
public enum EndpointKind : byte
{
    /// <summary>
    /// The endpoint was observed as the remote endpoint of an incoming or
    /// outgoing connection.
    /// This usually represents a NAT-mapped or ephemeral public endpoint and
    /// should NOT be treated as a reliable dial target.
    /// </summary>
    Observed = 1,

    /// <summary>
    /// The endpoint was explicitly advertised by the peer itself, typically
    /// via protocol handshake metadata (e.g., Version/Listener port).
    ///
    /// Advertised endpoints indicate that the peer claims to be listening
    /// for incoming connections on this address and port.
    ///
    /// Actual reachability is still validated through success/failure tracking.
    /// </summary>
    Advertised = 2,

    /// <summary>
    /// The endpoint was derived indirectly rather than directly observed or
    /// self-advertised.
    ///
    /// Derived endpoints usually require validation before being trusted for
    /// active communication.
    /// </summary>
    Derived = 4,

    /// <summary>
    /// The endpoint represents a relay or intermediary rather than a direct
    /// network address of the target node.
    ///
    /// Relay endpoints are never used for direct dialing and require
    /// protocol-specific relay support.
    /// </summary>
    Relay = 8
}
