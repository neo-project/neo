namespace Neo.Network.P2P.Capabilities
{
    public enum NodeCapabilityType : byte
    {
        TcpServer = 0x01,
        UdpServer = 0x02,
        WsServer = 0x03,

        FullNode = 0x10
    }
}
