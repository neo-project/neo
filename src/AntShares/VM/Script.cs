using AntShares.IO;
using AntShares.IO.Json;
using System.IO;

namespace AntShares.VM
{
    /// <summary>
    /// 脚本
    /// </summary>
    public class Script : ISerializable
    {
        public byte[] StackScript;
        public byte[] RedeemScript;

        public int Size => StackScript.Length.GetVarSize() + StackScript.Length + RedeemScript.Length.GetVarSize() + RedeemScript.Length;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            StackScript = reader.ReadVarBytes();
            RedeemScript = reader.ReadVarBytes();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(StackScript);
            writer.WriteVarBytes(RedeemScript);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public JObject ToJson()
        {
            JObject json = new JObject();
            json["stack"] = StackScript.ToHexString();
            json["redeem"] = RedeemScript.ToHexString();
            return json;
        }
    }
}
