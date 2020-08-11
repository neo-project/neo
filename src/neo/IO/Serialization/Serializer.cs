using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Neo.IO.Serialization
{
    public abstract class Serializer
    {
        private static readonly Dictionary<Type, Serializer> defaultSerializers = new Dictionary<Type, Serializer>
        {
            [typeof(bool)] = new UnmanagedSerializer<bool>(),
            [typeof(sbyte)] = new VarIntSerializer<sbyte>(),
            [typeof(byte)] = new VarIntSerializer<byte>(),
            [typeof(short)] = new VarIntSerializer<short>(),
            [typeof(ushort)] = new VarIntSerializer<ushort>(),
            [typeof(int)] = new VarIntSerializer<int>(),
            [typeof(uint)] = new VarIntSerializer<uint>(),
            [typeof(long)] = new VarIntSerializer<long>(),
            [typeof(ulong)] = new VarIntSerializer<ulong>(),
            [typeof(string)] = new StringSerializer(),
            [typeof(byte[])] = new ByteArraySerializer()
        };

        public abstract void DeserializeProperty(BinaryReader reader, object obj, PropertyInfo property, SerializedAttribute attribute);

        protected static Serializer GetDefaultSerializer(Type type)
        {
            if (!defaultSerializers.TryGetValue(type, out Serializer serializer))
            {
                if (type.IsEnum)
                    serializer = (Serializer)Activator.CreateInstance(typeof(UnmanagedSerializer<>).MakeGenericType(type));
                else if (type.IsArray)
                    serializer = (Serializer)Activator.CreateInstance(typeof(ArraySerializer<>).MakeGenericType(type.GetElementType()));
                else
                    serializer = (Serializer)Activator.CreateInstance(typeof(CompositeSerializer<>).MakeGenericType(type));
                defaultSerializers.Add(type, serializer);
            }
            return serializer;
        }

        public abstract void SerializeProperty(BinaryWriter writer, object obj, PropertyInfo property);
    }

    public abstract class Serializer<T> : Serializer
    {
        private static readonly ConcurrentDictionary<PropertyInfo, (Delegate, Delegate)> callbacks = new ConcurrentDictionary<PropertyInfo, (Delegate, Delegate)>();

        public abstract T Deserialize(BinaryReader reader, SerializedAttribute attribute);

        public sealed override void DeserializeProperty(BinaryReader reader, object obj, PropertyInfo property, SerializedAttribute attribute)
        {
            (_, var setter) = GetCallbacks(property);
            Action<object, T> action = (Action<object, T>)setter;
            action(obj, Deserialize(reader, attribute));
        }

        private static (Delegate, Delegate) GetCallbacks(PropertyInfo property)
        {
            return callbacks.GetOrAdd(property, p =>
            {
                var instExpr = Expression.Parameter(typeof(object));
                var convertExpr = Expression.Convert(instExpr, p.DeclaringType);
                var callExpr = Expression.Call(convertExpr, p.GetMethod);
                var getterExpr = Expression.Lambda<Func<object, T>>(callExpr, instExpr);
                var paramExpr = Expression.Parameter(typeof(T));
                callExpr = Expression.Call(convertExpr, p.SetMethod, paramExpr);
                var setterExpr = Expression.Lambda<Action<object, T>>(callExpr, instExpr, paramExpr);
                return (getterExpr.Compile(), setterExpr.Compile());
            });
        }

        public abstract void Serialize(BinaryWriter writer, T value);

        public sealed override void SerializeProperty(BinaryWriter writer, object obj, PropertyInfo property)
        {
            (var getter, _) = GetCallbacks(property);
            Func<object, T> func = (Func<object, T>)getter;
            Serialize(writer, func(obj));
        }
    }
}
