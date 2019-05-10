using Neo.IO.Caching;

namespace Neo.Network.P2P.Capabilities
{
    public enum NodeCapabilities : byte
    {
        [ReflectionCache(typeof(UInt16Capability))]
        TcpPort = 0x00,

        [ReflectionCache(typeof(UInt16Capability))]
        UdpPort = 0x01,

        [ReflectionCache(typeof(UInt16Capability))]
        WebsocketPort = 0x02,

        [ReflectionCache(typeof(StringCapability))]
        RpcServerAddress = 0x03
    }
}