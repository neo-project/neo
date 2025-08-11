// Copyright (C) 2015-2025 The Neo Project.
//
// BlockchainMetrics.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Blockchain metrics snapshot
    /// </summary>
    public class BlockchainMetrics
    {
        public DateTime Timestamp { get; set; }
        public uint CurrentHeight { get; set; }
        public bool IsSyncing { get; set; }
        public int NetworkId { get; set; }
    }
}
