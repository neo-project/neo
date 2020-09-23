using System;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Block : ISignable
    {
        public const int MaxContentsPerBlock = ushort.MaxValue;
        public const int MaxTransactionsPerBlock = MaxContentsPerBlock - 1;

        public Header Header;
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
            + BinaryFormat.GetVarSize(Transactions.Length + 1)  // Content count
            + ConsensusData.Size                                // ConsensusData
            + Transactions.Sum(p => p.Size);                    // Transactions

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Header = reader.ReadSerializable<Header>();
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
            for (int i = 0; i < Transactions.Length; i++)
            {
                writer.Write(Transactions[i]);
            }
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            ((ISignable)Header).SerializeUnsigned(writer);
        }

        Witness[] ISignable.Witnesses => ((ISignable)Header).Witnesses;
        
        public JObject ToJson(uint magic, byte addressVersion)
        {
            JObject json = Header.ToJson(magic, addressVersion);
            json["consensusdata"] = ConsensusData.ToJson();
            json["tx"] = Transactions.Select(p => p.ToJson(magic, addressVersion)).ToArray();
            return json;
        }
        
        public static Block FromJson(JObject json, byte? addressVersion)
        {
            Block block = new Block();
            block.Header = Header.FromJson(json, addressVersion);
            block.ConsensusData = ConsensusData.FromJson(json["consensusdata"]);
            block.Transactions = ((JArray)json["tx"]).Select(p => Transaction.FromJson(p, addressVersion)).ToArray();
            return block;
        }
    }
}
