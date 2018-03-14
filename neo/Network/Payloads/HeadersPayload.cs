using Neo.Core;
using Neo.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.Payloads
{
    public class HeadersPayload : ISerializable
    {
        public Header[] Headers;

        public int Size => Headers.GetVarSize();

        public static HeadersPayload Create(IEnumerable<Header> headers)
        {
            return new HeadersPayload
            {
                Headers = headers.ToArray()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Headers = reader.ReadSerializableArray<Header>(2000);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Headers);
        }
    }
}
