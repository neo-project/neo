namespace Neo.Network.P2P.Capabilities
{
    public enum NodeCapabilities : byte
    {
        TcpServer = 0x01,
        UdpServer = 0x02,
        WsServer = 0x03,

        FullNode = 0x10,
        AcceptRelay = 0x11
    }
}