using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public interface IVerifiable : ISerializable
    {
        void DeserializeUnsigned(BinaryReader reader);

        void SerializeUnsigned(BinaryWriter writer);
    }
}
