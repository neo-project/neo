namespace Neo.IO.Serialization
{
    public class ByteArraySerializer : Serializer<byte[]>
    {
        public override byte[] Deserialize(MemoryReader reader, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            return reader.ReadVarBytes(max).ToArray();
        }

        public override void Serialize(MemoryWriter writer, byte[] value)
        {
            writer.WriteVarBytes(value);
        }
    }
}
