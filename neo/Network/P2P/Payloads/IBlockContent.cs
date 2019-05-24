using Neo.IO;
using Neo.IO.Json;

namespace Neo.Network.P2P.Payloads
{
    public interface IBlockContent : ISerializable
    {
        UInt256 Hash { get; }
        JObject ToJson();
    }
}
