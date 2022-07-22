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

class StringSerializer : Serializer<string?>
{
    [return: NotNullIfNotNull("json")]
    protected internal override JString? Serialize(string? obj) => obj;

    [return: NotNullIfNotNull("json")]
    protected internal override string? Deserialize(JToken? json) => json?.GetString();
}
