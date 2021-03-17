using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to respond to <see cref="MessageCommand.GetHeaders"/> messages.
    /// </summary>
    public class HeadersPayload : ISerializable
    {
        /// <summary>
        /// Indicates the maximum number of headers sent each time.
        /// </summary>
        public const int MaxHeadersCount = 2000;

        /// <summary>
        /// The list of headers.
        /// </summary>
        public Header[] Headers;

        public int Size => Headers.GetVarSize();

        /// <summary>
        /// Creates a new instance of the <see cref="HeadersPayload"/> class.
        /// </summary>
        /// <param name="headers">The list of headers.</param>
        /// <returns>The created payload.</returns>
        public static HeadersPayload Create(params Header[] headers)
        {
            return new HeadersPayload
            {
                Headers = headers
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Headers = reader.ReadSerializableArray<Header>(MaxHeadersCount);
            if (Headers.Length == 0) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Headers);
        }
    }
}
