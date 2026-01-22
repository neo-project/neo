// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.SignData.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;

namespace Neo.Benchmark
{
    public class Benchmarks_SignData
    {
        private static readonly Transaction s_tx = new()
        {
            Attributes = [],
            Script = Array.Empty<byte>(),
            Signers = [new Signer() { Account = UInt160.Zero, AllowedContracts = [], AllowedGroups = [], Rules = [], Scopes = WitnessScope.Global }],
            Witnesses = []
        };

        /// <summary>
        /// Gets the data of a <see cref="IVerifiable"/> object to be hashed.
        /// </summary>
        /// <param name="verifiable">The <see cref="IVerifiable"/> object to hash.</param>
        /// <param name="network">The magic number of the network.</param>
        /// <returns>The data to hash.</returns>
        public static byte[] GetSignDataV1(IVerifiable verifiable, uint network)
        {
            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);
            writer.Write(network);
            writer.Write(verifiable.Hash);
            writer.Flush();
            return ms.ToArray();
        }

        [Benchmark]
        public void TestOld()
        {
            GetSignDataV1(s_tx, 0);
        }

        [Benchmark]
        public void TestNew()
        {
            s_tx.GetSignData(0);
        }
    }
}
