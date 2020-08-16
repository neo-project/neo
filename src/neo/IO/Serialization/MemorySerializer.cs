using Neo.IO.Json;
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

        public override ReadOnlyMemory<byte> FromJson(JObject json, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            byte[] result = Convert.FromBase64String(json.AsString());
            if (result.Length > max) throw new FormatException();
            return result;
        }

        public override void Serialize(MemoryWriter writer, ReadOnlyMemory<byte> value)
        {
            writer.WriteVarBytes(value.Span);
        }

        public override JObject ToJson(ReadOnlyMemory<byte> value)
        {
            return Convert.ToBase64String(value.Span);
        }
    }
}
