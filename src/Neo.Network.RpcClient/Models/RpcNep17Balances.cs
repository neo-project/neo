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

namespace Neo.Network.RPC.Models
{
    public class RpcNep17Balances
    {
        public UInt160 UserScriptHash { get; set; }

        public List<RpcNep17Balance> Balances { get; set; }

        public JObject ToJson(ProtocolSettings protocolSettings)
        {
            return new()
            {
                ["balance"] = Balances.Select(p => p.ToJson()).ToArray(),
                ["address"] = UserScriptHash.ToAddress(protocolSettings.AddressVersion)
            };
        }

        public static RpcNep17Balances FromJson(JObject json, ProtocolSettings protocolSettings)
        {
            return new()
            {
                Balances = ((JArray)json["balance"]).Select(p => RpcNep17Balance.FromJson((JObject)p, protocolSettings)).ToList(),
                UserScriptHash = json["address"].ToScriptHash(protocolSettings)
            };
        }
    }

    public class RpcNep17Balance
    {
        public UInt160 AssetHash { get; set; }

        public BigInteger Amount { get; set; }

        public uint LastUpdatedBlock { get; set; }

        public JObject ToJson()
        {
            return new()
            {
                ["assethash"] = AssetHash.ToString(),
                ["amount"] = Amount.ToString(),
                ["lastupdatedblock"] = LastUpdatedBlock
            };
        }

        public static RpcNep17Balance FromJson(JObject json, ProtocolSettings protocolSettings)
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
