using System.IO;

namespace Neo.IO.Serialization
{
    public class StringSerializer : Serializer<string>
    {
        public override string Deserialize(BinaryReader reader, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            return reader.ReadVarString(max);
        }

        public override void Serialize(BinaryWriter writer, string value)
        {
            writer.WriteVarString(value);
        }
    }
}
