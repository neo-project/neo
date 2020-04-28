using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.SmartContract.NNS
{
    public class RecordInfo : ISerializable
    {
        public RecordType RecordType { set; get; }
        public string Text { get; set; }

        public int Size => 1 + Text.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            RecordType = (RecordType)reader.ReadByte();
            Text = reader.ReadVarString(1024);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            writer.WriteVarString(Text);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["text"] = Text;
            json["recordType"] = RecordType.ToString();
            return json;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }

    public enum RecordType : byte
    {
        A = 0x00,
        CNAME = 0x01,
        TXT = 0x02,
        NS = 0x03,
        ERROR = 0x04
    }
}
