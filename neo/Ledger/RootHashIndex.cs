using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Ledger
{
    public class RootHashIndex : StateBase, ICloneable<RootHashIndex>
    {
        public UInt256 Hash = UInt256.Zero;
        public long Index = -1;

        public override int Size => base.Size + Hash.Size + sizeof(long);

        RootHashIndex ICloneable<RootHashIndex>.Clone()
        {
            return new RootHashIndex
            {
                Hash = Hash,
                Index = Index
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Hash = reader.ReadSerializable<UInt256>();
            Index = reader.ReadInt64();
        }

        void ICloneable<RootHashIndex>.FromReplica(RootHashIndex replica)
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
