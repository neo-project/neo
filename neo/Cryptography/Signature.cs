using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Cryptography
{
    public class Signature : ISerializable
    {
        /// <summary>
        /// Standard size for secp256r1: 64 bytes 
        /// </summary>
        public byte[] Data;

        public int Size => Data.Length;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Data = reader.ReadBytes(64);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Data);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["data"] = Data.ToHexString();
            return json;
        }

        public static Signature FromJson(JObject json)
        {
            Signature signature = new Signature();
            signature.Data = json["data"].AsString().HexToBytes();
            return signature;
        }
    }
}
