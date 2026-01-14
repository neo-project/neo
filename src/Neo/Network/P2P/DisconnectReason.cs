// Copyright (C) 2015-2026 The Neo Project.
//
// DisconnectReason.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Network.P2P;

/// <summary>
/// Specifies the reason for a disconnection event in a network or communication context.
/// </summary>
/// <remarks>Use this enumeration to determine why a connection was terminated.</remarks>
public enum DisconnectReason
{
    /// <summary>
    /// No specific reason for disconnection.
    /// </summary>
    None,

    /// <summary>
    /// The connection was closed normally.
    /// </summary>
    Close,

    /// <summary>
    /// The connection was closed due to a timeout.
    /// </summary>
    Timeout,

    /// <summary>
    /// The connection was closed due to a protocol violation.
    /// </summary>
    ProtocolViolation
}
