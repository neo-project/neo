using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Block : BlockBase, IInventory, IEquatable<Block>
    {
        public const int MaxTransactionsPerBlock = ushort.MaxValue;

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

        public override int Size => base.Size + ConsensusData.Size + Transactions.GetVarSize();

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ConsensusData = reader.ReadSerializable<ConsensusData>();
            Transactions = reader.ReadSerializableArray<Transaction>(MaxTransactionsPerBlock);
            if (Transactions.Length == 0) throw new FormatException();
            if (Transactions.Distinct().Count() != Transactions.Length)
                throw new FormatException();
            if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
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
            List<UInt256> hashes = new List<UInt256>(Transactions.Length + 1)
            {
                ConsensusData.Hash
            };
            hashes.AddRange(Transactions.Select(p => p.Hash));
            MerkleRoot = MerkleTree.ComputeRoot(hashes);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ConsensusData);
            writer.Write(Transactions);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["consensus_data"] = ConsensusData.ToJson();
            json["tx"] = Transactions.Select(p => p.ToJson()).ToArray();
            return json;
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
                ConsensusData = ConsensusData,
                Hashes = Transactions.Select(p => p.Hash).ToArray()
            };
        }
    }
}
