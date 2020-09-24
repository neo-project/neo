using System.IO;
using Neo.IO;

namespace Neo.Models
{
    public interface IWitnessed : ISerializable
    {
        Witness[] Witnesses { get; }
        void SerializeUnsigned(BinaryWriter writer);
        void DeserializeUnsigned(BinaryReader reader);
    }
}
