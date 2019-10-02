using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetFullBlocksPayload : ISerializable
    {
        public const ushort MaxBlocksCount = 500;
        public uint IndexStart;
        public short Count;

        public int Size => sizeof(uint) + sizeof(short);

        public static GetFullBlocksPayload Create(uint index_start, short count = -1)
        {
            return new GetFullBlocksPayload
            {
                IndexStart = index_start,
                Count = count
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            IndexStart = reader.ReadUInt32();
            if (IndexStart == 0) throw new FormatException();
            Count = reader.ReadInt16();
            if (Count <= 0 || Count > MaxBlocksCount) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(IndexStart);
            writer.Write(Count);
        }
    }
}