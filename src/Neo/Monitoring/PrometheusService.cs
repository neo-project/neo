// Copyright (C) 2015-2025 The Neo Project.
// 
// PrometheusService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.Plugins; // Added for Log
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics; // Added for Process metrics
using System.Net;
using System.Threading;
using System.Threading.Tasks;
// using Prometheus.SystemMetrics; // Required for system metrics collection - Commented out due to compatibility issues with netstandard2.1
using static Neo.Utility; // Added for Log

namespace Neo.Monitoring
{
    /// <summary>
    /// Provides Prometheus monitoring services for the Neo node.
    /// Collects metrics related to P2P network, mempool, RPC, consensus,
    /// performance, and system resources.
    /// </summary>
    public sealed class PrometheusService : IDisposable
    {
        private MetricServer? _metricServer;
        // private IDisposable? _systemMetricsCollector; // Commented out due to compatibility issues
        private Timer? _processMetricsTimer; // Timer for manual process metrics
        private readonly TimeSpan _processMetricsInterval = TimeSpan.FromSeconds(15); // Update interval
        private bool _enabled = false;

        // Use Lazy<T> for thread-safe lazy initialization
        private static readonly Lazy<PrometheusService> LazyInstance = new(() => new PrometheusService());

        /// <summary>
        /// Gets the singleton instance of the PrometheusService.
        /// Note: This instance is only fully initialized if Prometheus is enabled via configuration.
        /// Accessing this property ensures the singleton is created, but Start must be called to enable monitoring.
        /// </summary>
        public static PrometheusService Instance => LazyInstance.Value;

        /// <summary>
        /// Indicates whether Prometheus monitoring is currently enabled and running.
        /// </summary>
        public bool IsEnabled => _enabled;

        // ======================= Metric Definitions =======================
        // Using NonCapturingLazyInitializer for thread-safe lazy initialization.

        // --- P2P Metrics ---
        public readonly Lazy<Gauge> P2PConnections = NonCapturingLazyInitializer.CreateGauge(
            "neo_p2p_connections_total", "Current number of active P2P connections.");
        public readonly Lazy<Counter> P2PMessagesReceived = NonCapturingLazyInitializer.CreateCounter(
            "neo_p2p_messages_received_total", "Total number of P2P messages received.", "type");
        public readonly Lazy<Counter> P2PMessagesSent = NonCapturingLazyInitializer.CreateCounter(
            "neo_p2p_messages_sent_total", "Total number of P2P messages sent.", "type");

        // --- Mempool Metrics ---
        public readonly Lazy<Gauge> MempoolTransactions = NonCapturingLazyInitializer.CreateGauge(
             "neo_mempool_transactions_total", "Number of transactions currently in the mempool.");
        public readonly Lazy<Gauge> MempoolSizeBytes = NonCapturingLazyInitializer.CreateGauge(
            "neo_mempool_size_bytes", "Total size of transactions currently in the mempool.");
        public readonly Lazy<Counter> MempoolTransactionsAdded = NonCapturingLazyInitializer.CreateCounter(
            "neo_mempool_transactions_added_total", "Total number of transactions successfully added to the mempool.");
        public readonly Lazy<Counter> MempoolTransactionsRejected = NonCapturingLazyInitializer.CreateCounter(
            "neo_mempool_transactions_rejected_total", "Total number of transactions rejected from the mempool.", "reason"); // Added reason label

        // --- RPC Metrics ---
        public readonly Lazy<Counter> RpcRequests = NonCapturingLazyInitializer.CreateCounter(
            "neo_rpc_requests_total", "Total number of RPC requests handled.", "method", "status"); // Added status label (success/error)
        public readonly Lazy<Histogram> RpcRequestDuration = NonCapturingLazyInitializer.CreateHistogram(
            "neo_rpc_request_duration_seconds", "Histogram of RPC request duration in seconds.",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 15), LabelNames = new[] { "method" } });

        // --- Node/System Metrics ---
        // System metrics (CPU, Memory, TCP connections) are collected automatically if enabled.
        public readonly Lazy<Gauge> NodeBlockHeight = NonCapturingLazyInitializer.CreateGauge(
            "neo_node_block_height", "Current validated block height of the node.");
        // Removed NodeBlockTransactions and NodeBlockSize as these are better tracked per block during processing/consensus
        public readonly Lazy<Gauge> ProcessWorkingSet = NonCapturingLazyInitializer.CreateGauge(
            "neo_process_working_set_bytes", "Process working set memory in bytes.");
        public readonly Lazy<Gauge> ProcessCpuTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_process_cpu_seconds_total", "Total process CPU time consumed in seconds since process start.");

        // --- Consensus Metrics ---
        public readonly Lazy<Gauge> ConsensusHeight = NonCapturingLazyInitializer.CreateGauge(
             "neo_consensus_height", "Current block height the consensus service is working on.");
        public readonly Lazy<Gauge> ConsensusView = NonCapturingLazyInitializer.CreateGauge(
            "neo_consensus_view", "Current view number in the consensus service.");
        public readonly Lazy<Counter> ConsensusMessagesReceived = NonCapturingLazyInitializer.CreateCounter(
            "neo_consensus_messages_received_total", "Total number of consensus messages received.", "type");
        public readonly Lazy<Histogram> ConsensusBlockGenerationDuration = NonCapturingLazyInitializer.CreateHistogram(
             "neo_consensus_block_generation_duration_seconds", "Histogram of time taken to generate a block during consensus.",
             new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) }); // Added buckets
        public readonly Lazy<Counter> ConsensusNewBlockPersisted = NonCapturingLazyInitializer.CreateCounter(
            "neo_consensus_new_block_persisted_total", "Total number of new blocks persisted via consensus.");

        // --- Execution Metrics ---
        public readonly Lazy<Histogram> TransactionExecutionDuration = NonCapturingLazyInitializer.CreateHistogram(
             "neo_transaction_execution_duration_seconds", "Histogram of time taken to execute a transaction in the VM.",
             new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) }); // Added buckets
        public readonly Lazy<Histogram> BlockProcessingDuration = NonCapturingLazyInitializer.CreateHistogram(
            "neo_block_processing_duration_seconds", "Histogram of time taken to process and persist a block (verification, commit, events).",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.01, 2, 15) }); // Added buckets
        public readonly Lazy<Gauge> BlockProcessingTransactions = NonCapturingLazyInitializer.CreateGauge(
             "neo_block_processing_transactions_total", "Number of transactions in the last processed block.");
        public readonly Lazy<Gauge> BlockProcessingSizeBytes = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_processing_size_bytes", "Size of the last processed block in bytes.");
        public readonly Lazy<Gauge> BlockGasGenerated = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_gas_generated_total", "GAS generated in the last processed block (in 10^-8 units).");
        public readonly Lazy<Gauge> BlockSystemFee = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_system_fee_total", "System fee collected in the last processed block (in 10^-8 units).");
        public readonly Lazy<Gauge> BlockNetworkFee = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_network_fee_total", "Network fee collected in the last processed block (in 10^-8 units).");

        /// <summary>
        /// Private constructor for singleton pattern. Use Instance property.
        /// </summary>
        private PrometheusService()
        {
            // Initialization is deferred to Start method based on configuration.
        }

        /// <summary>
        /// Starts the Prometheus metrics server if enabled via configuration.
        /// </summary>
        /// <param name="settings">Prometheus settings containing host and port.</param>
        public void Start(PrometheusSettings? settings)
        {
            // Use the Lazy<T> lock implicitly via Instance access, but add explicit lock for mutation safety
            lock (LazyInstance) // Lock on the lazy object itself for mutation
            {
                if (_enabled) { Log(nameof(PrometheusService), LogLevel.Warning, "Prometheus service already started."); return; }
                if (settings == null || !settings.Enabled) { _enabled = false; Log(nameof(PrometheusService), LogLevel.Info, "Prometheus monitoring is disabled."); return; }

                try
                {
                    // _systemMetricsCollector = SystemMetrics.StartCollecting(); // Commented out due to compatibility issues
                    // Log(nameof(PrometheusService), LogLevel.Info, "Started collecting system metrics for Prometheus."); // Commented out

                    _metricServer = new MetricServer(hostname: settings.Host, port: settings.Port);
                    _metricServer.Start();

                    // Start manual process metrics collection
                    _processMetricsTimer = new Timer(UpdateProcessMetrics, null, _processMetricsInterval, _processMetricsInterval);
                    Log(nameof(PrometheusService), LogLevel.Info, $"Started manual process metrics collection (Update interval: {_processMetricsInterval.TotalSeconds}s).");

                    _enabled = true;
                    Log(nameof(PrometheusService), LogLevel.Info, $"Prometheus metrics server started at http://{settings.Host}:{settings.Port}/metrics");
                }
                catch (Exception ex)
                {
                    _enabled = false;
                    Log(nameof(PrometheusService), LogLevel.Error, $"Failed to start Prometheus service: {ex.GetBaseException().Message}");
                    _metricServer?.Stop();
                    _processMetricsTimer?.Dispose(); // Dispose timer on startup failure
                    // _metricServer?.Dispose(); // Dispose not available in prometheus-net v6.0.0
                    // _systemMetricsCollector?.Dispose(); // Commented out due to compatibility issues
                    _metricServer = null; // Clear refs on failure
                    _processMetricsTimer = null;
                    // _systemMetricsCollector = null; // Commented out
                }
            }
        }

        #region Metric Recording Methods

        // These methods provide a safe way to interact with metrics,
        // checking if the service is enabled before accessing the Lazy<T>.Value.

        // --- P2P ---
        public void SetP2PConnections(long count)
        {
            if (!_enabled) return;
            P2PConnections.Value.Set(count);
        }

        public void IncP2PMessagesReceived(string type)
        {
            if (!_enabled) return;
            P2PMessagesReceived.Value.WithLabels(type ?? "unknown").Inc();
        }

        public void IncP2PMessagesSent(string type)
        {
            if (!_enabled) return;
            P2PMessagesSent.Value.WithLabels(type ?? "unknown").Inc();
        }

        // --- Mempool ---
        public void SetMempoolTransactions(long count)
        {
            if (!_enabled) return;
            MempoolTransactions.Value.Set(count);
        }

        public void SetMempoolSize(long bytes)
        {
            if (!_enabled) return;
            MempoolSizeBytes.Value.Set(bytes);
        }

        public void IncMempoolTransactionsAdded()
        {
            if (!_enabled) return;
            MempoolTransactionsAdded.Value.Inc();
        }

        public void IncMempoolTransactionsRejected(string reason)
        {
            if (!_enabled) return;
            MempoolTransactionsRejected.Value.WithLabels(reason ?? "unknown").Inc();
        }

        // --- RPC ---
        public void IncRpcRequests(string method, bool success)
        {
            if (!_enabled) return;
            RpcRequests.Value.WithLabels(method ?? "unknown", success ? "success" : "error").Inc();
        }

        // Use: using (PrometheusService.Instance.MeasureRpcRequestDuration(method)) { ... }
        public IDisposable MeasureRpcRequestDuration(string method)
        {
            return _enabled ? RpcRequestDuration.Value.WithLabels(method ?? "unknown").NewTimer() : NullDisposable.Instance;
        }

        // --- Node State ---
        public void SetNodeBlockHeight(uint height)
        {
            if (!_enabled) return;
            NodeBlockHeight.Value.Set(height);
        }

        // --- Consensus ---
        public void SetConsensusHeight(uint height)
        {
            if (!_enabled) return;
            ConsensusHeight.Value.Set(height);
        }

        public void SetConsensusView(byte view)
        {
            if (!_enabled) return;
            ConsensusView.Value.Set(view);
        }

        public void IncConsensusMessagesReceived(string type)
        {
            if (!_enabled) return;
            ConsensusMessagesReceived.Value.WithLabels(type ?? "unknown").Inc();
        }

        // Use: using (PrometheusService.Instance.MeasureConsensusBlockGeneration()) { ... }
        public IDisposable MeasureConsensusBlockGeneration()
        {
            return _enabled ? ConsensusBlockGenerationDuration.Value.NewTimer() : NullDisposable.Instance;
        }

        public void IncConsensusNewBlockPersisted()
        {
            if (!_enabled) return;
            ConsensusNewBlockPersisted.Value.Inc();
        }

        // --- Execution ---
        public void RecordTransactionExecutionTime(double seconds)
        {
            if (!_enabled) return;
            TransactionExecutionDuration.Value.Observe(seconds);
        }

        // Use: using (var timer = PrometheusService.Instance.MeasureBlockProcessing()) { ... timer.SetBlockDetails(...); ... return result; }
        public IBlockProcessingTimer MeasureBlockProcessing()
        {
            // Returns a timer that also allows setting block-specific gauges upon disposal
            return _enabled ? new BlockProcessingTimerImpl(
                                BlockProcessingDuration.Value,
                                BlockProcessingTransactions.Value,
                                BlockProcessingSizeBytes.Value,
                                BlockGasGenerated.Value,
                                BlockSystemFee.Value,
                                BlockNetworkFee.Value)
                            : NullBlockProcessingTimer.Instance;
        }

        // Interface for the block processing timer to allow setting details
        public interface IBlockProcessingTimer : IDisposable
        {
            /// <summary>
            /// Sets the details for the block being processed. Call this before the timer is disposed.
            /// </summary>
            /// <param name="transactionCount">Number of transactions in the block.</param>
            /// <param name="sizeBytes">Size of the block in bytes.</param>
            /// <param name="gasGenerated">Total GAS generated in the block (10^-8 units).</param>
            /// <param name="systemFee">Total system fee collected (10^-8 units).</param>
            /// <param name="networkFee">Total network fee collected (10^-8 units).</param>
            void SetBlockDetails(int transactionCount, long sizeBytes, long gasGenerated, long systemFee, long networkFee);
        }

        // Implementation of the block processing timer
        private sealed class BlockProcessingTimerImpl : IBlockProcessingTimer
        {
            private readonly IDisposable _histogramTimer; // Store the IDisposable timer
            private readonly Gauge _txGauge;
            private readonly Gauge _sizeGauge;
            private readonly Gauge _gasGeneratedGauge;
            private readonly Gauge _systemFeeGauge;
            private readonly Gauge _networkFeeGauge;
            private bool _disposed = false;
            private int _txCount = 0;
            private long _sizeBytes = 0;
            private long _gasGenerated = 0;
            private long _systemFee = 0;
            private long _networkFee = 0;

            public BlockProcessingTimerImpl(Histogram histogram,
                                          Gauge txGauge, Gauge sizeGauge,
                                          Gauge gasGeneratedGauge, Gauge systemFeeGauge, Gauge networkFeeGauge)
            {
                _histogramTimer = histogram.NewTimer();
                _txGauge = txGauge;
                _sizeGauge = sizeGauge;
                _gasGeneratedGauge = gasGeneratedGauge;
                _systemFeeGauge = systemFeeGauge;
                _networkFeeGauge = networkFeeGauge;
            }

            public void SetBlockDetails(int transactionCount, long sizeBytes, long gasGenerated, long systemFee, long networkFee)
            {
                if (_disposed) return;
                _txCount = transactionCount;
                _sizeBytes = sizeBytes;
                _gasGenerated = gasGenerated;
                _systemFee = systemFee;
                _networkFee = networkFee;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _txGauge.Set(_txCount);
                _sizeGauge.Set(_sizeBytes);
                _gasGeneratedGauge.Set(_gasGenerated);
                _systemFeeGauge.Set(_systemFee);
                _networkFeeGauge.Set(_networkFee);
                _histogramTimer.Dispose(); // Dispose the stored timer
                _disposed = true;
            }
        }

        // Null implementation for when Prometheus is disabled
        private sealed class NullBlockProcessingTimer : IBlockProcessingTimer
        {
            public static readonly NullBlockProcessingTimer Instance = new NullBlockProcessingTimer();
            private NullBlockProcessingTimer() { }
            // Updated signature to match interface
            public void SetBlockDetails(int transactionCount, long sizeBytes, long gasGenerated, long systemFee, long networkFee) { /* No-op */ }
            public void Dispose() { /* No-op */ }
        }

        #endregion // Metric Recording Methods

        #region Process Metrics Update

        /// <summary>
        /// Callback method for the process metrics timer. Updates process-related gauges.
        /// </summary>
        private void UpdateProcessMetrics(object? state)
        {
            if (!_enabled) return; // Don't update if service is stopping/stopped

            try
            {
                using var currentProcess = Process.GetCurrentProcess();
                currentProcess.Refresh(); // Refresh process stats

                // Update metrics - Accessing Lazy<T>.Value is thread-safe, .Set() is thread-safe
                ProcessWorkingSet.Value.Set(currentProcess.WorkingSet64);
                ProcessCpuTotal.Value.Set(currentProcess.TotalProcessorTime.TotalSeconds);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the timer thread
                Log(nameof(PrometheusService), LogLevel.Error, $"Failed to update process metrics: {ex.Message}");
            }
        }

        #endregion // Process Metrics Update

        /// <summary>
        /// Stops the metrics server and disposes resources.
        /// </summary>
        public void Dispose()
        {
            lock (LazyInstance) // Lock for mutation safety
            {
                if (!_enabled && _metricServer == null && _processMetricsTimer == null /* && _systemMetricsCollector == null */) return; // SystemMetrics part commented out

                Log(nameof(PrometheusService), LogLevel.Info, "Stopping Prometheus service...");

                // Stop and dispose the process metrics timer first
                _processMetricsTimer?.Dispose();
                _processMetricsTimer = null;

                _metricServer?.Stop();
                // _metricServer?.Dispose(); // Dispose not available in prometheus-net v6.0.0
                // _systemMetricsCollector?.Dispose(); // Commented out due to compatibility issues
                _enabled = false;
                _metricServer = null;
                // _systemMetricsCollector = null; // Commented out
                Log(nameof(PrometheusService), LogLevel.Info, "Prometheus service stopped.");
            }
        }

        // Helper class for cleaner disposal pattern when using timers/using blocks
        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();
            private NullDisposable() { } // Prevent external instantiation
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Configuration settings for the Prometheus service.
    /// </summary>
    public class PrometheusSettings
    {
        public bool Enabled { get; set; } = false; // Disabled by default
        public string Host { get; set; } = "127.0.0.1"; // Default to loopback
        public int Port { get; set; } = 9100; // Default Prometheus port often used
    }

    // Helper for lazy initialization of metrics without capturing 'this'
    internal static class NonCapturingLazyInitializer
    {
        private static readonly LazyThreadSafetyMode Mode = LazyThreadSafetyMode.ExecutionAndPublication;

        // Generic factory for any metric type if needed later
        // public static Lazy<T> Create<T>(Func<T> factory) where T : class
        // {
        //     return new Lazy<T>(factory, Mode);
        // }

        public static Lazy<Counter> CreateCounter(string name, string help, params string[] labelNames)
        {
            return new Lazy<Counter>(() => Metrics.CreateCounter(name, help, new CounterConfiguration
            {
                LabelNames = labelNames ?? Array.Empty<string>() // Ensure not null
            }), Mode);
        }

        public static Lazy<Gauge> CreateGauge(string name, string help, params string[] labelNames)
        {
            return new Lazy<Gauge>(() => Metrics.CreateGauge(name, help, new GaugeConfiguration
            {
                LabelNames = labelNames ?? Array.Empty<string>() // Ensure not null
            }), Mode);
        }

        public static Lazy<Histogram> CreateHistogram(string name, string help, HistogramConfiguration? configuration = null)
        {
            configuration ??= new HistogramConfiguration();
            // Ensure LabelNames is not null if provided within configuration
            if (configuration.LabelNames == null) configuration.LabelNames = Array.Empty<string>();

            return new Lazy<Histogram>(() => Metrics.CreateHistogram(name, help, configuration), Mode);
        }
    }
}
