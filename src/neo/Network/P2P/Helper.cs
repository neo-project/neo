using Neo.Cryptography;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static UInt256 CalculateHash(this IVerifiable verifiable)
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            verifiable.SerializeUnsigned(writer);
            writer.Flush();
            return new UInt256(ms.ToArray().Sha256());
        }

        public static byte[] GetSignData(this IVerifiable verifiable, uint magic)
        {
            UInt256 hash = verifiable.CalculateHash();
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            writer.Write(magic);
            writer.Write(hash);
            writer.Flush();
            return ms.ToArray();
        }
    }
}
