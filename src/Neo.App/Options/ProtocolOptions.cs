// Copyright (C) 2015-2025 The Neo Project.
//
// ProtocolOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.App.Interfaces;
using Neo.Cryptography.ECC;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Neo.App.Options
{
    internal sealed class ProtocolOptions : IConvertObject<ProtocolSettings>
    {
        public uint Network { get; set; }

        public byte AddressVersion { get; set; }

        public ECPoint[] StandbyCommittee { get; set; } = [];

        public int ValidatorsCount { get; set; }

        public string[] SeedList { get; set; } = [];

        public uint MillisecondsPerBlock { get; set; }

        public uint MaxValidUntilBlockIncrement { get; set; }

        public uint MaxTransactionsPerBlock { get; set; }

        public int MemoryPoolMaxTransactions { get; set; }

        public uint MaxTraceableBlocks { get; set; }

        public Dictionary<Hardfork, uint> HardForks { get; set; } = [];

        public ulong InitialGasDistribution { get; set; }

        public ProtocolSettings ToObject() =>
            ProtocolSettings.Default with
            {
                Network = Network,
                AddressVersion = AddressVersion,
                MillisecondsPerBlock = MillisecondsPerBlock,
                MaxTransactionsPerBlock = MaxTransactionsPerBlock,
                MaxTraceableBlocks = MaxTraceableBlocks,
                MaxValidUntilBlockIncrement = MaxValidUntilBlockIncrement,
                MemoryPoolMaxTransactions = MemoryPoolMaxTransactions,
                InitialGasDistribution = InitialGasDistribution,
                Hardforks = HardForks.ToImmutableDictionary(),
                ValidatorsCount = ValidatorsCount,
                StandbyCommittee = [.. StandbyCommittee],
                SeedList = SeedList,
            };
    }
}
