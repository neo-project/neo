using AntShares.IO;
using System.IO;

namespace AntShares.Network.Payloads
{
    internal class GetBlocksPayload : ISerializable
    {
        public UInt256[] HashStart;
        public UInt256 HashStop;

        public static GetBlocksPayload Create(params UInt256[] hash_start)
        {
            return new GetBlocksPayload
            {
                HashStart = hash_start,
                HashStop = UInt256.Zero
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
