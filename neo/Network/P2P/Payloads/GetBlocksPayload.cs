using System.IO;
using Neo.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetBlocksPayload : ISerializable
    {
        public UInt256 HashStart;
        public int Count;

        public int Size => 32 + sizeof(int);

        public static GetBlocksPayload Create(UInt256 hash_start, int count)
        {
            return new GetBlocksPayload
            {
                HashStart = hash_start,
                Count = count
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            HashStart = reader.ReadSerializable<UInt256>();
            Count = reader.ReadInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(HashStart);
            writer.Write(Count);
        }
    }
}
