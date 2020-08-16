using Neo.IO.Json;
using System;

namespace Neo.IO.Serialization
{
    public class UnmanagedSerializer<T> : Serializer<T> where T : unmanaged
    {
        public unsafe override T Deserialize(MemoryReader reader, SerializedAttribute attribute)
        {
            ReadOnlyMemory<byte> buffer = reader.ReadBytes(sizeof(T));
            fixed (byte* p = buffer.Span)
            {
                return *(T*)p;
            }
        }

        public override T FromJson(JObject json, SerializedAttribute attribute)
        {
            return (T)Convert.ChangeType(json.AsNumber(), typeof(T));
        }

        public unsafe override void Serialize(MemoryWriter writer, T value)
        {
            ReadOnlySpan<byte> buffer = new ReadOnlySpan<byte>(&value, sizeof(T));
            writer.Write(buffer);
        }

        public override JObject ToJson(T value)
        {
            return (double)Convert.ChangeType(value, typeof(double));
        }
    }
}
