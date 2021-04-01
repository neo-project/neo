using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetStateRootsPayload : ISerializable
    {
        public uint StartIndex;
        public uint Count;

        public int Size => sizeof(uint) + sizeof(uint);

        public static GetStateRootsPayload Create(uint start_index, uint count)
        {
            return new GetStateRootsPayload
            {
                StartIndex = start_index,
                Count = Math.Min(count, StateRootsPayload.MaxStateRootsCount),
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            StartIndex = reader.ReadUInt32();
            Count = reader.ReadUInt32();
            if (Count == 0) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StartIndex);
            writer.Write(Count);
        }
    }
}
