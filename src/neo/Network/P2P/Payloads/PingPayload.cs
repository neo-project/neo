using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class PingPayload : ISerializable
    {
        public uint LastBlockIndex;
        public long LastStateIndex;
        public uint Timestamp;
        public uint Nonce;

        public int Size =>
            sizeof(uint) +  //LastBlockIndex
            sizeof(long) +  //LastStateIndex
            sizeof(uint) +  //Timestamp
            sizeof(uint);   //Nonce


        public static PingPayload Create(uint height, long state_height)
        {
            Random rand = new Random();
            return Create(height, state_height, (uint)rand.Next());
        }

        public static PingPayload Create(uint height, long state_height, uint nonce)
        {
            return new PingPayload
            {
                LastBlockIndex = height,
                LastStateIndex = state_height,
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                Nonce = nonce
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            LastBlockIndex = reader.ReadUInt32();
            LastStateIndex = reader.ReadInt64();
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(LastBlockIndex);
            writer.Write(LastStateIndex);
            writer.Write(Timestamp);
            writer.Write(Nonce);
        }
    }
}
