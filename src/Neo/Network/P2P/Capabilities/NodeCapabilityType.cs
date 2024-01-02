// Copyright (C) 2015-2024 The Neo Project.
//
// NodeCapabilityType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
        WsServer = 0x02,

        #endregion

        #region Others

        /// <summary>
        /// Indicates that the node has complete block data.
        /// </summary>
        FullNode = 0x10

        #endregion
    }
}
