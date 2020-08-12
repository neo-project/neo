using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Neo.IO.Serialization
{
    public class CompositeSerializer<T> : Serializer<T> where T : Serializable
    {
        private readonly (PropertyInfo, SerializedAttribute, Serializer)[] serializers;

        protected virtual Type TargetType { get; } = typeof(T);

        public CompositeSerializer()
        {
            serializers = GetSerializers().OrderBy(p => p.Item2.Order).ToArray();
        }

        public sealed override T Deserialize(MemoryReader reader, SerializedAttribute _)
        {
            var constructorInfo = TargetType.GetConstructor(Type.EmptyTypes);
            var newExpr = Expression.New(constructorInfo);
            var constructorExpr = Expression.Lambda<Func<object>>(newExpr);
            var constructor = constructorExpr.Compile();
            object obj = constructor();
            foreach (var (info, attribute, serializer) in serializers)
                serializer.DeserializeProperty(reader, obj, info, attribute);
            return (T)obj;
        }

        private static Dictionary<string, PropertyInfo> GetProperties(Type type)
        {
            if (type.BaseType is null) return new Dictionary<string, PropertyInfo>();
            Dictionary<string, PropertyInfo> dictionary = GetProperties(type.BaseType);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                dictionary[property.Name] = property;
            return dictionary;
        }

        private IEnumerable<(PropertyInfo, SerializedAttribute, Serializer)> GetSerializers()
        {
            foreach (PropertyInfo info in GetProperties(TargetType).Values)
            {
                if (info.GetMethod is null || info.SetMethod is null) continue;
                SerializedAttribute attribute = info.GetCustomAttribute<SerializedAttribute>();
                if (attribute is null) continue;
                Serializer serializer = attribute.Serializer is null
                    ? GetDefaultSerializer(info.PropertyType)
                    : (Serializer)Activator.CreateInstance(attribute.Serializer);
                yield return (info, attribute, serializer);
            }
        }

        public sealed override void Serialize(MemoryWriter writer, T value)
        {
            foreach (var (info, _, serializer) in serializers)
                serializer.SerializeProperty(writer, value, info);
        }
    }
}
