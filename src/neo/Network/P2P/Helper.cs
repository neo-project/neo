using Neo.Cryptography;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            verifiable.SerializeUnsigned(writer);
            writer.Flush();
            return ms.ToArray();
        }

        public static UInt256 CalculateHash(this IVerifiable verifiable)
        {
            return new UInt256(Crypto.Hash256(verifiable.GetHashData()));
        }
    }
}
