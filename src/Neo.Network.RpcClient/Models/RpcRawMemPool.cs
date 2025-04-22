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

namespace Neo.Network.RPC.Models
{
    public class RpcRawMemPool
    {
        public uint Height { get; set; }

        public List<UInt256> Verified { get; set; }

        public List<UInt256> UnVerified { get; set; }

        public JObject ToJson()
        {
            JObject json = new();
            json["height"] = Height;
            json["verified"] = new JArray(Verified.Select(p => (JToken)p.ToString()));
            json["unverified"] = new JArray(UnVerified.Select(p => (JToken)p.ToString()));
            return json;
        }

        public static RpcRawMemPool FromJson(JObject json)
        {
            return new RpcRawMemPool
            {
                Height = uint.Parse(json["height"].AsString()),
                Verified = ((JArray)json["verified"]).Select(p => UInt256.Parse(p.AsString())).ToList(),
                UnVerified = ((JArray)json["unverified"]).Select(p => UInt256.Parse(p.AsString())).ToList()
            };
        }
    }
}
