using Neo.IO;
using System.IO;
using System.Text;

namespace Neo.Oracle.Protocols.HTTPS
{
    public class OracleHTTPSRequest : OracleRequest
    {
        public enum HTTPMethod : byte
        {
            GET = 0x00
        }

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
        /// Constructor
        /// </summary>
        public OracleHTTPSRequest() : base(RequestType.HTTPS) { }

        /// <summary>
        /// Get hash data
        /// </summary>
        /// <returns>Hash data</returns>
        protected override byte[] GetHashData()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((byte)Type);
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
