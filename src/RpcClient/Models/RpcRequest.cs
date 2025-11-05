// Copyright (C) 2015-2025 The Neo Project.
//
// RpcRequest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System.Linq;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcRequest
    {
        public JsonNode Id { get; set; }

        public string JsonRpc { get; set; }

        public string Method { get; set; }

        public JsonNode[] Params { get; set; }

        public static RpcRequest FromJson(JsonObject json)
        {
            return new RpcRequest
            {
                Id = json["id"],
                JsonRpc = json["jsonrpc"].AsString(),
                Method = json["method"].AsString(),
                Params = ((JsonArray)json["params"]).ToArray()
            };
        }

        public JsonObject ToJson()
        {
            return new()
            {
                ["id"] = Id,
                ["jsonrpc"] = JsonRpc,
                ["method"] = Method,
                ["params"] = new JsonArray(Params)
            };
        }
    }
}
