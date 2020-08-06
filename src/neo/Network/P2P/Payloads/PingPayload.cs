using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class PingPayload : ISerializable
    {
        public uint LastBlockIndex;
        public UInt256 LastBlockHash;
        public uint Timestamp;
        public uint Nonce;

        public int Size =>
            sizeof(uint) +      //LastBlockIndex
            UInt256.Length +    //LastBlockHash
            sizeof(uint) +      //Timestamp
            sizeof(uint);       //Nonce

        public static PingPayload Create(Block header)
        {
            Random rand = new Random();
            return Create(header.Index, header.Hash, (uint)rand.Next());
        }

        public static PingPayload Create(uint height, UInt256 hash, uint nonce)
        {
            return new PingPayload
            {
                LastBlockIndex = height,
                LastBlockHash = hash,
                Timestamp = DateTime.UtcNow.ToTimestamp(),
                Nonce = nonce
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            LastBlockIndex = reader.ReadUInt32();
            LastBlockHash = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(LastBlockIndex);
            writer.Write(LastBlockHash);
            writer.Write(Timestamp);
            writer.Write(Nonce);
        }
    }
}
