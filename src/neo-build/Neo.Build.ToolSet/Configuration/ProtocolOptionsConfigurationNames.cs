// Copyright (C) 2015-2025 The Neo Project.
//
// ProtocolOptionsConfigurationNames.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Build.ToolSet.Configuration
{
    internal class ProtocolOptionsConfigurationNames
    {
        public static readonly string NetworkKey = "PROTOCOL:NETWORK";
        public static readonly string AddressVersionKey = "PROTOCOL:ADDRESSVERSION";
        public static readonly string MillisecondsPerBlockKey = "PROTOCOL:MILLISECONDSPERBLOCK";
        public static readonly string MaxTransactionsPerBlockKey = "PROTOCOL:MAXTRANSACTIONSPERBLOCK";
        public static readonly string MemoryPoolMaxTransactionsKey = "PROTOCOL:MEMORYPOOLMAXTRANSACTIONS";
        public static readonly string MaxTraceableBlocksKey = "PROTOCOL:MAXTRACEABLEBLOCKS";
        public static readonly string InitialGasDistributionKey = "PROTOCOL:INITIALGASDISTRIBUTION";
    }
}
