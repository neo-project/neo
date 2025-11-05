// Copyright (C) 2015-2025 The Neo Project.
//
// RpcAccount.cs file belongs to the neo project and is free
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
    public class RpcAccount
    {
        public string Address { get; set; }

        public bool HasKey { get; set; }

        public string Label { get; set; }

        public bool WatchOnly { get; set; }

        public JsonObject ToJson()
        {
            return new()
            {
                ["address"] = Address,
                ["haskey"] = HasKey,
                ["label"] = Label,
                ["watchonly"] = WatchOnly
            };
        }

        public static RpcAccount FromJson(JsonObject json)
        {
            return new RpcAccount
            {
                Address = json["address"].AsString(),
                HasKey = json["haskey"].GetValue<bool>(),
                Label = json["label"]?.AsString(),
                WatchOnly = json["watchonly"].GetValue<bool>(),
            };
        }
    }
}
