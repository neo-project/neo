using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Block : BlockBase, IEquatable<Block>
    {
        public const int MaxContentsPerBlock = ushort.MaxValue;
        public const int MaxTransactionsPerBlock = MaxContentsPerBlock - 1;

        public ConsensusData ConsensusData;
        public Transaction[] Transactions;

        public Block(uint magic) : base(magic)
        {
        }

        private Lazy<Header> header  = null;
        public Header Header
        {
            get
            {
                header ??= new Lazy<Header>(() => new Header(magic)
                    {
                        PrevHash = PrevHash,
                        MerkleRoot = MerkleRoot,
                        Timestamp = Timestamp,
                        Index = Index,
                        NextConsensus = NextConsensus,
                        Witness = Witness
                    });
                return header.Value;
            }
        }

        public static UInt256 CalculateMerkleRoot(UInt256 consensusDataHash, IEnumerable<UInt256> transactionHashes)
        {
            return MerkleTree.ComputeRoot(transactionHashes.Prepend(consensusDataHash).ToArray());
        }

        public override int Size => base.Size
            + BinaryFormat.GetVarSize(Transactions.Length + 1)  // Content count
            + ConsensusData.Size                                // ConsensusData
            + Transactions.Sum(p => p.Size);                    // Transactions

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            int count = (int)reader.ReadVarInt(MaxContentsPerBlock);
            if (count == 0) throw new FormatException();
            ConsensusData = reader.ReadSerializable<ConsensusData>();
            Transactions = new Transaction[count - 1];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = reader.ReadSerializable(() => new Transaction(magic));
            }

            if (Transactions.Distinct().Count() != Transactions.Length) throw new FormatException();
            if (CalculateMerkleRoot(ConsensusData.Hash, Transactions.Select(p => p.Hash)) != MerkleRoot) throw new FormatException();
        }

        public bool Equals(Block other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Block);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarInt(Transactions.Length + 1);
            writer.Write(ConsensusData);
            for (int i = 0; i < Transactions.Length; i++)
            {
                writer.Write(Transactions[i]);
            }
        }
    }
}
