using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Block : BlockBase, IInventory, IEquatable<Block>
    {
        public const int MaxContentsPerBlock = ushort.MaxValue;
        public const int MaxTransactionsPerBlock = MaxContentsPerBlock - 1;

        public ConsensusData ConsensusData;
        public Transaction[] Transactions;

        private Header _header = null;
        public Header Header
        {
            get
            {
                if (_header == null)
                {
                    _header = new Header
                    {
                        PrevHash = PrevHash,
                        MerkleRoot = MerkleRoot,
                        Timestamp = Timestamp,
                        Index = Index,
                        NextConsensus = NextConsensus,
                        Witness = Witness
                    };
                }
                return _header;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.Block;

        public override int Size => base.Size
            + IO.Helper.GetVarSize(Transactions.Length + 1) //Count
            + ConsensusData.Size                            //ConsensusData
            + Transactions.Sum(p => p.Size);                //Transactions

        public static UInt256 CalculateMerkleRoot(UInt256 consensusDataHash, params UInt256[] transactionHashes)
        {
            List<UInt256> hashes = new List<UInt256>(transactionHashes.Length + 1) { consensusDataHash };
            hashes.AddRange(transactionHashes);
            return MerkleTree.ComputeRoot(hashes);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            int count = (int)reader.ReadVarInt(MaxContentsPerBlock);
            if (count == 0) throw new FormatException();
            ConsensusData = reader.ReadSerializable<ConsensusData>();
            Transactions = new Transaction[count - 1];
            for (int i = 0; i < Transactions.Length; i++)
                Transactions[i] = reader.ReadSerializable<Transaction>();
            if (Transactions.Distinct().Count() != Transactions.Length)
                throw new FormatException();
            if (CalculateMerkleRoot(ConsensusData.Hash, Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                throw new FormatException();
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

        public void RebuildMerkleRoot()
        {
            MerkleRoot = CalculateMerkleRoot(ConsensusData.Hash, Transactions.Select(p => p.Hash).ToArray());
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarInt(Transactions.Length + 1);
            writer.Write(ConsensusData);
            foreach (Transaction tx in Transactions)
                writer.Write(tx);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["consensus_data"] = ConsensusData.ToJson();
            json["tx"] = Transactions.Select(p => p.ToJson()).ToArray();
            return json;
        }

        public new static Block FromJson(JObject json)
        {
            Block block = new Block();
            BlockBase blockBase = block;
            blockBase.FromJson(json);
            block.ConsensusData = ConsensusData.FromJson(json["consensus_data"]);
            block.Transactions = ((JArray)json["tx"]).Select(p => Transaction.FromJson(p)).ToArray();
            return block;
        }

        public TrimmedBlock Trim()
        {
            return new TrimmedBlock
            {
                Version = Version,
                PrevHash = PrevHash,
                MerkleRoot = MerkleRoot,
                Timestamp = Timestamp,
                Index = Index,
                NextConsensus = NextConsensus,
                Witness = Witness,
                Hashes = new[] { ConsensusData.Hash }.Concat(Transactions.Select(p => p.Hash)).ToArray(),
                ConsensusData = ConsensusData
            };
        }
    }
}
