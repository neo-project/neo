using Neo.IO;
using Neo.SmartContract;
using System.IO;
using System.Numerics;

namespace Neo.Ledger
{
    public class StorageItem : ICloneable<StorageItem>, ISerializable
    {
        private byte[] value;
        public bool IsConstant;
        private object cached;

        public int Size => Value.GetVarSize() + sizeof(bool);

        public byte[] Value
        {
            get
            {
                if (value is null && cached is IInteroperable interoperable)
                    value = BinarySerializer.Serialize(interoperable.ToStackItem(null), 4096);
                return value;
            }
            set
            {
                cached = null;
                this.value = value;
            }
        }

        public StorageItem() { }

        public StorageItem(byte[] value, bool isConstant = false)
        {
            this.value = value;
            this.IsConstant = isConstant;
        }

        public StorageItem(IInteroperable interoperable, bool isConstant = false)
        {
            this.cached = interoperable;
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

        public T[] GetSerializableArray<T>(int max = 0x1000000) where T : ISerializable, new()
        {
            if (cached is T[] value) return value;

            var ret = Value.AsSerializableArray<T>(max);
            cached = ret;
            return ret;
        }

        public T GetSerializable<T>() where T : ISerializable, new()
        {
            if (cached is T value) return value;

            var ret = Value.AsSerializable<T>();
            cached = ret;
            return ret;
        }

        public BigInteger GetBigInteger()
        {
            if (cached is BigInteger value) return value;

            var ret = new BigInteger(Value);
            cached = ret;
            return ret;
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
            IsConstant = replica.IsConstant;
        }

        public T GetInteroperable<T>() where T : IInteroperable, new()
        {
            if (cached is null)
            {
                var interoperable = new T();
                interoperable.FromStackItem(BinarySerializer.Deserialize(value, 16, 34));
                cached = interoperable;
            }
            value = null;
            return (T)cached;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
        }
    }
}
