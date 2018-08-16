using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Ledger
{
    public class HashIndexState : StateBase, ICloneable<HashIndexState>
    {
        public UInt256 Hash = UInt256.Zero;
        public uint Index = uint.MaxValue;

        public override int Size => base.Size + Hash.Size + sizeof(uint);

        HashIndexState ICloneable<HashIndexState>.Clone()
        {
            return new HashIndexState
            {
                Hash = Hash,
                Index = Index
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Hash = reader.ReadSerializable<UInt256>();
            Index = reader.ReadUInt32();
        }

        void ICloneable<HashIndexState>.FromReplica(HashIndexState replica)
        {
            Hash = replica.Hash;
            Index = replica.Index;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Hash);
            writer.Write(Index);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hash"] = Hash.ToString();
            json["index"] = Index;
            return json;
        }
    }
}
