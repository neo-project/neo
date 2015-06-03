using System.IO;

namespace AntShares.IO
{
    internal interface ISerializable
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }
}
