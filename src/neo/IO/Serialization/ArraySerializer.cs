using Neo.IO.Json;
using System;
using System.Linq;

namespace Neo.IO.Serialization
{
    public class ArraySerializer<T> : Serializer<T[]> where T : Serializable
    {
        private static readonly Serializer<T> elementSerializer = GetDefaultSerializer<T>();

        public sealed override T[] Deserialize(MemoryReader reader, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            T[] result = new T[reader.ReadVarInt((ulong)max)];
            for (int i = 0; i < result.Length; i++)
                result[i] = DeserializeElement(reader);
            return result;
        }

        protected virtual T DeserializeElement(MemoryReader reader)
        {
            return elementSerializer.Deserialize(reader, null);
        }

        protected virtual T ElementFromJson(JObject json)
        {
            return elementSerializer.FromJson(json, null);
        }

        protected virtual JObject ElementToJson(T value)
        {
            return elementSerializer.ToJson(value);
        }

        public sealed override T[] FromJson(JObject json, SerializedAttribute attribute)
        {
            int max = attribute?.Max >= 0 ? attribute.Max : 0x1000000;
            JArray array = (JArray)json;
            if (array.Count > max) throw new FormatException();
            return array.Select(p => p is null ? null : ElementFromJson(p)).ToArray();
        }

        public sealed override void Serialize(MemoryWriter writer, T[] value)
        {
            writer.WriteVarInt(value.Length);
            foreach (T item in value)
                SerializeElement(writer, item);
        }

        protected virtual void SerializeElement(MemoryWriter writer, T value)
        {
            elementSerializer.Serialize(writer, value);
        }

        public sealed override JObject ToJson(T[] value)
        {
            return new JArray(value.Select(p => p is null ? null : ElementToJson(p)));
        }
    }
}
