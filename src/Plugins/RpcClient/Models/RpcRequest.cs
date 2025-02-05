// Copyright (C) 2015-2025 The Neo Project.
//
// RpcRequest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System.Linq;

namespace Neo.Network.RPC.Models
{
    public class RpcRequest
    {
        public JToken Id { get; set; }

        public string JsonRpc { get; set; }

        public string Method { get; set; }

        public JToken[] Params { get; set; }

        public static RpcRequest FromJson(JObject json)
        {
            return new RpcRequest
            {
                Id = json["id"],
                JsonRpc = json["jsonrpc"].AsString(),
                Method = json["method"].AsString(),
                Params = ((JArray)json["params"]).ToArray()
            };
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["id"] = Id;
            json["jsonrpc"] = JsonRpc;
            json["method"] = Method;
            json["params"] = new JArray(Params);
            return json;
        }
    }
}
