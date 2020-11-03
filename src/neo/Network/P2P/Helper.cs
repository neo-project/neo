using Neo.Cryptography;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            return GetHashData(verifiable, ProtocolSettings.Default.Magic);
        }

        public static byte[] GetHashData(this IVerifiable verifiable, uint magic)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(magic);
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static UInt256 CalculateHash(this IVerifiable verifiable)
        {
            return new UInt256(Crypto.Hash256(verifiable.GetHashData(ProtocolSettings.Default.Magic)));
        }

        public static UInt256 CalculateHash(this IVerifiable verifiable, uint magic)
        {
            return new UInt256(Crypto.Hash256(verifiable.GetHashData(magic)));
        }
    }
}
