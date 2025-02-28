// Copyright (C) 2015-2025 The Neo Project.
//
// ProtocolSettingsModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Interfaces;
using Neo.Cryptography.ECC;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Neo.Build.Core.Models
{
    public class ProtocolSettingsModel : JsonModel, IConvertToObject<ProtocolSettings>
    {
        public uint Network { get; set; }

        public byte AddressVersion { get; set; }

        public uint MillisecondsPerBlock { get; set; }

        public uint MaxTransactionsPerBlock { get; set; }

        public int MemoryPoolMaxTransactions { get; set; }

        public uint MaxTraceableBlocks { get; set; }

        public ulong InitialGasDistribution { get; set; }

        public IDictionary<Hardfork, uint>? Hardforks { get; set; }

        public ICollection<ECPoint>? StandbyCommittee { get; set; }

        public int ValidatorsCount { get; set; }

        public ICollection<string>? SeedList { get; set; }

        public ProtocolSettings ToObject() =>
            ProtocolSettings.Custom with
            {
                Network = Network,
                AddressVersion = AddressVersion,
                MillisecondsPerBlock = MillisecondsPerBlock,
                MaxTransactionsPerBlock = MaxTransactionsPerBlock,
                MemoryPoolMaxTransactions = MemoryPoolMaxTransactions,
                MaxTraceableBlocks = MaxTraceableBlocks,
                InitialGasDistribution = InitialGasDistribution,
                Hardforks = Hardforks!.ToImmutableDictionary(),
                StandbyCommittee = [.. StandbyCommittee!],
                SeedList = [.. SeedList!],
            };
    }
}
