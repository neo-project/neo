// Copyright (C) 2015-2025 The Neo Project.
//
// RpcNep17Balances.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.Wallets;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcNep17Balances
    {
        public UInt160 UserScriptHash { get; set; }

        public List<RpcNep17Balance> Balances { get; set; }

        public JsonObject ToJson(ProtocolSettings protocolSettings)
        {
            return new()
            {
                ["balance"] = new JsonArray(Balances.Select(p => p.ToJson()).ToArray()),
                ["address"] = UserScriptHash.ToAddress(protocolSettings.AddressVersion)
            };
        }

        public static RpcNep17Balances FromJson(JsonObject json, ProtocolSettings protocolSettings)
        {
            return new()
            {
                Balances = ((JsonArray)json["balance"]).Select(p => RpcNep17Balance.FromJson((JsonObject)p, protocolSettings)).ToList(),
                UserScriptHash = json["address"].ToScriptHash(protocolSettings)
            };
        }
    }

    public class RpcNep17Balance
    {
        public UInt160 AssetHash { get; set; }

        public BigInteger Amount { get; set; }

        public uint LastUpdatedBlock { get; set; }

        public JsonObject ToJson()
        {
            return new()
            {
                ["assethash"] = AssetHash.ToString(),
                ["amount"] = Amount.ToString(),
                ["lastupdatedblock"] = LastUpdatedBlock
            };
        }

        public static RpcNep17Balance FromJson(JsonObject json, ProtocolSettings protocolSettings)
        {
            return new()
            {
                AssetHash = json["assethash"].ToScriptHash(protocolSettings),
                Amount = BigInteger.Parse(json["amount"].AsString()),
                LastUpdatedBlock = (uint)json["lastupdatedblock"].AsNumber()
            };
        }
    }
}
