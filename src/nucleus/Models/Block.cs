using System;
using System.IO;
using System.Linq;
using Neo.IO;

namespace Neo.Models
{
    public class Block : ISerializable
    {
        public const int MaxContentsPerBlock = ushort.MaxValue;
        public const int MaxTransactionsPerBlock = MaxContentsPerBlock - 1;

        public BlockHeader Header;
        public ConsensusData ConsensusData;
        public Transaction[] Transactions;

        public uint Version => Header.Version;
        public UInt256 PrevHash => Header.PrevHash;
        public UInt256 MerkleRoot => Header.MerkleRoot;
        public ulong Timestamp => Header.Timestamp;
        public uint Index => Header.Index;
        public UInt160 NextConsensus => Header.NextConsensus;
        public Witness Witness => Header.Witness;

        int ISerializable.Size => 
            Header.Size                                         // Header
            + IO.Extensions.GetVarSize(Transactions.Length + 1) // Content count
            + ConsensusData.Size                                // ConsensusData
            + Transactions.Sum(p => p.Size);                    // Transactions

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Header = reader.ReadSerializable<BlockHeader>();
            int count = (int)reader.ReadVarInt(MaxContentsPerBlock);
            if (count == 0) throw new FormatException();
            ConsensusData = reader.ReadSerializable<ConsensusData>();
            Transactions = new Transaction[count - 1];
            for (int i = 0; i < Transactions.Length; i++)
                Transactions[i] = reader.ReadSerializable<Transaction>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Header);
            writer.WriteVarInt(Transactions.Length + 1);
            writer.Write(ConsensusData);
            foreach (Transaction tx in Transactions)
                writer.Write(tx);
        }
    }
}
