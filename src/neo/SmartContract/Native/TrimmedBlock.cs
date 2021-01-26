using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;

namespace Neo.SmartContract.Native
{
    public class TrimmedBlock : BlockBase, IInteroperable
    {
        public UInt256[] Hashes;
        public ConsensusData ConsensusData;

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

        public override int Size => base.Size
            + Hashes.GetVarSize()           //Hashes
            + (ConsensusData?.Size ?? 0);   //ConsensusData

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Hashes = reader.ReadSerializableArray<UInt256>(Block.MaxContentsPerBlock);
            if (Hashes.Length > 0)
                ConsensusData = reader.ReadSerializable<ConsensusData>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Hashes);
            if (Hashes.Length > 0)
                writer.Write(ConsensusData);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["consensusdata"] = ConsensusData?.ToJson();
            json["hashes"] = Hashes.Select(p => (JObject)p.ToString()).ToArray();
            return json;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        StackItem IInteroperable.ToStackItem(ReferenceCounter referenceCounter)
        {
            return new VM.Types.Array(referenceCounter, new StackItem[]
            {
                // Computed properties
                Hash.ToArray(),

                // BlockBase properties
                Version,
                PrevHash.ToArray(),
                MerkleRoot.ToArray(),
                Timestamp,
                Index,
                NextConsensus.ToArray(),

                // Block properties
                Hashes.Length - 1
            });
        }
    }
}
