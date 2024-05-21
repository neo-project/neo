// Copyright (C) 2015-2024 The Neo Project.
//
// RpcMethodToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract;
using System;

namespace Neo.Network.RPC.Models
{
    class RpcMethodToken
    {
        public static MethodToken FromJson(JObject json)
        {
            return new MethodToken
            {
                Hash = UInt160.Parse(json["hash"].AsString()),
                Method = json["method"].AsString(),
                ParametersCount = (ushort)json["paramcount"].AsNumber(),
                HasReturnValue = json["hasreturnvalue"].AsBoolean(),
                CallFlags = (CallFlags)Enum.Parse(typeof(CallFlags), json["callflags"].AsString())
            };
        }
    }
}
