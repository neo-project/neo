// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
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
