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

class ByteArraySerializer : Serializer<byte[]?>
{
    [return: NotNullIfNotNull("obj")]
    protected internal override JString? Serialize(byte[]? obj) => obj is null ? null : Convert.ToBase64String(obj);

    [return: NotNullIfNotNull("json")]
    protected internal override byte[]? Deserialize(JToken? json) => json is null ? null : Convert.FromBase64String(json.GetString());
}
