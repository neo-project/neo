// Copyright (C) 2015-2025 The Neo Project.
//
// ProtocolConfigurationNames.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.App.Configuration
{
    internal class ProtocolConfigurationNames
    {
        public static readonly string SectionName = "ProtocolConfiguration";

        public static readonly string NetworkKey = $"{SectionName}:Network";
        public static readonly string AddressVersionKey = $"{SectionName}:AddressVersion";
        public static readonly string MillisecondsPerBlockKey = $"{SectionName}:MillisecondsPerBlock";
        public static readonly string MaxTransactionsPerBlockKey = $"{SectionName}:MaxTransactionsPerBlock";
        public static readonly string MemoryPoolMaxTransactionsKey = $"{SectionName}:MemoryPoolMaxTransactions";
        public static readonly string MaxTraceableBlocksKey = $"{SectionName}:MaxTraceableBlocks";
        public static readonly string HardForksKey = $"{SectionName}:Hardforks";
        public static readonly string InitialGasDistributionKey = $"{SectionName}:InitialGasDistribution";
        public static readonly string ValidatorsCountKey = $"{SectionName}:ValidatorsCount";
        public static readonly string StandbyCommitteeKey = $"{SectionName}:StandbyCommittee";
        public static readonly string SeedListKey = $"{SectionName}:SeedList";
    }
}
