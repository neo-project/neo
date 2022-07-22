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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace Neo.Json.Serialization;

class CompositeSerializer<T> : Serializer<T>
{
    private static readonly (PropertyInfo, Serializer, Func<T, Serializer, JToken?>, Action<T, Serializer, JToken?>)[] properties;

    static CompositeSerializer()
    {
        properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.GetMethod is not null && p.SetMethod is not null)
            .Select(p =>
            {
                Serializer serializer = GetSerializer(p.PropertyType);
                Type sType = serializer.GetType();
                MethodInfo serializeMethod = sType.GetMethod(nameof(Serialize), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
                MethodInfo deserializeMethod = sType.GetMethod(nameof(Deserialize), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
                var instanceExpr = Expression.Parameter(typeof(T));
                var serializerExpr = Expression.Parameter(typeof(Serializer));
                var callExpr = Expression.Call(instanceExpr, p.GetMethod!);
                var convertExpr = Expression.Convert(serializerExpr, sType);
                callExpr = Expression.Call(convertExpr, serializeMethod, callExpr);
                var getterExpr = Expression.Lambda<Func<T, Serializer, JToken?>>(callExpr, instanceExpr, serializerExpr);
                var valueExpr = Expression.Parameter(typeof(JToken));
                callExpr = Expression.Call(convertExpr, deserializeMethod, valueExpr);
                callExpr = Expression.Call(instanceExpr, p.SetMethod!, callExpr);
                var setterExpr = Expression.Lambda<Action<T, Serializer, JToken?>>(callExpr, instanceExpr, serializerExpr, valueExpr);
                return (p, serializer, getterExpr.Compile(), setterExpr.Compile());
            })
            .ToArray();
    }

    [return: NotNullIfNotNull("obj")]
    protected internal override JObject? Serialize(T? obj)
    {
        if (obj is null) return null;
        JObject result = new();
        foreach (var (property, serializer, getter, _) in properties)
            result[property.Name] = getter(obj, serializer);
        return result;
    }

    [return: NotNullIfNotNull("json")]
    protected internal override T? Deserialize(JToken? json)
    {
        switch (json)
        {
            case JObject obj:
                T result = (T)FormatterServices.GetUninitializedObject(typeof(T));
                foreach (var (property, serializer, _, setter) in properties)
                    setter(result, serializer, obj[property.Name]);
                return result;
            case null:
                return default;
            default:
                throw new FormatException();
        }
    }
}
