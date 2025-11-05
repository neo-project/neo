// Copyright (C) 2015-2025 The Neo Project.
//
// RpcPeers.cs file belongs to the neo project and is free
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
    public class RpcPeers
    {
        public RpcPeer[] Unconnected { get; set; }

        public RpcPeer[] Bad { get; set; }

        public RpcPeer[] Connected { get; set; }

        public JsonObject ToJson()
        {
            return new()
            {
                ["unconnected"] = new JsonArray(Unconnected.Select(p => p.ToJson()).ToArray()),
                ["bad"] = new JsonArray(Bad.Select(p => p.ToJson()).ToArray()),
                ["connected"] = new JsonArray(Connected.Select(p => p.ToJson()).ToArray())
            };
        }

        public static RpcPeers FromJson(JsonObject json)
        {
            return new RpcPeers
            {
                Unconnected = ((JsonArray)json["unconnected"]).Select(p => RpcPeer.FromJson((JsonObject)p)).ToArray(),
                Bad = ((JsonArray)json["bad"]).Select(p => RpcPeer.FromJson((JsonObject)p)).ToArray(),
                Connected = ((JsonArray)json["connected"]).Select(p => RpcPeer.FromJson((JsonObject)p)).ToArray()
            };
        }
    }

    public class RpcPeer
    {
        public string Address { get; set; }

        public int Port { get; set; }

        public JsonObject ToJson() => new() { ["address"] = Address, ["port"] = Port };

        public static RpcPeer FromJson(JsonObject json)
        {
            return new RpcPeer
            {
                Address = json["address"].AsString(),
                Port = int.Parse(json["port"].AsString())
            };
        }
    }
}
