using AntShares.Core;
using AntShares.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Network.Payloads
{
    internal class HeadersPayload : ISerializable
    {
        public BlockHeader[] Headers;

        public static HeadersPayload Create(IEnumerable<BlockHeader> headers)
        {
            return new HeadersPayload
            {
                Headers = headers.ToArray()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.Headers = reader.ReadSerializableArray<BlockHeader>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Headers);
        }
    }
}
