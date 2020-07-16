using Neo.IO;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Neo.Ledger
{
    public class StorageItem : ICloneable<StorageItem>, ISerializable
    {
        private byte[] value;
        public bool IsConstant;
        private object cache;
        private Func<byte[]> serializer;

        public int Size => Value.GetVarSize() + sizeof(bool);

        public byte[] Value
        {
            get
            {
                if (value is null && serializer != null)
                    value = serializer();
                return value;
            }
            set
            {
                cache = null;
                serializer = null;
                this.value = value;
            }
        }

        public StorageItem() { }

        public StorageItem(byte[] value, bool isConstant = false)
        {
            this.IsConstant = isConstant;
            this.value = value;
            this.cache = null;
            this.serializer = null;
        }

        public StorageItem(BigInteger value, bool isConstant = false)
        {
            this.IsConstant = isConstant;
            this.value = value.ToByteArrayStandard();
            this.cache = value;
        }

        public StorageItem(IInteroperable interoperable, bool isConstant = false)
        {
            this.IsConstant = isConstant;
            this.value = null;
            this.cache = interoperable;
            this.serializer = () => BinarySerializer.Serialize(interoperable.ToStackItem(null), 4096);
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

        public void Set(BigInteger value)
        {
            this.value = value.ToByteArrayStandard();
            this.cache = value;
        }

        public void Set<T>(IReadOnlyCollection<T> value) where T : ISerializable
        {
            this.value = null;
            this.cache = value;
            this.serializer = () => value.ToByteArray();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
            IsConstant = replica.IsConstant;
        }

        public T GetInteroperable<T>() where T : IInteroperable, new()
        {
            if (cache is null)
            {
                var interoperable = new T();
                interoperable.FromStackItem(BinarySerializer.Deserialize(value, 16, 34));
                this.serializer = () => BinarySerializer.Serialize(interoperable.ToStackItem(null), 4096);
                cache = interoperable;
            }
            value = null;
            return (T)cache;
        }

        public T[] GetSerializableArray<T>(int max = 0x1000000) where T : ISerializable, new()
        {
            if (cache is null)
            {
                var ret = Value.AsSerializableArray<T>(max);
                this.serializer = () => ret.ToByteArray();
                cache = ret;
            }
            value = null;
            return ((IReadOnlyCollection<T>)cache).ToArray();
        }

        public BigInteger GetBigInteger()
        {
            if (cache is null)
            {
                var ret = new BigInteger(Value);
                this.serializer = () => ret.ToByteArrayStandard();
                cache = ret;
            }
            value = null;
            return (BigInteger)cache;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
        }
    }
}
