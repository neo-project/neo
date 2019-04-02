using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class PingPayload : ISerializable
    {
        public uint LastBlockIndex;
        public uint Timestamp;
        public uint Nonce;

        public int Size =>
            sizeof(uint) +  //LastBlockIndex
            sizeof(uint) +  //Timestamp
            sizeof(uint);   //Nonce


        public static PingPayload Create(uint height)
        {
            Random rand = new Random();
            return Create(height, (uint)rand.Next());
        }

        public static PingPayload Create(uint height, uint nonce)
        {
            return new PingPayload
            {
                LastBlockIndex = height,
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                Nonce = nonce
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            LastBlockIndex = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(LastBlockIndex);
            writer.Write(Timestamp);
            writer.Write(Nonce);
        }
    }
}
