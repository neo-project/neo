using Neo.IO.Json;
using System;

namespace Neo.IO.Serialization
{
    public class StringSerializer : Serializer<string>
    {
        public override string Deserialize(MemoryReader reader, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            return reader.ReadVarString(max);
        }

        public override string FromJson(JObject json, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            string result = json.AsString();
            if (Utility.StrictUTF8.GetByteCount(result) > max) throw new FormatException();
            return result;
        }

        public override void Serialize(MemoryWriter writer, string value)
        {
            writer.WriteVarString(value);
        }

        public override JObject ToJson(string value)
        {
            return value;
        }
    }
}
