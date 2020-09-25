using System.IO;
using Neo.Cryptography;
using Neo.IO.Json;

namespace Neo.Models
{
    public static class Extensions
    {
        public static byte[] GetHashData(this IWitnessed witnessed, uint magic)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(magic);
            witnessed.SerializeUnsigned(writer);
            writer.Flush();
            return ms.ToArray();
        }

        public static UInt256 CalculateHash(this IWitnessed witnessed, uint magic)
        {
            return new UInt256(Crypto.Hash256(witnessed.GetHashData(magic)));
        }

        public static UInt160 ToScriptHash(this JObject value, byte addressVersion)
        {
            var addressOrScriptHash = value.AsString();

            return addressOrScriptHash.Length < 40 ?
                addressOrScriptHash.ToScriptHash(addressVersion) : UInt160.Parse(addressOrScriptHash);
        }
    }
}
