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

class EnumSerializer<T> : Serializer<T> where T : unmanaged, Enum
{
    protected internal override JString Serialize(T obj) => obj.ToString();

    protected internal override T Deserialize(JToken? json) => json!.GetEnum<T>();
}
