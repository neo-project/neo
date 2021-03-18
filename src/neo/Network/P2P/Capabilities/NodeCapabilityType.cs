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
