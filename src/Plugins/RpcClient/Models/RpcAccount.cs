// Copyright (C) 2015-2025 The Neo Project.
//
// RpcAccount.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;

namespace Neo.Network.RPC.Models
{
    public class RpcAccount
    {
        public string Address { get; set; }

        public bool HasKey { get; set; }

        public string Label { get; set; }

        public bool WatchOnly { get; set; }

        public JObject ToJson()
        {
            return new JObject
            {
                ["address"] = Address,
                ["haskey"] = HasKey,
                ["label"] = Label,
                ["watchonly"] = WatchOnly
            };
        }

        public static RpcAccount FromJson(JObject json)
        {
            return new RpcAccount
            {
                Address = json["address"].AsString(),
                HasKey = json["haskey"].AsBoolean(),
                Label = json["label"]?.AsString(),
                WatchOnly = json["watchonly"].AsBoolean(),
            };
        }
    }
}
