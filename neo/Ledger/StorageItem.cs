using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    public class StorageItem : ICloneable<StorageItem>, ISerializable
    {
        public byte[] Value;

        public int Size => Value.GetVarSize();

        StorageItem ICloneable<StorageItem>.Clone()
        {
            return new StorageItem
            {
                Value = Value
            };
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
        }
    }
}
