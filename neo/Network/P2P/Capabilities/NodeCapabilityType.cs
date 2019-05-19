namespace Neo.Network.P2P.Capabilities
{
    public enum NodeCapabilityType : byte
    {
        //Servers
        TcpServer = 0x01,
        WsServer = 0x02,
        UdpServer = 0x03,

        //Others
        FullNode = 0x10
    }
}
