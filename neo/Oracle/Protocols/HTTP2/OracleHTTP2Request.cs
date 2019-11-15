using System.IO;
using System.Text;

namespace Neo.Oracle.Protocols.HTTP2
{
    public class OracleHTTP2Request : OracleRequest
    {
        /// <summary>
        /// Get hash data
        /// </summary>
        /// <returns>Hash data</returns>
        protected override byte[] GetHashData()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((byte)RequestType.HTTP2);
                // TODO
                writer.Flush();

                return stream.ToArray();
            }
        }
    }
}
