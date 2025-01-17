// Copyright (C) 2015-2025 The Neo Project.
//
// NodeCapabilityType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Network.P2P.Capabilities
{
    /// <summary>
    /// Represents the type of <see cref="NodeCapability"/>.
    /// </summary>
    public enum NodeCapabilityType : byte
    {
        #region Servers

        /// <summary>
        /// Indicates that the node is listening on a Tcp port.
        /// </summary>
        TcpServer = 0x01,

        /// <summary>
        /// Indicates that the node is listening on a WebSocket port.
        /// </summary>
        [Obsolete]
        WsServer = 0x02,

        /// <summary>
        /// Disable p2p compression
        /// </summary>
        DisableCompression = 0x03,

        #endregion

        #region Data availability

        /// <summary>
        /// Indicates that the node has complete current state.
        /// </summary>
        FullNode = 0x10,

        /// <summary>
        /// Indicates that the node stores full block history. These nodes can be used
        /// for P2P synchronization from genesis (other ones can cut the tail and
        /// won't respond to requests for old (wrt MaxTraceableBlocks) blocks).
        /// </summary>
        ArchivalNode = 0x11,

        #endregion

        #region Private extensions

        /// <summary>
        /// The first extension ID. Any subsequent can be used in an
        /// implementation-specific way.
        /// </summary>
        Extension0 = 0xf0

        #endregion
    }
}
