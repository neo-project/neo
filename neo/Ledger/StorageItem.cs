using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    public class StorageItem : StateBase, ICloneable<StorageItem>
    {
        public byte[] Value;
        public bool IsConstant;
        // Block Height when storage item was last updated
        public uint Height;

        public override int Size => base.Size + Value.GetVarSize() + sizeof(uint) + sizeof(bool);

        StorageItem ICloneable<StorageItem>.Clone()
        {
            return new StorageItem
            {
                Value = Value,
                IsConstant = IsConstant,
                Height = Height
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Value = reader.ReadVarBytes();
            IsConstant = reader.ReadBoolean();
            Height = reader.ReadUInt32();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
            IsConstant = replica.IsConstant;
            Height = replica.Height;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
            writer.Write(Height);
        }
    }
}
