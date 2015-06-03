using AntShares.IO;
using System.IO;
using System.Text;

namespace AntShares.Network.Payloads
{
    public abstract class Payload : ISerializable
    {
        public abstract void Deserialize(BinaryReader reader);

        public static T FromBytes<T>(byte[] data) where T : Payload, new()
        {
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return reader.ReadSerializable<T>();
            }
        }

        public abstract void Serialize(BinaryWriter writer);

        public byte[] ToArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                Serialize(writer);
                return ms.ToArray();
            }
        }
    }
}
