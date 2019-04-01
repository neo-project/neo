using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class PingPayload : ISerializable
    {
        public int Size => sizeof(uint);
        public uint currentHeight;

        public static PingPayload Create(uint height)
        {
            return new PingPayload
            {
                currentHeight= height
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            currentHeight = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(currentHeight);
        }
    }
}
