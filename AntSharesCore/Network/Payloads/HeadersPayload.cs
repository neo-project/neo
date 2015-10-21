using AntShares.Core;
using AntShares.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Network.Payloads
{
    internal class HeadersPayload : ISerializable
    {
        public Block[] Headers;

        public static HeadersPayload Create(IEnumerable<Block> headers)
        {
            return new HeadersPayload
            {
                Headers = headers.ToArray()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Headers = reader.ReadSerializableArray<Block>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Headers);
        }
    }
}
