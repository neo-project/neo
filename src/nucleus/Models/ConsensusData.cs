using System.Globalization;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class ConsensusData : ISerializable
    {
        public byte PrimaryIndex;
        public ulong Nonce;

        public int Size => sizeof(byte) + sizeof(ulong);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            PrimaryIndex = reader.ReadByte();
            Nonce = reader.ReadUInt64();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(PrimaryIndex);
            writer.Write(Nonce);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["primary"] = PrimaryIndex;
            json["nonce"] = Nonce.ToString("x16");
            return json;
        }
                
        public static ConsensusData FromJson(JObject json)
        {
            return new ConsensusData
            {
                PrimaryIndex = (byte)json["primary"].AsNumber(),
                Nonce = ulong.Parse(json["nonce"].AsString(), NumberStyles.HexNumber)
            };
        }

    }
}
