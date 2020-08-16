using Neo.IO.Json;

namespace Neo.IO.Serialization
{
    public class BooleanSerializer : Serializer<bool>
    {
        public override bool Deserialize(MemoryReader reader, SerializedAttribute attribute)
        {
            return reader.ReadBoolean();
        }

        public override bool FromJson(JObject json, SerializedAttribute attribute)
        {
            return json.AsBoolean();
        }

        public override void Serialize(MemoryWriter writer, bool value)
        {
            writer.Write(value);
        }

        public override JObject ToJson(bool value)
        {
            return value;
        }
    }
}
