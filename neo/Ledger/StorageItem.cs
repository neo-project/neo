using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    public class StorageItem : ICloneable<StorageItem>, ISerializable
    {
        public byte[] Value;
        public bool IsConstant;

        public int Size => Value.GetVarSize() + sizeof(bool);

        StorageItem ICloneable<StorageItem>.Clone()
        {
            return new StorageItem
            {
                Value = Value,
                IsConstant = IsConstant
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
            IsConstant = reader.ReadBoolean();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
            IsConstant = replica.IsConstant;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
        }
    }
}
