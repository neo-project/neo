using Neo.IO;

namespace Neo.Network.P2P.Capabilities
{
    public interface INodeCapability : ISerializable
    {
        NodeCapabilities Type { get; }
    }
}