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

class UInt64Serializer : Serializer<ulong>
{
    protected internal override JToken Serialize(ulong obj) => obj <= JNumber.MAX_SAFE_INTEGER ? obj : obj.ToString();

    protected internal override ulong Deserialize(JToken? json) => json switch
    {
        JNumber n => (ulong)n.Value,
        JString s => ulong.Parse(s.Value),
        _ => throw new FormatException()
    };
}
