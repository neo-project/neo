// Copyright (C) 2015-2025 The Neo Project.
//
// IMemPoolMetricsHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;

namespace Neo.IEventHandlers
{
    /// <summary>
    /// Interface for plugins that need to collect memory pool metrics
    /// </summary>
    public interface IMemPoolMetricsHandler
    {
        /// <summary>
        /// Called periodically with memory pool statistics
        /// </summary>
        /// <param name="memPool">The memory pool instance</param>
        /// <param name="stats">Current memory pool statistics</param>
        void MemPool_StatsSnapshot_Handler(MemoryPool memPool, MemPoolStats stats);
    }

    /// <summary>
    /// Memory pool statistics snapshot
    /// </summary>
    public class MemPoolStats
    {
        public int Count { get; set; }
        public int VerifiedCount { get; set; }
        public int UnverifiedCount { get; set; }
        public int Capacity { get; set; }
        public long TotalMemoryBytes { get; set; }
        public int ConflictsCount { get; set; }
        public int LastBatchRemovedCount { get; set; }
    }
}
