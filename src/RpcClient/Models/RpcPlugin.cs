// Copyright (C) 2015-2025 The Neo Project.
//
// RpcPlugin.cs file belongs to the neo project and is free
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
    public class RpcPlugin
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public string[] Interfaces { get; set; }

        public JsonObject ToJson()
        {
            return new()
            {
                ["name"] = Name,
                ["version"] = Version,
                ["interfaces"] = new JsonArray(Interfaces.Select(p => (JsonNode)p).ToArray())
            };
        }

        public static RpcPlugin FromJson(JsonObject json)
        {
            return new RpcPlugin
            {
                Name = json["name"].AsString(),
                Version = json["version"].AsString(),
                Interfaces = ((JsonArray)json["interfaces"]).Select(p => p.AsString()).ToArray()
            };
        }
    }
}
