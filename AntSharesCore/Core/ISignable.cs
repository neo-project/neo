using AntShares.Core.Scripts;
using AntShares.IO;
using System.IO;

namespace AntShares.Core
{
    public interface ISignable : ISerializable
    {
        Script[] Scripts { get; set; }

        void DeserializeUnsigned(BinaryReader reader);
        UInt160[] GetScriptHashesForVerifying();
        void SerializeUnsigned(BinaryWriter writer);
    }
}
