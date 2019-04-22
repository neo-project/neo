using Neo.Network.P2P.Payloads;
using System.IO;
using System.IO.Compression;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static byte[] UncompressGzip(this byte[] data)
        {
            using (var output = new MemoryStream())
            using (var input = new MemoryStream(data))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            {
                int nRead;
                byte[] buffer = new byte[1024];

                while ((nRead = gzip.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, nRead);
                }

                return output.ToArray();
            }
        }

        public static byte[] CompressGzip(this byte[] data)
        {
            using (var stream = new MemoryStream())
            {
                using (var gzip = new GZipStream(stream, CompressionLevel.Optimal, true))
                {
                    gzip.Write(data, 0, data.Length);
                    gzip.Flush();
                }

                return stream.ToArray();
            }
        }

        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}
