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
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    class RpcNefFile
    {
        public static NefFile FromJson(JsonObject json)
        {
            return new NefFile
            {
                Compiler = json["compiler"].AsString(),
                Source = json["source"].AsString(),
                Tokens = ((JsonArray)json["tokens"]).Select(p => RpcMethodToken.FromJson((JsonObject)p)).ToArray(),
                Script = Convert.FromBase64String(json["script"].AsString()),
                CheckSum = (uint)json["checksum"].AsNumber()
            };
        }
    }
}
