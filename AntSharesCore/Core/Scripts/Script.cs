using AntShares.IO;
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
    }
}
