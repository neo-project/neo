using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    public class StorageItem : StateBase, ICloneable<StorageItem>
    {
        public byte[] Value;
        public bool IsConstant;

        public override int Size => base.Size + Value.GetVarSize() + sizeof(bool);

        StorageItem ICloneable<StorageItem>.Clone()
        {
            return new StorageItem
            {
                Value = Value,
                IsConstant = IsConstant
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Value = reader.ReadVarBytes();
            IsConstant = reader.ReadBoolean();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
            IsConstant = replica.IsConstant;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
        }
    }
}
