using Neo.IO;
using Neo.Persistence;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public interface IVerifiable : ISerializable
    {
        Witness Witness { get; set; }

        void DeserializeUnsigned(BinaryReader reader);

        UInt160 GetScriptHashForVerification(Snapshot snapshot);

        void SerializeUnsigned(BinaryWriter writer);
    }
}
