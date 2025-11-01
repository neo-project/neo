// Copyright (C) 2015-2025 The Neo Project.
//
// MetricNames.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Constants for metric names used throughout the plugin
    /// </summary>
    public static class MetricNames
    {
        // Blockchain metrics
        public const string BlocksProcessedTotal = "neo.blocks.processed_total";
        public const string TransactionsProcessedTotal = "neo.transactions.processed_total";
        public const string ContractInvocationsTotal = "neo.contracts.invocations_total";
        public const string BlockProcessingTime = "neo.block.processing_time";
        public const string BlockchainHeight = "neo.blockchain.height";
        public const string BlockProcessingRate = "neo.block.processing_rate";
        public const string IsSyncing = "neo.blockchain.is_syncing";

        // MemPool metrics
        public const string MempoolSize = "neo.mempool.size";
        public const string MempoolVerifiedCount = "neo.mempool.verified_count";
        public const string MempoolUnverifiedCount = "neo.mempool.unverified_count";
        public const string MempoolMemoryBytes = "neo.mempool.memory_bytes";
        public const string MempoolConflictsTotal = "neo.mempool.conflicts_total";
        public const string MempoolBatchRemovedSize = "neo.mempool.batch_removed_size";
        public const string MempoolCapacityRatio = "neo.mempool.capacity_ratio";

        // Network/P2P metrics
        public const string P2PConnectedPeers = "neo.p2p.connected_peers";
        public const string P2PUnconnectedPeers = "neo.p2p.unconnected_peers";

        // Transaction metrics
        public const string TransactionVerificationFailuresTotal = "neo.transaction.verification_failures_total";

        // System metrics
        public const string ProcessCpuUsage = "process.cpu.usage";
        public const string SystemCpuUsage = "system.cpu.usage";
        public const string ProcessMemoryWorkingSet = "process.memory.working_set";
        public const string ProcessMemoryVirtual = "process.memory.virtual";
        public const string DotnetGcHeapSize = "dotnet.gc.heap_size";
        public const string ProcessThreadCount = "process.thread_count";
        public const string NodeStartTime = "neo.node.start_time";
        public const string NetworkId = "neo.network.id";
    }
}
