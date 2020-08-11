using System.IO;

namespace Neo.IO.Serialization
{
    public class ByteArraySerializer : Serializer<byte[]>
    {
        public override byte[] Deserialize(BinaryReader reader, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            return reader.ReadVarBytes(max);
        }

        public override void Serialize(BinaryWriter writer, byte[] value)
        {
            writer.WriteVarBytes(value);
        }
    }
}
