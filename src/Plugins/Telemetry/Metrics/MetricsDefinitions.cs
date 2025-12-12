// Copyright (C) 2015-2025 The Neo Project.
//
// MetricsDefinitions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Prometheus;

namespace Neo.Plugins.Telemetry.Metrics
{
    /// <summary>
    /// Centralized definitions for all Neo node metrics.
    /// Following Prometheus naming conventions: https://prometheus.io/docs/practices/naming/
    /// </summary>
    public static class MetricsDefinitions
    {
        private const string Prefix = "neo_";
        private static readonly string[] CommonLabels = ["node_id", "network"];

        #region Blockchain Metrics

        /// <summary>
        /// Current block height of the node.
        /// </summary>
        public static readonly Gauge BlockHeight = Prometheus.Metrics.CreateGauge(
            $"{Prefix}blockchain_height",
            "Current block height of the Neo node",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Header height (may be ahead of block height during sync).
        /// </summary>
        public static readonly Gauge HeaderHeight = Prometheus.Metrics.CreateGauge(
            $"{Prefix}blockchain_header_height",
            "Current header height of the Neo node",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Total number of blocks persisted.
        /// </summary>
        public static readonly Counter BlocksPersisted = Prometheus.Metrics.CreateCounter(
            $"{Prefix}blockchain_blocks_persisted_total",
            "Total number of blocks persisted",
            new CounterConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Time taken to persist the last block in milliseconds.
        /// </summary>
        public static readonly Histogram BlockPersistDuration = Prometheus.Metrics.CreateHistogram(
            $"{Prefix}blockchain_block_persist_duration_milliseconds",
            "Time taken to persist a block in milliseconds",
            new HistogramConfiguration
            {
                LabelNames = CommonLabels,
                Buckets = [10, 50, 100, 250, 500, 1000, 2500, 5000, 10000]
            });

        /// <summary>
        /// Number of transactions in the last persisted block.
        /// </summary>
        public static readonly Gauge BlockTransactionCount = Prometheus.Metrics.CreateGauge(
            $"{Prefix}blockchain_block_transactions",
            "Number of transactions in the last persisted block",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Total transactions processed.
        /// </summary>
        public static readonly Counter TransactionsProcessed = Prometheus.Metrics.CreateCounter(
            $"{Prefix}blockchain_transactions_processed_total",
            "Total number of transactions processed",
            new CounterConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Sync status: 1 = synced, 0 = syncing.
        /// </summary>
        public static readonly Gauge SyncStatus = Prometheus.Metrics.CreateGauge(
            $"{Prefix}blockchain_sync_status",
            "Sync status of the node (1 = synced, 0 = syncing)",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Blocks behind the network.
        /// </summary>
        public static readonly Gauge BlocksBehind = Prometheus.Metrics.CreateGauge(
            $"{Prefix}blockchain_blocks_behind",
            "Number of blocks behind the network",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Time since last block in seconds.
        /// </summary>
        public static readonly Gauge TimeSinceLastBlock = Prometheus.Metrics.CreateGauge(
            $"{Prefix}blockchain_time_since_last_block_seconds",
            "Time since the last block was received in seconds",
            new GaugeConfiguration { LabelNames = CommonLabels });

        #endregion

        #region Network Metrics

        /// <summary>
        /// Number of connected peers.
        /// </summary>
        public static readonly Gauge ConnectedPeers = Prometheus.Metrics.CreateGauge(
            $"{Prefix}network_peers_connected",
            "Number of connected peers",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Number of unconnected peers in the pool.
        /// </summary>
        public static readonly Gauge UnconnectedPeers = Prometheus.Metrics.CreateGauge(
            $"{Prefix}network_peers_unconnected",
            "Number of unconnected peers in the pool",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Total peer connections established.
        /// </summary>
        public static readonly Counter PeerConnectionsTotal = Prometheus.Metrics.CreateCounter(
            $"{Prefix}network_peer_connections_total",
            "Total number of peer connections established",
            new CounterConfiguration { LabelNames = [.. CommonLabels, "direction"] });

        /// <summary>
        /// Total peer disconnections.
        /// </summary>
        public static readonly Counter PeerDisconnectionsTotal = Prometheus.Metrics.CreateCounter(
            $"{Prefix}network_peer_disconnections_total",
            "Total number of peer disconnections",
            new CounterConfiguration { LabelNames = [.. CommonLabels, "reason"] });

        /// <summary>
        /// Messages received by type.
        /// </summary>
        public static readonly Counter MessagesReceived = Prometheus.Metrics.CreateCounter(
            $"{Prefix}network_messages_received_total",
            "Total number of messages received by type",
            new CounterConfiguration { LabelNames = [.. CommonLabels, "message_type"] });

        /// <summary>
        /// Messages sent by type.
        /// </summary>
        public static readonly Counter MessagesSent = Prometheus.Metrics.CreateCounter(
            $"{Prefix}network_messages_sent_total",
            "Total number of messages sent by type",
            new CounterConfiguration { LabelNames = [.. CommonLabels, "message_type"] });

        /// <summary>
        /// Bytes received from network.
        /// </summary>
        public static readonly Counter BytesReceived = Prometheus.Metrics.CreateCounter(
            $"{Prefix}network_bytes_received_total",
            "Total bytes received from network",
            new CounterConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Bytes sent to network.
        /// </summary>
        public static readonly Counter BytesSent = Prometheus.Metrics.CreateCounter(
            $"{Prefix}network_bytes_sent_total",
            "Total bytes sent to network",
            new CounterConfiguration { LabelNames = CommonLabels });

        #endregion

        #region Mempool Metrics

        /// <summary>
        /// Current number of transactions in the memory pool.
        /// </summary>
        public static readonly Gauge MempoolSize = Prometheus.Metrics.CreateGauge(
            $"{Prefix}mempool_transactions",
            "Current number of transactions in the memory pool",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Number of verified transactions in the memory pool.
        /// </summary>
        public static readonly Gauge MempoolVerifiedCount = Prometheus.Metrics.CreateGauge(
            $"{Prefix}mempool_verified_transactions",
            "Number of verified transactions in the memory pool",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Number of unverified transactions in the memory pool.
        /// </summary>
        public static readonly Gauge MempoolUnverifiedCount = Prometheus.Metrics.CreateGauge(
            $"{Prefix}mempool_unverified_transactions",
            "Number of unverified transactions in the memory pool",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Memory pool capacity.
        /// </summary>
        public static readonly Gauge MempoolCapacity = Prometheus.Metrics.CreateGauge(
            $"{Prefix}mempool_capacity",
            "Maximum capacity of the memory pool",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Memory pool utilization percentage.
        /// </summary>
        public static readonly Gauge MempoolUtilization = Prometheus.Metrics.CreateGauge(
            $"{Prefix}mempool_utilization_ratio",
            "Memory pool utilization ratio (0-1)",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Total transactions added to mempool.
        /// </summary>
        public static readonly Counter MempoolTransactionsAdded = Prometheus.Metrics.CreateCounter(
            $"{Prefix}mempool_transactions_added_total",
            "Total transactions added to the memory pool",
            new CounterConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Total transactions removed from mempool.
        /// </summary>
        public static readonly Counter MempoolTransactionsRemoved = Prometheus.Metrics.CreateCounter(
            $"{Prefix}mempool_transactions_removed_total",
            "Total transactions removed from the memory pool",
            new CounterConfiguration { LabelNames = [.. CommonLabels, "reason"] });

        #endregion

        #region System Resource Metrics

        /// <summary>
        /// Process CPU usage percentage.
        /// </summary>
        public static readonly Gauge CpuUsage = Prometheus.Metrics.CreateGauge(
            $"{Prefix}system_cpu_usage_ratio",
            "Process CPU usage ratio (0-1)",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Process memory usage in bytes.
        /// </summary>
        public static readonly Gauge MemoryUsageBytes = Prometheus.Metrics.CreateGauge(
            $"{Prefix}system_memory_usage_bytes",
            "Process memory usage in bytes",
            new GaugeConfiguration { LabelNames = [.. CommonLabels, "type"] });

        /// <summary>
        /// GC collection count by generation.
        /// </summary>
        public static readonly Gauge GcCollectionCount = Prometheus.Metrics.CreateGauge(
            $"{Prefix}system_gc_collection_count",
            "GC collection count by generation",
            new GaugeConfiguration { LabelNames = [.. CommonLabels, "generation"] });

        /// <summary>
        /// Thread pool worker threads.
        /// </summary>
        public static readonly Gauge ThreadPoolWorkerThreads = Prometheus.Metrics.CreateGauge(
            $"{Prefix}system_threadpool_worker_threads",
            "Number of thread pool worker threads",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Thread pool completion port threads.
        /// </summary>
        public static readonly Gauge ThreadPoolCompletionPortThreads = Prometheus.Metrics.CreateGauge(
            $"{Prefix}system_threadpool_completion_port_threads",
            "Number of thread pool completion port threads",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Process uptime in seconds.
        /// </summary>
        public static readonly Gauge ProcessUptime = Prometheus.Metrics.CreateGauge(
            $"{Prefix}system_process_uptime_seconds",
            "Process uptime in seconds",
            new GaugeConfiguration { LabelNames = CommonLabels });

        #endregion

        #region Plugin Metrics

        /// <summary>
        /// Number of loaded plugins.
        /// </summary>
        public static readonly Gauge PluginsLoaded = Prometheus.Metrics.CreateGauge(
            $"{Prefix}plugins_loaded",
            "Number of loaded plugins",
            new GaugeConfiguration { LabelNames = CommonLabels });

        /// <summary>
        /// Plugin status (1 = running, 0 = stopped).
        /// </summary>
        public static readonly Gauge PluginStatus = Prometheus.Metrics.CreateGauge(
            $"{Prefix}plugin_status",
            "Plugin status (1 = running, 0 = stopped)",
            new GaugeConfiguration { LabelNames = [.. CommonLabels, "plugin_name"] });

        #endregion

        #region Node Info Metrics

        /// <summary>
        /// Node information (version, etc.) as labels.
        /// </summary>
        public static readonly Gauge NodeInfo = Prometheus.Metrics.CreateGauge(
            $"{Prefix}node_info",
            "Node information",
            new GaugeConfiguration { LabelNames = [.. CommonLabels, "version", "protocol_version"] });

        /// <summary>
        /// Node start timestamp.
        /// </summary>
        public static readonly Gauge NodeStartTime = Prometheus.Metrics.CreateGauge(
            $"{Prefix}node_start_time_seconds",
            "Node start time as Unix timestamp",
            new GaugeConfiguration { LabelNames = CommonLabels });

        #endregion
    }
}
