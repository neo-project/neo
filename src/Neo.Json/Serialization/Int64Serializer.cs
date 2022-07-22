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

class Int64Serializer : Serializer<long>
{
    protected internal override JToken Serialize(long obj) => obj >= JNumber.MIN_SAFE_INTEGER && obj <= JNumber.MAX_SAFE_INTEGER ? obj : obj.ToString();

    protected internal override long Deserialize(JToken? json) => json switch
    {
        JNumber n => (long)n.Value,
        JString s => long.Parse(s.Value),
        _ => throw new FormatException()
    };
}
