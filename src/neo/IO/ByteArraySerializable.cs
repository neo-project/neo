using System;
using System.IO;

namespace Neo.IO
{
    public class ByteArraySerializable : IEquatable<byte[]>, IEquatable<ByteArraySerializable>, ISerializable, ICloneable<ByteArraySerializable>
    {
        public byte[] Value;

        public ByteArraySerializable() { }

        private ByteArraySerializable(byte[] value)
        {
            this.Value = value;
        }

        public int Size => Value.GetVarSize();

        public ByteArraySerializable Clone()
        {
            // We share the value, we should not change the array directly
            return new ByteArraySerializable(this.Value);
        }

        public unsafe void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes(ushort.MaxValue);
        }

        public bool Equals(byte[] other)
        {
            return ByteArrayEqualityComparer.Default.Equals(Value, other);
        }

        public bool Equals(ByteArraySerializable other)
        {
            if (other == null) return false;
            return ByteArrayEqualityComparer.Default.Equals(Value, other.Value);
        }

        public void FromReplica(ByteArraySerializable replica)
        {
            Value = replica.Value;
        }

        public unsafe void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
        }

        public static implicit operator ByteArraySerializable(byte[] value)
        {
            return new ByteArraySerializable(value);
        }

        public static implicit operator byte[](ByteArraySerializable wrapper)
        {
            return wrapper.Value;
        }
    }
}
