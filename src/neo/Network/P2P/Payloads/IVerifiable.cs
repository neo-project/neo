using Neo.IO;
using Neo.Persistence;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public interface IVerifiable : ISerializable
    {
        Witness[] Witnesses { get; }

        void DeserializeUnsigned(BinaryReader reader);

        UInt160[] GetScriptHashesForVerifying(StoreView snapshot);

        void SerializeUnsigned(BinaryWriter writer);
    }
}
