using AntShares.IO;
using System.IO;

namespace AntShares.Network
{
    public abstract class Payload : ISerializable
    {
        public abstract void Deserialize(BinaryReader reader);

        public abstract void Serialize(BinaryWriter writer);

        public byte[] ToArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                Serialize(writer);
                return ms.ToArray();
            }
        }
    }
}
