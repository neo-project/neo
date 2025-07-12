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

using Neo.Build.Core.Interfaces;

namespace Neo.Build.ToolSet.Options
{
    internal class ProtocolOptions : IConvertToObject<ProtocolSettings>
    {
        public uint Network { get; set; }

        public byte AddressVersion { get; set; }

        public uint MillisecondsPerBlock { get; set; }

        public uint MaxTransactionsPerBlock { get; set; }

        public int MemoryPoolMaxTransactions { get; set; }

        public uint MaxTraceableBlocks { get; set; }

        public ulong InitialGasDistribution { get; set; }

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
            };
    }
}
