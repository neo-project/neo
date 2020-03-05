using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetStateRootsPayload : ISerializable
    {
        public uint StartIndex;
        public uint Count;

        public int Size => sizeof(uint) + sizeof(uint);

        public static GetStateRootsPayload Create(uint start_index, uint count = 100)
        {
            return new GetStateRootsPayload
            {
                StartIndex = start_index,
                Count = count,
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            StartIndex = reader.ReadUInt32();
            Count = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StartIndex);
            writer.Write(Count);
        }
    }
}
