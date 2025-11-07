// Copyright (C) 2015-2025 The Neo Project.
//
// MemPoolMetrics.cs file belongs to the neo project and is free
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
    /// Memory pool metrics snapshot
    /// </summary>
    public class MemPoolMetrics
    {
        public DateTime Timestamp { get; set; }
        public int Count { get; set; }
        public int VerifiedCount { get; set; }
        public int UnverifiedCount { get; set; }
        public int Capacity { get; set; }
        public double CapacityRatio { get; set; }
        public long EstimatedMemoryBytes { get; set; }
    }
}
