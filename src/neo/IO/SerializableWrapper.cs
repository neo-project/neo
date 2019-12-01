using System;
using System.IO;

namespace Neo.IO
{
    public class SerializableWrapper<T> : IEquatable<T>, IEquatable<SerializableWrapper<T>>, ISerializable
        where T : unmanaged
    {
        private static unsafe readonly int ValueSize = sizeof(T);
        private T value;

        public SerializableWrapper()
        {
        }

        private SerializableWrapper(T value)
        {
            this.value = value;
        }

        public int Size => ValueSize;

        public unsafe void Deserialize(BinaryReader reader)
        {
            fixed (T* p = &value)
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
            return value.Equals(other);
        }

        public bool Equals(SerializableWrapper<T> other)
        {
            return value.Equals(other.value);
        }

        public unsafe void Serialize(BinaryWriter writer)
        {
            fixed (T* p = &value)
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
            return wrapper.value;
        }
    }
}
