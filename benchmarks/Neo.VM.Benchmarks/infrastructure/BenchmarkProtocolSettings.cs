// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkProtocolSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Benchmark.Infrastructure
{
    internal static class BenchmarkProtocolSettings
    {
        private static readonly IReadOnlyList<ECPoint> s_fallbackValidators = CreateFallbackValidators();
        private static readonly IReadOnlyList<ECPoint> s_fallbackCommittee = s_fallbackValidators;

        public static IReadOnlyList<ECPoint> StandbyValidators => ProtocolSettings.Default.StandbyValidators.Count > 0
            ? ProtocolSettings.Default.StandbyValidators
            : s_fallbackValidators;

        public static IReadOnlyList<ECPoint> StandbyCommittee => ProtocolSettings.Default.StandbyCommittee.Count > 0
            ? ProtocolSettings.Default.StandbyCommittee
            : s_fallbackCommittee;

        public static int ValidatorsCount => ProtocolSettings.Default.ValidatorsCount > 0
            ? ProtocolSettings.Default.ValidatorsCount
            : StandbyValidators.Count;

        public static UInt160 GetCommitteeAddress()
        {
            return Contract.GetBFTAddress(StandbyValidators);
        }

        public static ProtocolSettings ResolveSettings(ProtocolSettings? candidate = null)
        {
            var baseSettings = candidate ?? ProtocolSettings.Default;
            if (baseSettings.StandbyCommittee.Count > 0 && baseSettings.ValidatorsCount > 0)
                return baseSettings;

            var committee = s_fallbackCommittee.Select(static p => (ECPoint)p).ToArray();
            var validatorsCount = committee.Length;

            return new ProtocolSettings
            {
                Network = baseSettings.Network,
                AddressVersion = baseSettings.AddressVersion,
                StandbyCommittee = committee,
                ValidatorsCount = validatorsCount,
                SeedList = baseSettings.SeedList.ToArray(),
                MillisecondsPerBlock = baseSettings.MillisecondsPerBlock,
                MaxValidUntilBlockIncrement = baseSettings.MaxValidUntilBlockIncrement,
                MaxTransactionsPerBlock = baseSettings.MaxTransactionsPerBlock,
                MemoryPoolMaxTransactions = baseSettings.MemoryPoolMaxTransactions,
                MaxTraceableBlocks = baseSettings.MaxTraceableBlocks,
                InitialGasDistribution = baseSettings.InitialGasDistribution,
                Hardforks = baseSettings.Hardforks
            };
        }

        private static IReadOnlyList<ECPoint> CreateFallbackValidators()
        {
            var curve = ECCurve.Secp256r1;
            return new[]
            {
                Multiply(curve.G, 1u),
                Multiply(curve.G, 2u),
                Multiply(curve.G, 3u),
                Multiply(curve.G, 5u),
                Multiply(curve.G, 7u),
                Multiply(curve.G, 11u),
                Multiply(curve.G, 13u)
            };
        }

        private static ECPoint Multiply(ECPoint point, uint scalar)
        {
            var buffer = new byte[32];
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(28), scalar);
            return point * buffer;
        }
    }
}
