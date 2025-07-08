// Copyright (C) 2015-2025 The Neo Project.
//
// RpcNefFile.cs file belongs to the neo project and is free
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
using System.Linq;

namespace Neo.Network.RPC.Models
{
    class RpcNefFile
    {
        public static NefFile FromJson(JObject json)
        {
            return new NefFile
            {
                Compiler = json["compiler"].AsString(),
                Source = json["source"].AsString(),
                Tokens = ((JArray)json["tokens"]).Select(p => RpcMethodToken.FromJson((JObject)p)).ToArray(),
                Script = Convert.FromBase64String(json["script"].AsString()),
                CheckSum = (uint)json["checksum"].AsNumber()
            };
        }
    }
}
