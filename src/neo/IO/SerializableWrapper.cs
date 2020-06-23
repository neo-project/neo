using System;
using System.IO;

namespace Neo.IO
{
    public class SerializableWrapper<T> : ICloneable<SerializableWrapper<T>>, IEquatable<T>, IEquatable<SerializableWrapper<T>>, ISerializable
        where T : unmanaged
    {
        private static unsafe readonly int ValueSize = sizeof(T);
        internal T Value;

        public SerializableWrapper()
        {
        }

        private SerializableWrapper(T value)
        {
            this.Value = value;
        }

        public int Size => ValueSize;

        public SerializableWrapper<T> Clone()
        {
            return Value;
        }

        public unsafe void Deserialize(BinaryReader reader)
        {
            fixed (T* p = &Value)
            {
                Span<byte> buffer = new Span<byte>(p, ValueSize);
                int i = 0;
                while (i < ValueSize)
                {
                    int count = reader.Read(buffer[i..]);
                    if (count == 0) throw new FormatException();
                    i += count;
                }
            }
        }

        public bool Equals(T other)
        {
            return Value.Equals(other);
        }

        public bool Equals(SerializableWrapper<T> other)
        {
            return Value.Equals(other.Value);
        }

        public void FromReplica(SerializableWrapper<T> replica)
        {
            Value = replica.Value;
        }

        public unsafe void Serialize(BinaryWriter writer)
        {
            fixed (T* p = &Value)
            {
                ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>(p, ValueSize);
                writer.Write(buffer);
            }
        }

        public static implicit operator SerializableWrapper<T>(T value)
        {
            return new SerializableWrapper<T>(value);
        }

        public static implicit operator T(SerializableWrapper<T> wrapper)
        {
            return wrapper.Value;
        }
    }
}
