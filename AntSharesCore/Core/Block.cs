using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class Block : ISerializable
    {
        public const UInt32 Version = 0;
        public UInt256 PrevBlock;
        public UInt256 MerkleRoot;
        public UInt32 Timestamp;
        public UInt32 Bits;
        public UInt32 Nonce;
        public UInt160 Miner;
        public byte[] Script;
        public Transaction[] Transactions;

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Version)
                throw new FormatException();
            this.PrevBlock = reader.ReadSerializable<UInt256>();
            this.MerkleRoot = reader.ReadSerializable<UInt256>();
            this.Timestamp = reader.ReadUInt32();
            this.Bits = reader.ReadUInt32();
            this.Nonce = reader.ReadUInt32();
            this.Miner = reader.ReadSerializable<UInt160>();
            this.Script = reader.ReadBytes((int)reader.ReadVarInt());
            this.Transactions = new Transaction[reader.ReadVarInt()];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = Transaction.DeserializeFrom(reader);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevBlock);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Bits);
            writer.Write(Nonce);
            writer.Write(Miner);
            writer.WriteVarInt(Script.Length); writer.Write(Script);
            writer.Write(Transactions);
        }
    }
}
