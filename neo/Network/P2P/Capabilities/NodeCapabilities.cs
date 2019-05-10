using Neo.IO.Caching;

namespace Neo.Network.P2P.Capabilities
{
    public enum NodeCapabilities : byte
    {
        [ReflectionCache(typeof(ServerCapability))]
        Server = 0x00,
    }
}