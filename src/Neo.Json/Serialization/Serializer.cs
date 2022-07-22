// Copyright (C) 2015-2022 The Neo Project.
// 
// The Neo.Json is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;

namespace Neo.Json.Serialization;

public abstract class Serializer
{
    private static readonly Dictionary<Type, Serializer> serializers = new()
    {
        [typeof(bool)] = new BooleanSerializer(),
        [typeof(sbyte)] = new IntegerSerializer<sbyte>(),
        [typeof(byte)] = new IntegerSerializer<byte>(),
        [typeof(short)] = new IntegerSerializer<short>(),
        [typeof(ushort)] = new IntegerSerializer<ushort>(),
        [typeof(int)] = new IntegerSerializer<int>(),
        [typeof(uint)] = new IntegerSerializer<uint>(),
        [typeof(long)] = new Int64Serializer(),
        [typeof(ulong)] = new UInt64Serializer(),
        [typeof(string)] = new StringSerializer(),
        [typeof(byte[])] = new ByteArraySerializer()
    };

    protected static Serializer GetSerializer(Type type)
    {
        if (!serializers.TryGetValue(type, out var serializer))
        {
            Type? serializerAttribute = type.CustomAttributes
                .Select(p => p.AttributeType)
                .FirstOrDefault(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(SerializerAttribute<>));
            if (serializerAttribute is not null)
                serializer = (Serializer)Activator.CreateInstance(serializerAttribute.GenericTypeArguments[0])!;
            else if (type.IsEnum)
                serializer = (Serializer)Activator.CreateInstance(typeof(EnumSerializer<>).MakeGenericType(type))!;
            else if (type.IsArray)
                serializer = (Serializer)Activator.CreateInstance(typeof(ArraySerializer<>).MakeGenericType(type.GetElementType()!))!;
            else
                serializer = (Serializer)Activator.CreateInstance(typeof(CompositeSerializer<>).MakeGenericType(type))!;
            serializers.Add(type, serializer);
        }
        return serializer;
    }

    protected static Serializer<T> GetSerializer<T>()
    {
        return (Serializer<T>)GetSerializer(typeof(T));
    }

    [return: NotNullIfNotNull("obj")]
    public static JToken? Serialize(object? obj)
    {
        if (obj is null) return JToken.Null;
        Serializer serializer = GetSerializer(obj.GetType());
        return serializer.SerializeObject(obj);
    }

    public static JToken Serialize<T>(T obj) where T : struct
    {
        Serializer<T> serializer = GetSerializer<T>();
        return serializer.Serialize(obj);
    }

    [return: NotNullIfNotNull("json")]
    public static T? Deserialize<T>(JToken? json)
    {
        Serializer<T> serializer = GetSerializer<T>();
        return serializer.Deserialize(json);
    }

    private protected abstract JToken SerializeObject(object obj);
}

public abstract class Serializer<T> : Serializer
{
    private protected sealed override JToken SerializeObject(object obj) => Serialize((T)obj);

    [return: NotNullIfNotNull("obj")]
    protected internal abstract JToken? Serialize(T? obj);

    [return: NotNullIfNotNull("json")]
    protected internal abstract T? Deserialize(JToken? json);
}
