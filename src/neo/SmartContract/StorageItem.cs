using Neo.IO;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract
{
    public class StorageItem : ISerializable
    {
        private byte[] value;
        private object cache;
        public bool IsConstant;

        public int Size => Value.GetVarSize() + sizeof(bool);

        public byte[] Value
        {
            get
            {
                return value ??= cache switch
                {
                    BigInteger bi => bi.ToByteArrayStandard(),
                    IInteroperable interoperable => BinarySerializer.Serialize(interoperable.ToStackItem(null), 1024 * 1024),
                    IReadOnlyCollection<ISerializable> list => list.ToByteArray(),
                    null => null,
                    _ => throw new InvalidCastException()
                };
            }
            set
            {
                this.value = value;
                cache = null;
            }
        }

        public StorageItem() { }

        public StorageItem(byte[] value, bool isConstant = false)
        {
            this.value = value;
            this.IsConstant = isConstant;
        }

        public StorageItem(BigInteger value, bool isConstant = false)
        {
            this.cache = value;
            this.IsConstant = isConstant;
        }

        public StorageItem(IInteroperable interoperable, bool isConstant = false)
        {
            this.cache = interoperable;
            this.IsConstant = isConstant;
        }

        public void Add(BigInteger integer)
        {
            Set(this + integer);
        }

        public StorageItem Clone()
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

        public void FromReplica(StorageItem replica)
        {
            Value = replica.Value;
            IsConstant = replica.IsConstant;
        }

        public T GetInteroperable<T>() where T : IInteroperable, new()
        {
            if (cache is null)
            {
                var interoperable = new T();
                interoperable.FromStackItem(BinarySerializer.Deserialize(value, ExecutionEngineLimits.Default.MaxStackSize, ExecutionEngineLimits.Default.MaxItemSize));
                cache = interoperable;
            }
            value = null;
            return (T)cache;
        }

        public List<T> GetSerializableList<T>() where T : ISerializable, new()
        {
            cache ??= new List<T>(value.AsSerializableArray<T>());
            value = null;
            return (List<T>)cache;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
            writer.Write(IsConstant);
        }

        public void Set(BigInteger integer)
        {
            cache = integer;
            value = null;
        }

        public static implicit operator BigInteger(StorageItem item)
        {
            item.cache ??= new BigInteger(item.value);
            return (BigInteger)item.cache;
        }
    }
}
