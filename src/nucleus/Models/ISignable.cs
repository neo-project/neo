using Neo.IO;
using System.IO;

namespace Neo.Models
{
    public interface ISignable : ISerializable
    {
        Witness[] Witnesses { get; }
        void SerializeUnsigned(BinaryWriter writer);
    }
}
