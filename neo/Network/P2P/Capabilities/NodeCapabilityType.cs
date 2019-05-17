namespace Neo.Network.P2P.Capabilities
{
    public enum NodeCapabilityType : byte
    {
        #region Servers
        
        TcpServer = 0x01,
        WsServer = 0x02,
        
        #endregion

        FullNode = 0x10
    }
}