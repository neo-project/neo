using Neo.IO;
using System;
using System.IO;
using System.Text;

namespace Neo.Oracle.Protocols.Https
{
    public class OracleHttpsRequest : OracleRequest
    {
        /// <summary>
        /// HTTP Methods
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public override OracleRequestType Type => OracleRequestType.HTTPS;

        /// <summary>
        /// URL
        /// </summary>
        public Uri URL { get; set; }

        /// <summary>
        /// Filter
        /// </summary>
        public string Filter { get; set; }

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
                writer.WriteVarString(URL.ToString());
                writer.WriteVarString(Filter);
                writer.Flush();

                return stream.ToArray();
            }
        }
    }
}
