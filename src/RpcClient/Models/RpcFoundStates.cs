// Copyright (C) 2015-2025 The Neo Project.
//
// RpcFoundStates.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Linq;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcFoundStates
    {
        public bool Truncated;
        public (byte[] key, byte[] value)[] Results;
        public byte[] FirstProof;
        public byte[] LastProof;

        public static RpcFoundStates FromJson(JsonObject json)
        {
            return new RpcFoundStates
            {
                Truncated = json["truncated"].GetValue<bool>(),
                Results = ((JsonArray)json["results"])
                    .Select(j => (
                        Convert.FromBase64String(j["key"].AsString()),
                        Convert.FromBase64String(j["value"].AsString())
                    ))
                    .ToArray(),
                FirstProof = ProofFromJson((JsonValue)json["firstProof"]),
                LastProof = ProofFromJson((JsonValue)json["lastProof"]),
            };
        }

        static byte[] ProofFromJson(JsonValue json)
            => json == null ? null : Convert.FromBase64String(json.AsString());
    }
}
