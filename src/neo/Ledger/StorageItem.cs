using Neo.IO;
using Neo.SmartContract;
using System.IO;

namespace Neo.Ledger
{
    public class StorageItem : ICloneable<StorageItem>, ISerializable
    {
        private byte[] value;
        private IInteroperable interoperable;
        public bool IsConstant;

        public int Size => Value.GetVarSize() + sizeof(bool);

        public byte[] Value
        {
            get
            {
                if (value is null && interoperable != null)
                    value = BinarySerializer.Serialize(interoperable.ToStackItem(null), 4096);
                return value;
            }
            set
            {
                interoperable = null;
                this.value = value;
            }
        }

        public StorageItem()
        {
        }

        public StorageItem(byte[] value, bool isConstant = false)
        {
            this.value = value;
            this.IsConstant = isConstant;
        }

        public StorageItem(IInteroperable interoperable, bool isConstant = false)
        {
            this.interoperable = interoperable;
            this.IsConstant = isConstant;
        }

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

        public T GetInteroperable<T>() where T : IInteroperable, new()
        {
            if (interoperable is null)
            {
                interoperable = new T();
                interoperable.FromStackItem(BinarySerializer.Deserialize(value, 16, 34));
            }
            value = null;
            return (T)interoperable;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
        }
    }
}
