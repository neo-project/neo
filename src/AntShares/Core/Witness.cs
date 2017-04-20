using AntShares.IO;
using AntShares.IO.Json;
using System.IO;

namespace AntShares.Core
{
    public class Witness : ISerializable
    {
        public byte[] InvocationScript;
        public byte[] VerificationScript;

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            InvocationScript = reader.ReadVarBytes();
            VerificationScript = reader.ReadVarBytes();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public JObject ToJson()
        {
            JObject json = new JObject();
            json["invocation"] = InvocationScript.ToHexString();
            json["verification"] = VerificationScript.ToHexString();
            return json;
        }
    }
}
