// Copyright (C) 2015-2024 The Neo Project.
//
// RpcBlockHeader.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.Network.P2P.Payloads;

namespace Neo.Network.RPC.Models
{
    public class RpcBlockHeader
    {
        public Header Header { get; set; }

        public uint Confirmations { get; set; }

        public UInt256 NextBlockHash { get; set; }

        public JObject ToJson(ProtocolSettings protocolSettings)
        {
            JObject json = Header.ToJson(protocolSettings);
            json["confirmations"] = Confirmations;
            json["nextblockhash"] = NextBlockHash?.ToString();
            return json;
        }

        public static RpcBlockHeader FromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new RpcBlockHeader
            {
                Header = Utility.HeaderFromJson(json, protocolSettings),
                Confirmations = (uint)json["confirmations"].AsNumber(),
                NextBlockHash = json["nextblockhash"] is null ? null : UInt256.Parse(json["nextblockhash"].AsString())
            };
        }
    }
}
