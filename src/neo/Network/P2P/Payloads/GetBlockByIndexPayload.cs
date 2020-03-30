using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetBlockByIndexPayload : ISerializable
    {
        private const ushort MaxBlocksCount = 2000;
        public uint IndexStart;
        public ushort Count;

        public int Size => sizeof(uint) + sizeof(ushort);

        public static GetBlockByIndexPayload Create(uint index_start, ushort count)
        {
            return new GetBlockByIndexPayload
            {
                IndexStart = index_start,
                Count = count
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            IndexStart = reader.ReadUInt32();
            Count = reader.ReadUInt16();
            if (Count == 0 || Count > MaxBlocksCount) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(IndexStart);
            writer.Write(Count);
        }
    }
}
