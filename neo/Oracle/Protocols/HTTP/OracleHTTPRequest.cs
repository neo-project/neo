using Neo.IO;
using System.IO;
using System.Text;

namespace Neo.Oracle.Protocols.HTTP
{
    public class OracleHTTPRequest : OracleRequest
    {
        public enum HTTPMethod : byte
        {
            GET = 0,
            POST = 1,
            PUT = 2,
            DELETE = 3
        }

        /// <summary>
        /// Version Major
        /// </summary>
        public byte VersionMajor { get; set; } = 1;

        /// <summary>
        /// Version Minor
        /// </summary>
        public byte VersionMinor { get; set; } = 1;

        /// <summary>
        /// HTTP Methods
        /// </summary>
        public HTTPMethod Method { get; set; }

        /// <summary>
        /// URL
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// Filter
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// Body
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// Get hash data
        /// </summary>
        /// <returns>Hash data</returns>
        protected override byte[] GetHashData()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((byte)RequestType.HTTP);
                writer.Write(VersionMajor);
                writer.Write(VersionMinor);
                writer.Write((byte)Method);
                writer.WriteVarString(URL);
                writer.WriteVarString(Filter);
                if (Body != null) writer.WriteVarBytes(Body);
                writer.Flush();

                return stream.ToArray();
            }
        }
    }
}
