using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class GetStateRootPayload : ISerializable
    {
        public uint Index;
        public int Size => sizeof(uint);

        public static GetStateRootPayload Create(uint index)
        {
            return new GetStateRootPayload
            {
                Index = index
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Index = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Index);
        }
    }
}
