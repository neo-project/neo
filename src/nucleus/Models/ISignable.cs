using Neo.IO;
using System.IO;

namespace Neo.Models
{
    public interface ISignable : ISerializable
    {
        void SerializeUnsigned(BinaryWriter writer);
    }
}
