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
        public const int MaxContentsPerBlock = ushort.MaxValue;
        public const int MaxTransactionsPerBlock = MaxContentsPerBlock - 1;

        public IBlockContent[] Contents;

        public ConsensusData ConsensusData => (ConsensusData)Contents[0];

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

        public override int Size => base.Size + Contents.GetVarSize();

        public IEnumerable<Transaction> Transactions => Contents.Skip(1).Cast<Transaction>();

        public int TransactionCount => Contents.Length - 1;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Contents = new IBlockContent[reader.ReadVarInt(MaxContentsPerBlock)];
            Contents[0] = reader.ReadSerializable<ConsensusData>();
            for (int i = 1; i < Contents.Length; i++)
                Contents[i] = reader.ReadSerializable<Transaction>();
            if (Contents.Select(p => p.Hash).Distinct().Count() != Contents.Length)
                throw new FormatException();
            if (MerkleTree.ComputeRoot(Contents.Select(p => p.Hash).ToArray()) != MerkleRoot)
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
            MerkleRoot = MerkleTree.ComputeRoot(Contents.Select(p => p.Hash).ToArray());
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Contents);
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
                Hashes = Contents.Select(p => p.Hash).ToArray(),
                ConsensusData = ConsensusData
            };
        }
    }
}
