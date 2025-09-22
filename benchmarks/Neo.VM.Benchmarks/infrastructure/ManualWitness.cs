// Copyright (C) 2015-2025 The Neo Project.
//
// ManualWitness.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Lightweight <see cref="IVerifiable"/> implementation that advertises pre-defined witnesses.
    /// </summary>
    internal sealed class ManualWitness : IVerifiable
    {
        private readonly UInt160[] _hashes;

        public ManualWitness(params UInt160[] hashes)
        {
            _hashes = hashes ?? Array.Empty<UInt160>();
        }

        public int Size => 0;

        public Witness[] Witnesses { get; set; } = Array.Empty<Witness>();

        public void Deserialize(ref MemoryReader reader)
        {
        }

        public void DeserializeUnsigned(ref MemoryReader reader)
        {
        }

        public UInt160[] GetScriptHashesForVerifying(DataCache snapshot) => _hashes;

        public void Serialize(BinaryWriter writer)
        {
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
        }
    }
}
