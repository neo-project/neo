// Copyright (C) 2015-2025 The Neo Project.
//
// RpcUnclaimedGas.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcUnclaimedGas
    {
        public long Unclaimed { get; set; }

        public string Address { get; set; }

        public JsonObject ToJson() => new() { ["unclaimed"] = Unclaimed.ToString(), ["address"] = Address };

        public static RpcUnclaimedGas FromJson(JsonObject json)
        {
            return new RpcUnclaimedGas
            {
                Unclaimed = long.Parse(json["unclaimed"].AsString()),
                Address = json["address"].AsString()
            };
        }
    }
}
