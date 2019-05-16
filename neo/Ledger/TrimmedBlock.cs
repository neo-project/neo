using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Linq;

namespace Neo.Ledger
{
    public class TrimmedBlock : BlockBase, ICloneable<TrimmedBlock>
    {
        public ConsensusData ConsensusData;
        public UInt256[] Hashes;

        public bool IsBlock => Hashes.Length > 0;

        public Block GetBlock(DataCache<UInt256, TransactionState> cache)
        {
            return new Block
            {
                Version = Version,
                PrevHash = PrevHash,
                MerkleRoot = MerkleRoot,
                Timestamp = Timestamp,
                Index = Index,
                NextConsensus = NextConsensus,
                Witness = Witness,
                ConsensusData = ConsensusData,
                Transactions = Hashes.Select(p => cache[p].Transaction).ToArray()
            };
        }

        private Header _header = null;
        public Header Header
        {
            get
            {
                if (_header == null)
                {
                    _header = new Header
                    {
                        Version = Version,
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

        public override int Size => base.Size + ConsensusData.Size + Hashes.GetVarSize();

        TrimmedBlock ICloneable<TrimmedBlock>.Clone()
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
                Hashes = Hashes,
                _header = _header
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ConsensusData = reader.ReadSerializable<ConsensusData>();
            Hashes = reader.ReadSerializableArray<UInt256>(Block.MaxTransactionsPerBlock);
        }

        void ICloneable<TrimmedBlock>.FromReplica(TrimmedBlock replica)
        {
            Version = replica.Version;
            PrevHash = replica.PrevHash;
            MerkleRoot = replica.MerkleRoot;
            Timestamp = replica.Timestamp;
            Index = replica.Index;
            NextConsensus = replica.NextConsensus;
            Witness = replica.Witness;
            ConsensusData = replica.ConsensusData;
            Hashes = replica.Hashes;
            _header = replica._header;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ConsensusData);
            writer.Write(Hashes);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["consensus_data"] = ConsensusData.ToJson();
            json["hashes"] = Hashes.Select(p => (JObject)p.ToString()).ToArray();
            return json;
        }
    }
}
