// Copyright (C) 2015-2025 The Neo Project.
//
// RpcValidator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System.Numerics;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcValidator
    {
        public string PublicKey { get; set; }

        public BigInteger Votes { get; set; }

        public JsonObject ToJson() => new() { ["publickey"] = PublicKey, ["votes"] = Votes.ToString() };

        public static RpcValidator FromJson(JsonObject json)
        {
            return new RpcValidator
            {
                PublicKey = json["publickey"].AsString(),
                Votes = BigInteger.Parse(json["votes"].AsString()),
            };
        }
    }
}
