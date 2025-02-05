// Copyright (C) 2015-2025 The Neo Project.
//
// RpcContractState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;

namespace Neo.Network.RPC.Models
{
    public class RpcContractState
    {
        public ContractState ContractState { get; set; }

        public JObject ToJson()
        {
            return ContractState.ToJson();
        }

        public static RpcContractState FromJson(JObject json)
        {
            return new RpcContractState
            {
                ContractState = new ContractState
                {
                    Id = (int)json["id"].AsNumber(),
                    UpdateCounter = (ushort)json["updatecounter"].AsNumber(),
                    Hash = UInt160.Parse(json["hash"].AsString()),
                    Nef = RpcNefFile.FromJson((JObject)json["nef"]),
                    Manifest = ContractManifest.FromJson((JObject)json["manifest"])
                }
            };
        }
    }
}
