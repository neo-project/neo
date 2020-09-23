using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetBlocksPayload : ISerializable
    {
        public UInt256 HashStart;
        public short Count;

        public int Size => sizeof(short) + UInt256.Length;

        public static GetBlocksPayload Create(UInt256 hash_start, short count = -1)
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
            Count = reader.ReadInt16();
            if (Count < -1 || Count == 0) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(HashStart);
            writer.Write(Count);
        }
    }
}
