using System.IO;
using Neo.Cryptography;

namespace Neo.Models
{
    public static class Extensions
    {
        public static byte[] GetHashData(this ISignable signable, uint magic)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(magic);
            signable.SerializeUnsigned(writer);
            writer.Flush();
            return ms.ToArray();
        }

        public static UInt256 CalculateHash(this ISignable signable, uint magic)
        {
            return new UInt256(Crypto.Hash256(signable.GetHashData(magic)));
        }
    }
}
