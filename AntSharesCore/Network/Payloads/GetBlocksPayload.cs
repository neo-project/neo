using AntShares.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Network.Payloads
{
    internal class GetBlocksPayload : ISerializable
    {
        public UInt256[] HashStart;
        public UInt256 HashStop;

        public static GetBlocksPayload Create(IEnumerable<UInt256> hash_start, UInt256 hash_stop = null)
        {
            return new GetBlocksPayload
            {
                HashStart = hash_start.ToArray(),
                HashStop = hash_stop ?? UInt256.Zero
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            HashStart = reader.ReadSerializableArray<UInt256>();
            HashStop = reader.ReadSerializable<UInt256>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(HashStart);
            writer.Write(HashStop);
        }
    }
}
