// Copyright (C) 2015-2025 The Neo Project.
//
// RpcRawMemPool.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcRawMemPool
    {
        public uint Height { get; set; }

        public List<UInt256> Verified { get; set; }

        public List<UInt256> UnVerified { get; set; }

        public JsonObject ToJson()
        {
            return new()
            {
                ["height"] = Height,
                ["verified"] = new JsonArray(Verified.Select(p => (JsonNode)p.ToString()).ToArray()),
                ["unverified"] = new JsonArray(UnVerified.Select(p => (JsonNode)p.ToString()).ToArray())
            };
        }

        public static RpcRawMemPool FromJson(JsonObject json)
        {
            return new RpcRawMemPool
            {
                Height = uint.Parse(json["height"].AsString()),
                Verified = ((JsonArray)json["verified"]).Select(p => UInt256.Parse(p.AsString())).ToList(),
                UnVerified = ((JsonArray)json["unverified"]).Select(p => UInt256.Parse(p.AsString())).ToList()
            };
        }
    }
}
