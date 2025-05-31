// Copyright (C) 2015-2025 The Neo Project.
//
// RpcStateRoot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using Neo.Network.P2P.Payloads;
using System.Linq;

namespace Neo.Network.RPC.Models
{
    public class RpcStateRoot
    {
        public byte Version;
        public uint Index;
        public UInt256 RootHash;
        public Witness Witness;

        public static RpcStateRoot FromJson(JObject json)
        {
            return new RpcStateRoot
            {
                Version = (byte)json["version"].AsNumber(),
                Index = (uint)json["index"].AsNumber(),
                RootHash = UInt256.Parse(json["roothash"].AsString()),
                Witness = ((JArray)json["witnesses"]).Select(p => Utility.WitnessFromJson((JObject)p)).FirstOrDefault()
            };
        }
    }
}
