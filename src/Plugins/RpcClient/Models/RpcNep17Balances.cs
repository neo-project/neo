// Copyright (C) 2015-2025 The Neo Project.
//
// RpcNep17Balances.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
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
            JObject json = new();
            json["balance"] = Balances.Select(p => p.ToJson()).ToArray();
            json["address"] = UserScriptHash.ToAddress(protocolSettings.AddressVersion);
            return json;
        }

        public static RpcNep17Balances FromJson(JObject json, ProtocolSettings protocolSettings)
        {
            RpcNep17Balances nep17Balance = new()
            {
                Balances = ((JArray)json["balance"]).Select(p => RpcNep17Balance.FromJson((JObject)p, protocolSettings)).ToList(),
                UserScriptHash = json["address"].ToScriptHash(protocolSettings)
            };
            return nep17Balance;
        }
    }

    public class RpcNep17Balance
    {
        public UInt160 AssetHash { get; set; }

        public BigInteger Amount { get; set; }

        public uint LastUpdatedBlock { get; set; }

        public JObject ToJson()
        {
            JObject json = new();
            json["assethash"] = AssetHash.ToString();
            json["amount"] = Amount.ToString();
            json["lastupdatedblock"] = LastUpdatedBlock;
            return json;
        }

        public static RpcNep17Balance FromJson(JObject json, ProtocolSettings protocolSettings)
        {
            RpcNep17Balance balance = new()
            {
                AssetHash = json["assethash"].ToScriptHash(protocolSettings),
                Amount = BigInteger.Parse(json["amount"].AsString()),
                LastUpdatedBlock = (uint)json["lastupdatedblock"].AsNumber()
            };
            return balance;
        }
    }
}
