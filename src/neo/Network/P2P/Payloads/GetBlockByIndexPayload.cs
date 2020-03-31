using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetBlockByIndexPayload : ISerializable
    {
        public uint IndexStart;
        public short Count;

        public int Size => sizeof(uint) + sizeof(short);

        public static GetBlockByIndexPayload Create(uint index_start, short count = -1)
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
            Count = reader.ReadInt16();
            if (Count < -1 || Count == 0 || Count > HeadersPayload.MaxHeadersCount)
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(IndexStart);
            writer.Write(Count);
        }
    }
}
