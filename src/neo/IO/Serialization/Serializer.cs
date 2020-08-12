using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            [typeof(byte[])] = new ByteArraySerializer(),
            [typeof(ReadOnlyMemory<byte>)] = new MemorySerializer()
        };
        private static readonly Dictionary<Type, Action<Serializer, MemoryWriter, Serializable>> serializeActions = new Dictionary<Type, Action<Serializer, MemoryWriter, Serializable>>();

        public static T Deserialize<T>(ReadOnlyMemory<byte> memory) where T : Serializable
        {
            Serializer<T> serializer = GetDefaultSerializer<T>();
            return serializer.Deserialize(memory, null);
        }

        public abstract void DeserializeProperty(MemoryReader reader, Serializable obj, PropertyInfo property, SerializedAttribute attribute);

        protected static Serializer<T> GetDefaultSerializer<T>()
        {
            return (Serializer<T>)GetDefaultSerializer(typeof(T));
        }

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

        private static Action<Serializer, MemoryWriter, Serializable> GetSerializeAction(Type type)
        {
            if (!serializeActions.TryGetValue(type, out var action))
            {
                Type serializerType = typeof(CompositeSerializer<>).MakeGenericType(type);
                var instExpr = Expression.Parameter(typeof(Serializer));
                var convertExprInst = Expression.Convert(instExpr, serializerType);
                var paramExpr1 = Expression.Parameter(typeof(MemoryWriter));
                var paramExpr2 = Expression.Parameter(typeof(Serializable));
                var convertExpr2 = Expression.Convert(paramExpr2, type);
                var callExpr = Expression.Call(convertExprInst, serializerType.GetMethod(nameof(Serialize)), paramExpr1, convertExpr2);
                var actionExpr = Expression.Lambda<Action<Serializer, MemoryWriter, Serializable>>(callExpr, instExpr, paramExpr1, paramExpr2);
                action = actionExpr.Compile();
                serializeActions.Add(type, action);
            }
            return action;
        }

        public static byte[] Serialize(Serializable serializable)
        {
            Type type = serializable.GetType();
            Serializer serializer = GetDefaultSerializer(type);
            Action<Serializer, MemoryWriter, Serializable> action = GetSerializeAction(type);
            using MemoryWriter writer = new MemoryWriter();
            action(serializer, writer, serializable);
            return writer.ToArray();
        }

        public abstract void SerializeProperty(MemoryWriter writer, Serializable obj, PropertyInfo property);
    }

    public abstract class Serializer<T> : Serializer
    {
        private static readonly ConcurrentDictionary<PropertyInfo, (Delegate, Delegate)> callbacks = new ConcurrentDictionary<PropertyInfo, (Delegate, Delegate)>();

        public abstract T Deserialize(MemoryReader reader, SerializedAttribute attribute);

        public sealed override void DeserializeProperty(MemoryReader reader, Serializable obj, PropertyInfo property, SerializedAttribute attribute)
        {
            (_, var setter) = GetCallbacks(property);
            Action<Serializable, T> action = (Action<Serializable, T>)setter;
            action(obj, Deserialize(reader, attribute));
        }

        private static (Delegate, Delegate) GetCallbacks(PropertyInfo property)
        {
            return callbacks.GetOrAdd(property, p =>
            {
                var instExpr = Expression.Parameter(typeof(Serializable));
                var convertExpr = Expression.Convert(instExpr, p.DeclaringType);
                var callExpr = Expression.Call(convertExpr, p.GetMethod);
                var getterExpr = Expression.Lambda<Func<Serializable, T>>(callExpr, instExpr);
                var paramExpr = Expression.Parameter(typeof(T));
                callExpr = Expression.Call(convertExpr, p.SetMethod, paramExpr);
                var setterExpr = Expression.Lambda<Action<Serializable, T>>(callExpr, instExpr, paramExpr);
                return (getterExpr.Compile(), setterExpr.Compile());
            });
        }

        public abstract void Serialize(MemoryWriter writer, T value);

        public sealed override void SerializeProperty(MemoryWriter writer, Serializable obj, PropertyInfo property)
        {
            (var getter, _) = GetCallbacks(property);
            Func<Serializable, T> func = (Func<Serializable, T>)getter;
            Serialize(writer, func(obj));
        }
    }
}
