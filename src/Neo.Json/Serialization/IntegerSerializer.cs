// Copyright (C) 2015-2022 The Neo Project.
// 
// The Neo.Json is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Json.Serialization;

class IntegerSerializer<T> : Serializer<T> where T : unmanaged
{
    protected internal override JToken Serialize(T obj) => (JToken)Convert.ToDouble(obj);

    protected internal override T Deserialize(JToken? json) => json switch
    {
        JNumber n => (T)Convert.ChangeType(n.Value, typeof(T)),
        JString s => (T)Convert.ChangeType(long.Parse(s.Value), typeof(T)),
        _ => throw new FormatException()
    };
}
