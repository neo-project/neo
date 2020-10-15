using System;
using System.IO;
using System.Linq;

namespace Neo.IO
{
    public class ByteArrayWrapper : ICloneable<ByteArrayWrapper>, IEquatable<ByteArrayWrapper>, IEquatable<byte[]>, ISerializable
    {
        public byte[] Value;

        public ByteArrayWrapper()
        {
        }

        public ByteArrayWrapper(byte[] value)
        {
            this.Value = value;
        }

        public int Size => Value is null ? 0 : Value.Length;

        public ByteArrayWrapper Clone()
        {
            return new ByteArrayWrapper(Value);
        }

        public void FromReplica(ByteArrayWrapper replia)
        {
            this.Value = replia.Value;
        }

        public unsafe void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
        }

        public bool Equals(byte[] other)
        {
            if (other is null || Value is null) return false;
            return Value.SequenceEqual<byte>(other);
        }

        public bool Equals(ByteArrayWrapper other)
        {
            if (other is null) return false;
            if (other.Value is null && Value is null) return true;
            if (other.Value is null || Value is null) return false;
            return Value.SequenceEqual<byte>(other.Value);
        }

        public unsafe void Serialize(BinaryWriter writer)
        {
            if (Value is null)
                writer.Write((byte)0);
            else
                writer.WriteVarBytes(Value);
        }

        public static implicit operator ByteArrayWrapper(byte[] value)
        {
            return new ByteArrayWrapper(value);
        }

        public static implicit operator byte[](ByteArrayWrapper wrapper)
        {
            return wrapper.Value;
        }
    }
}
