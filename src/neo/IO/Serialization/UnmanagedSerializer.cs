using System;
using System.IO;

namespace Neo.IO.Serialization
{
    public class UnmanagedSerializer<T> : Serializer<T> where T : unmanaged
    {
        public unsafe override T Deserialize(BinaryReader reader, SerializedAttribute attribute)
        {
            Span<byte> buffer = stackalloc byte[sizeof(T)];
            reader.FillBuffer(buffer);
            fixed (byte* p = buffer)
            {
                return *(T*)p;
            }
        }

        public unsafe override void Serialize(BinaryWriter writer, T value)
        {
            ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>(&value, sizeof(T));
            writer.Write(buffer);
        }
    }
}
