using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(ProtocolSettings.Default.Magic);
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static UInt256 XorUInt256List(this UInt256[] values)
        {
            if (values == null || values.Length == 0) return UInt256.Zero;
            byte[] result = values[0].ToArray();
            for (int i = 1; i < values.Length; i++)
            {
                var array = values[i].ToArray();
                for (int j = 0; j < UInt256.Length; j++)
                {
                    result[j] ^= array[j];
                }
            }
            return new UInt256(result);
        }
    }
}
