namespace Neo.IO.Serialization
{
    public class ArraySerializer<T> : Serializer<T[]> where T : Serializable
    {
        private static readonly Serializer<T> elementSerializer = (Serializer<T>)GetDefaultSerializer(typeof(T));

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
    }
}
