using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    public class StorageItem : ICloneable<StorageItem>, ISerializable
    {
        public byte[] Value;
        public bool IsConstant;
        public bool IsIntegerCache;

        public int Size => Value.GetVarSize() + sizeof(bool);

        StorageItem ICloneable<StorageItem>.Clone()
        {
            return new StorageItem
            {
                Value = Value,
                IsConstant = IsConstant,
                IsIntegerCache = IsIntegerCache
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
            IsConstant = reader.ReadBoolean();
            IsIntegerCache = reader.ReadBoolean();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
            IsConstant = replica.IsConstant;
            IsIntegerCache = replica.IsIntegerCache;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
            writer.Write(IsIntegerCache);
        }
    }
}
