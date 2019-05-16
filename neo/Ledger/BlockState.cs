using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Ledger
{
    public class BlockState : StateBase, ICloneable<BlockState>
    {
        public TrimmedBlock TrimmedBlock;

        public override int Size => base.Size + TrimmedBlock.Size;

        BlockState ICloneable<BlockState>.Clone()
        {
            return new BlockState
            {
                TrimmedBlock = TrimmedBlock
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TrimmedBlock = reader.ReadSerializable<TrimmedBlock>();
        }

        void ICloneable<BlockState>.FromReplica(BlockState replica)
        {
            TrimmedBlock = replica.TrimmedBlock;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(TrimmedBlock);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["trimmed"] = TrimmedBlock.ToJson();
            return json;
        }
    }
}
