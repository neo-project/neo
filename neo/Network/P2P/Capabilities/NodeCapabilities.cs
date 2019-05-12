using Neo.IO.Caching;

namespace Neo.Network.P2P.Capabilities
{
    public enum NodeCapabilities : byte
    {
        [ReflectionCache(typeof(TcpServerCapability))]
        TcpServer = 0x01,

        [ReflectionCache(typeof(UdpServerCapability))]
        UdpServer = 0x02,

        [ReflectionCache(typeof(WsServerCapability))]
        WsServer = 0x03,
    }
}