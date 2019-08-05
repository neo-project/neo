using Neo.IO;
using Neo.IO.Json;
using System.IO;
using Neo.Cryptography.ECC;

namespace Neo.Cryptography
{
    public class PublicKey : ISerializable
    {
        /// <summary>
        /// Standard elliptic curve: secp256r1
        /// </summary>
        public ECPoint Data;

        public int Size => Data.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Data = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Data.ToArray());
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["data"] = Data.ToArray().ToHexString();
            return json;
        }

        public static PublicKey FromJson(JObject json)
        {
            PublicKey pubkey = new PublicKey();
            using (MemoryStream ms = new MemoryStream(json["data"].AsString().HexToBytes(), false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                pubkey.Data = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            }
            return pubkey;
        }
    }
}
