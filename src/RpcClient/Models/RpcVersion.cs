// Copyright (C) 2015-2025 The Neo Project.
//
// RpcVersion.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Neo.Network.RPC.Models
{
    public class RpcVersion
    {
        public class RpcProtocol
        {
            public uint Network { get; set; }
            public int ValidatorsCount { get; set; }
            public uint MillisecondsPerBlock { get; set; }
            public uint MaxValidUntilBlockIncrement { get; set; }
            public uint MaxTraceableBlocks { get; set; }
            public byte AddressVersion { get; set; }
            public uint MaxTransactionsPerBlock { get; set; }
            public int MemoryPoolMaxTransactions { get; set; }
            public ulong InitialGasDistribution { get; set; }
            public IReadOnlyDictionary<Hardfork, uint> Hardforks { get; set; }
            public IReadOnlyList<string> SeedList { get; set; }
            public IReadOnlyList<ECPoint> StandbyCommittee { get; set; }

            public JsonObject ToJson()
            {
                return new()
                {
                    ["network"] = Network,
                    ["validatorscount"] = ValidatorsCount,
                    ["msperblock"] = MillisecondsPerBlock,
                    ["maxvaliduntilblockincrement"] = MaxValidUntilBlockIncrement,
                    ["maxtraceableblocks"] = MaxTraceableBlocks,
                    ["addressversion"] = AddressVersion,
                    ["maxtransactionsperblock"] = MaxTransactionsPerBlock,
                    ["memorypoolmaxtransactions"] = MemoryPoolMaxTransactions,
                    ["initialgasdistribution"] = InitialGasDistribution,
                    ["hardforks"] = new JsonArray(Hardforks.Select(s => new JsonObject()
                    {
                        ["name"] = StripPrefix(s.Key.ToString(), "HF_"), // Strip HF_ prefix.
                        ["blockheight"] = s.Value,
                    }).ToArray()),
                    ["standbycommittee"] = new JsonArray(StandbyCommittee.Select(u => (JsonNode)u.ToString()).ToArray()),
                    ["seedlist"] = new JsonArray(SeedList.Select(u => (JsonNode)u).ToArray())
                };
            }

            public static RpcProtocol FromJson(JsonObject json)
            {
                return new()
                {
                    Network = (uint)json["network"].AsNumber(),
                    ValidatorsCount = (int)json["validatorscount"].AsNumber(),
                    MillisecondsPerBlock = (uint)json["msperblock"].AsNumber(),
                    MaxValidUntilBlockIncrement = (uint)json["maxvaliduntilblockincrement"].AsNumber(),
                    MaxTraceableBlocks = (uint)json["maxtraceableblocks"].AsNumber(),
                    AddressVersion = (byte)json["addressversion"].AsNumber(),
                    MaxTransactionsPerBlock = (uint)json["maxtransactionsperblock"].AsNumber(),
                    MemoryPoolMaxTransactions = (int)json["memorypoolmaxtransactions"].AsNumber(),
                    InitialGasDistribution = (ulong)json["initialgasdistribution"].AsNumber(),
                    Hardforks = new Dictionary<Hardfork, uint>(((JsonArray)json["hardforks"]).Select(s =>
                    {
                        var name = s["name"].AsString();
                        // Add HF_ prefix to the hardfork response for proper Hardfork enum parsing.
                        var hardfork = Enum.Parse<Hardfork>(name.StartsWith("HF_") ? name : $"HF_{name}");
                        return new KeyValuePair<Hardfork, uint>(hardfork, (uint)s["blockheight"].AsNumber());
                    })),
                    SeedList = [.. ((JsonArray)json["seedlist"]).Select(s => s.AsString())],
                    StandbyCommittee = [.. ((JsonArray)json["standbycommittee"]).Select(s => ECPoint.Parse(s.AsString(), ECCurve.Secp256r1))]
                };
            }

            private static string StripPrefix(string s, string prefix)
            {
                return s.StartsWith(prefix) ? s.Substring(prefix.Length) : s;
            }
        }

        public int TcpPort { get; set; }

        public uint Nonce { get; set; }

        public string UserAgent { get; set; }

        public RpcProtocol Protocol { get; set; } = new();

        public JsonObject ToJson()
        {
            return new()
            {
                ["network"] = Protocol.Network, // Obsolete
                ["tcpport"] = TcpPort,
                ["nonce"] = Nonce,
                ["useragent"] = UserAgent,
                ["protocol"] = Protocol.ToJson()
            };
        }

        public static RpcVersion FromJson(JsonObject json)
        {
            return new()
            {
                TcpPort = (int)json["tcpport"].AsNumber(),
                Nonce = (uint)json["nonce"].AsNumber(),
                UserAgent = json["useragent"].AsString(),
                Protocol = RpcProtocol.FromJson((JsonObject)json["protocol"])
            };
        }
    }
}
