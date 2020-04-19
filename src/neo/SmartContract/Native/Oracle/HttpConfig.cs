using Neo.IO;
using System.IO;

namespace Neo.SmartContract.Native.Oracle
{
    public class HttpConfig : ISerializable
    {
        public const string Key = "HttpConfig";

        public int TimeOut { get; set; } = 5000;
        public string[] AllowedFormats { get; set; } = new string[]
        {
            "application/json"
        };

        public int Size => sizeof(int) + AllowedFormats.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            TimeOut = reader.ReadInt32();
            AllowedFormats = new string[reader.ReadVarInt(byte.MaxValue)];
            for (int x = 0; x < AllowedFormats.Length; x++)
            {
                AllowedFormats[x] = reader.ReadVarString(byte.MaxValue);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TimeOut);
            writer.WriteVarInt(AllowedFormats.Length);
            foreach (var entry in AllowedFormats)
            {
                writer.WriteVarString(entry);
            }
        }
    }
}
