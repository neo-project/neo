using AntShares.IO;
using AntShares.IO.Json;
using System.IO;

namespace AntShares.Core.Scripts
{
    public class Script : ISerializable
    {
        public byte[] StackScript;
        public byte[] RedeemScript;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            StackScript = reader.ReadBytes((int)reader.ReadVarInt());
            RedeemScript = reader.ReadBytes((int)reader.ReadVarInt());
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarInt(StackScript.Length); writer.Write(StackScript);
            writer.WriteVarInt(RedeemScript.Length); writer.Write(RedeemScript);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["stack"] = StackScript.ToHexString();
            json["redeem"] = RedeemScript.ToHexString();
            return json;
        }
    }
}
