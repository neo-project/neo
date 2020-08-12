using System;

namespace Neo.IO.Serialization
{
    public class MemorySerializer : Serializer<ReadOnlyMemory<byte>>
    {
        public override ReadOnlyMemory<byte> Deserialize(MemoryReader reader, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            return reader.ReadVarBytes(max);
        }

        public override void Serialize(MemoryWriter writer, ReadOnlyMemory<byte> value)
        {
            writer.WriteVarBytes(value.Span);
        }
    }
}
