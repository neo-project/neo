using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetStateRootsPayload : ISerializable
    {
        public uint StartIndex;
        public uint EndIndex;

        public int Size => sizeof(uint) + sizeof(uint);

        public static GetStateRootsPayload Create(uint start_index, uint end_index)
        {
            return new GetStateRootsPayload
            {
                StartIndex = start_index,
                EndIndex = end_index,
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            StartIndex = reader.ReadUInt32();
            EndIndex = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StartIndex);
            writer.Write(EndIndex);
        }
    }
}
