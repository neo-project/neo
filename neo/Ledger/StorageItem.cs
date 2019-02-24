using System;
using Neo.IO;
using System.IO;
using System.Linq;
using Neo.Cryptography;

namespace Neo.Ledger
{
    public class StorageItem : StateBase, ICloneable<StorageItem>, IEquatable<StorageItem>
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

        public bool Equals(StorageItem other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsConstant == other.IsConstant && Value.SequenceEqual(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((StorageItem) obj);
        }

        public override int GetHashCode() =>
            IsConstant.GetHashCode() + (Value != null ? (int) Value.Murmur32(0) : 0);
    }
}