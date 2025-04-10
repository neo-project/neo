// Copyright (C) 2015-2025 The Neo Project.
//
// PrometheusService.Metrics.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Prometheus;
using System;

namespace Neo.Monitoring
{
    public sealed partial class PrometheusService
    {
        // ======================= Metric Definitions =======================

        // --- Core Blockchain Metrics ---
        public readonly Lazy<Gauge> BlockchainBlockHeight = NonCapturingLazyInitializer.CreateGauge(
            "neo_blockchain_block_height", "Current validated block height of the node.");
        public readonly Lazy<Gauge> BlockchainSyncStatus = NonCapturingLazyInitializer.CreateGauge(
            "neo_blockchain_sync_status", "Sync progress from 0 (syncing) to 1 (synced).");
        public readonly Lazy<Gauge> BlockchainChainTipLag = NonCapturingLazyInitializer.CreateGauge(
            "neo_blockchain_chain_tip_lag", "Blocks behind the network chain tip.");

        // --- Node Performance Metrics ---
        public readonly Lazy<Gauge> NodeMemoryWorkingSetBytes = NonCapturingLazyInitializer.CreateGauge(
            "neo_node_memory_working_set_bytes", "Process working set memory in bytes.");
        public readonly Lazy<Gauge> NodeCpuSecondsTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_node_cpu_seconds_total", "Total process CPU time consumed in seconds since process start.");
        public readonly Lazy<Counter> NodeApiRequestsTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_node_api_requests_total", "Total number of RPC/API requests handled.", "method", "status");
        public readonly Lazy<Histogram> NodeApiRequestDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
            "neo_node_api_request_duration_seconds", "Histogram of RPC/API request duration in seconds.",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 15), LabelNames = new[] { "method" } });

        // --- Network Metrics ---
        public readonly Lazy<Gauge> NetworkPeersCount = NonCapturingLazyInitializer.CreateGauge(
            "neo_network_peers_count", "Current number of active P2P connections.");
        public readonly Lazy<Counter> NetworkP2PMessagesReceivedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_network_p2p_messages_received_total", "Total number of P2P messages received.", "type");
        public readonly Lazy<Counter> NetworkP2PMessagesSentTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_network_p2p_messages_sent_total", "Total number of P2P messages sent.", "type");

        // --- Transaction Pool (Mempool) Metrics ---
        public readonly Lazy<Gauge> MempoolSizeTransactions = NonCapturingLazyInitializer.CreateGauge(
             "neo_mempool_size_transactions", "Number of transactions currently in the mempool.");
        public readonly Lazy<Counter> MempoolTransactionsAddedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_mempool_transactions_added_total", "Total number of transactions successfully added to the mempool.");
        public readonly Lazy<Counter> MempoolTransactionsRejectedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_mempool_transactions_rejected_total", "Total number of transactions rejected from the mempool.", "reason");

        // --- Consensus Metrics (dBFT Specific) ---
        public readonly Lazy<Gauge> ConsensusCurrentHeight = NonCapturingLazyInitializer.CreateGauge(
             "neo_consensus_current_height", "Current block height the consensus service is working on.");
        public readonly Lazy<Gauge> ConsensusCurrentView = NonCapturingLazyInitializer.CreateGauge(
            "neo_consensus_current_view", "Current view number in the consensus service.");
        public readonly Lazy<Counter> ConsensusP2PMessagesReceivedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_consensus_p2p_messages_received_total", "Total number of consensus messages received.", "type");
        public readonly Lazy<Histogram> ConsensusBlockGenerationDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
             "neo_consensus_block_generation_duration_seconds", "Histogram of time taken to generate a block during consensus.",
             new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) });
        public readonly Lazy<Counter> ConsensusNewBlockPersistedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_consensus_new_block_persisted_total", "Total number of new blocks persisted via consensus.");

        // --- Validator Metrics (dBFT Specific) ---
        public readonly Lazy<Gauge> ValidatorActive = NonCapturingLazyInitializer.CreateGauge(
            "neo_validator_active", "Indicates if the node is currently an active consensus validator (1 if active, 0 otherwise).");
        public readonly Lazy<Counter> ValidatorMissedBlocksTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_validator_missed_blocks_total", "Total number of block proposals missed by this node when it was the primary validator.");

        // --- Execution & Block Processing Metrics ---
        public readonly Lazy<Histogram> TransactionExecutionDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
             "neo_transaction_execution_duration_seconds", "Histogram of time taken to execute a transaction in the VM.",
             new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) });
        public readonly Lazy<Histogram> BlockProcessingDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
            "neo_block_processing_duration_seconds", "Histogram of time taken to process and persist a block (verification, commit, events).",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.01, 2, 15) });
        public readonly Lazy<Gauge> BlockProcessingTransactionsTotal = NonCapturingLazyInitializer.CreateGauge(
             "neo_block_processing_transactions_total", "Number of transactions in the last processed block.");
        public readonly Lazy<Gauge> BlockProcessingSizeBytes = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_processing_size_bytes", "Size of the last processed block in bytes.");

        // --- N3 Economics Metrics ---
        public readonly Lazy<Gauge> BlockGasGeneratedTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_gas_generated_total", "GAS generated in the last processed block (in 10^-8 units).");
        public readonly Lazy<Gauge> BlockSystemFeeTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_system_fee_total", "System fee collected in the last processed block (in 10^-8 units).");
        public readonly Lazy<Gauge> BlockNetworkFeeTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_network_fee_total", "Network fee collected in the last processed block (in 10^-8 units).");

        // --- Security Metrics ---
        public readonly Lazy<Counter> FailedAuthenticationAttemptsTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_failed_authentication_attempts_total", "Total number of failed authentication attempts.", "service");
        public readonly Lazy<Counter> InvalidP2PMessageCountTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_invalid_p2p_message_count_total", "Total number of invalid P2P messages received.", "reason");
        public readonly Lazy<Counter> UnexpectedShutdownsTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_unexpected_shutdowns_total", "Total number of unexpected node shutdowns detected (e.g., via recovery).", "reason");
    }
}
