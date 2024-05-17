// Copyright (C) 2015-2024 The Neo Project.
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

namespace Neo.Network.RPC.Models
{
    public class RpcFoundStates
    {
        public bool Truncated;
        public (byte[] key, byte[] value)[] Results;
        public byte[] FirstProof;
        public byte[] LastProof;

        public static RpcFoundStates FromJson(JObject json)
        {
            return new RpcFoundStates
            {
                Truncated = json["truncated"].AsBoolean(),
                Results = ((JArray)json["results"])
                    .Select(j => (
                        Convert.FromBase64String(j["key"].AsString()),
                        Convert.FromBase64String(j["value"].AsString())
                    ))
                    .ToArray(),
                FirstProof = ProofFromJson((JString)json["firstProof"]),
                LastProof = ProofFromJson((JString)json["lastProof"]),
            };
        }

        static byte[] ProofFromJson(JString json)
            => json == null ? null : Convert.FromBase64String(json.AsString());
    }
}
