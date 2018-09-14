using Neo.IO;
using Neo.IO.Json;
using System.IO;
using System.Linq;

namespace Neo.Ledger
{
    public class HeaderHashList : StateBase, ICloneable<HeaderHashList>
    {
        public UInt256[] Hashes;

        public override int Size => base.Size + Hashes.GetVarSize();

        HeaderHashList ICloneable<HeaderHashList>.Clone()
        {
            return new HeaderHashList
            {
                Hashes = Hashes
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Hashes = reader.ReadSerializableArray<UInt256>();
        }

        void ICloneable<HeaderHashList>.FromReplica(HeaderHashList replica)
        {
            Hashes = replica.Hashes;
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
