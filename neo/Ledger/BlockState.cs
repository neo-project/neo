using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Ledger
{
    public class BlockState : StateBase, ICloneable<BlockState>
    {
        public long SystemFeeAmount;
        public TrimmedBlock TrimmedBlock;

        public override int Size => base.Size + sizeof(long) + TrimmedBlock.Size;

        BlockState ICloneable<BlockState>.Clone()
        {
            return new BlockState
            {
                SystemFeeAmount = SystemFeeAmount,
                TrimmedBlock = TrimmedBlock
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            SystemFeeAmount = reader.ReadInt64();
            TrimmedBlock = reader.ReadSerializable<TrimmedBlock>();
        }

        void ICloneable<BlockState>.FromReplica(BlockState replica)
        {
            SystemFeeAmount = replica.SystemFeeAmount;
            TrimmedBlock = replica.TrimmedBlock;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(SystemFeeAmount);
            writer.Write(TrimmedBlock);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["sysfee_amount"] = SystemFeeAmount.ToString();
            json["trimmed"] = TrimmedBlock.ToJson();
            return json;
        }
    }
}
