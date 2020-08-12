using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.IO.Serialization
{
    public partial class PolymorphousArraySerializer<T, TEnum> : ArraySerializer<T>
        where T : Serializable
        where TEnum : Enum
    {
        private static readonly Dictionary<Type, ElementSerializer> serializers = ReflectionCache<TEnum>.GetTypes().ToDictionary(p => p, p => new ElementSerializer(p));

        protected override T DeserializeElement(BinaryReader reader)
        {
            TEnum e = (TEnum)Enum.ToObject(typeof(TEnum), reader.ReadByte());
            Type type = ReflectionCache<TEnum>.GetType(e);
            ElementSerializer serializer = serializers[type];
            reader.BaseStream.Seek(-1, SeekOrigin.Current);
            return serializer.Deserialize(reader, null);
        }

        protected override void SerializeElement(BinaryWriter writer, T value)
        {
            ElementSerializer serializer = serializers[value.GetType()];
            serializer.Serialize(writer, value);
        }
    }
}
