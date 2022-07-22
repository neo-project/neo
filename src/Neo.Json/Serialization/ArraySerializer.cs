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

class ArraySerializer<T> : Serializer<T?[]?>
{
    private static readonly Serializer<T> elementSerializer = GetSerializer<T>();

    [return: NotNullIfNotNull("obj")]
    protected internal override JArray? Serialize(T?[]? obj)
    {
        if (obj is null) return null;
        return obj.Select(elementSerializer.Serialize).ToArray();
    }

    [return: NotNullIfNotNull("json")]
    protected internal override T?[]? Deserialize(JToken? json) => json switch
    {
        JArray array => array.Select(elementSerializer.Deserialize).ToArray(),
        null => null,
        _ => throw new FormatException()
    };
}
