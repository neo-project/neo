using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System.IO;
using System.Linq;

namespace Neo.Ledger
{
    public class TrimmedBlock : BlockBase
    {
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
                ConsensusData = ConsensusData,
                NextConsensus = NextConsensus,
                Witness = Witness,
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
                        ConsensusData = ConsensusData,
                        NextConsensus = NextConsensus,
                        Witness = Witness
                    };
                }
                return _header;
            }
        }

        public override int Size => base.Size + Hashes.GetVarSize();

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Hashes = reader.ReadSerializableArray<UInt256>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Hashes);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hashes"] = Hashes.Select(p => (JObject)p.ToString()).ToArray();
            return json;
        }
    }
}
