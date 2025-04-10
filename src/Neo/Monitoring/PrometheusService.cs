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

using Akka.Actor; // Added for IActorRef and Ask pattern
using Neo; // Added for NeoSystem access
using Neo.Ledger; // Added for Blockchain access
using Neo.Network.P2P; // Added for LocalNode message access
using Neo.Plugins; // Added for Log
using Neo.SmartContract.Native; // Added for NativeContract access
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics; // Added for Process metrics
using System.Net;
using System.Net.Http; // Added for HttpClient
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
        private Timer? _nodeStateMetricsTimer; // Timer for node state metrics (blockchain, network, etc.)
        private readonly TimeSpan _processMetricsInterval = TimeSpan.FromSeconds(15); // Update interval for process metrics
        private readonly TimeSpan _nodeStateMetricsInterval = TimeSpan.FromSeconds(5); // Update interval for node state
        private bool _enabled = false;

        // Use Lazy<T> for thread-safe lazy initialization
        private static readonly Lazy<PrometheusService> LazyInstance = new(() => new PrometheusService());

        // Store the NeoSystem instance
        private NeoSystem? _neoSystem;

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

        // ======================= Metric Definitions (Aligned with prometheus.md) =======================

        // --- Core Blockchain Metrics ---
        public readonly Lazy<Gauge> BlockchainBlockHeight = NonCapturingLazyInitializer.CreateGauge(
            "neo_blockchain_block_height", "Current validated block height of the node.");
        public readonly Lazy<Gauge> BlockchainSyncStatus = NonCapturingLazyInitializer.CreateGauge(
            "neo_blockchain_sync_status", "Sync progress from 0 (syncing) to 1 (synced)."); // Added based on doc
        public readonly Lazy<Gauge> BlockchainChainTipLag = NonCapturingLazyInitializer.CreateGauge(
            "neo_blockchain_chain_tip_lag", "Blocks behind the network chain tip."); // Added based on doc
        // Note: Many other core metrics like block time, propagation, finality, forks require deeper integration.

        // --- Node Performance Metrics ---
        public readonly Lazy<Gauge> NodeMemoryWorkingSetBytes = NonCapturingLazyInitializer.CreateGauge(
            "neo_node_memory_working_set_bytes", "Process working set memory in bytes."); // Renamed from ProcessWorkingSet
        public readonly Lazy<Gauge> NodeCpuSecondsTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_node_cpu_seconds_total", "Total process CPU time consumed in seconds since process start."); // Corrected back to Gauge
        public readonly Lazy<Counter> NodeApiRequestsTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_node_api_requests_total", "Total number of RPC/API requests handled.", "method", "status"); // Renamed from RpcRequests
        public readonly Lazy<Histogram> NodeApiRequestDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
            "neo_node_api_request_duration_seconds", "Histogram of RPC/API request duration in seconds.",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 15), LabelNames = new[] { "method" } }); // Renamed from RpcRequestDuration
        // Note: Other node metrics (heap, GC, disk, files) require deeper runtime/OS integration.

        // --- Network Metrics ---
        public readonly Lazy<Gauge> NetworkPeersCount = NonCapturingLazyInitializer.CreateGauge(
            "neo_network_peers_count", "Current number of active P2P connections."); // Renamed from P2PConnections
        public readonly Lazy<Counter> NetworkP2PMessagesReceivedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_network_p2p_messages_received_total", "Total number of P2P messages received.", "type"); // Renamed from P2PMessagesReceived
        public readonly Lazy<Counter> NetworkP2PMessagesSentTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_network_p2p_messages_sent_total", "Total number of P2P messages sent.", "type"); // Renamed from P2PMessagesSent
        // Note: Other network metrics (traffic, latency, peer details) require deeper integration.

        // --- Transaction Pool (Mempool) Metrics ---
        public readonly Lazy<Gauge> MempoolSizeTransactions = NonCapturingLazyInitializer.CreateGauge(
             "neo_mempool_size_transactions", "Number of transactions currently in the mempool."); // Renamed from MempoolTransactions
        public readonly Lazy<Gauge> MempoolSizeBytes = NonCapturingLazyInitializer.CreateGauge(
            "neo_mempool_size_bytes", "Total size of transactions currently in the mempool."); // Kept name
        public readonly Lazy<Counter> MempoolTransactionsAddedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_mempool_transactions_added_total", "Total number of transactions successfully added to the mempool."); // Renamed
        public readonly Lazy<Counter> MempoolTransactionsRejectedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_mempool_transactions_rejected_total", "Total number of transactions rejected from the mempool.", "reason"); // Renamed
        // Note: Other mempool metrics (tx age, fee rate, propagation) require deeper integration.

        // --- Consensus Metrics (dBFT Specific) ---
        public readonly Lazy<Gauge> ConsensusCurrentHeight = NonCapturingLazyInitializer.CreateGauge(
             "neo_consensus_current_height", "Current block height the consensus service is working on."); // Renamed from ConsensusHeight
        public readonly Lazy<Gauge> ConsensusCurrentView = NonCapturingLazyInitializer.CreateGauge(
            "neo_consensus_current_view", "Current view number in the consensus service."); // Renamed from ConsensusView
        public readonly Lazy<Counter> ConsensusP2PMessagesReceivedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_consensus_p2p_messages_received_total", "Total number of consensus messages received.", "type"); // Renamed from ConsensusMessagesReceived
        public readonly Lazy<Histogram> ConsensusBlockGenerationDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
             "neo_consensus_block_generation_duration_seconds", "Histogram of time taken to generate a block during consensus.",
             new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.1, 2, 10) }); // Kept name
        public readonly Lazy<Counter> ConsensusNewBlockPersistedTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_consensus_new_block_persisted_total", "Total number of new blocks persisted via consensus."); // Kept name
        // Note: Other consensus metrics (validator status, participation, rewards) require deeper integration.

        // --- Validator Metrics (dBFT Specific) ---
        public readonly Lazy<Gauge> ValidatorActive = NonCapturingLazyInitializer.CreateGauge(
            "neo_validator_active", "Indicates if the node is currently an active consensus validator (1 if active, 0 otherwise).");
        public readonly Lazy<Counter> ValidatorMissedBlocksTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_validator_missed_blocks_total", "Total number of block proposals missed by this node when it was the primary validator.");

        // --- Execution & Block Processing Metrics ---
        public readonly Lazy<Histogram> TransactionExecutionDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
             "neo_transaction_execution_duration_seconds", "Histogram of time taken to execute a transaction in the VM.",
             new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) }); // Kept name
        public readonly Lazy<Histogram> BlockProcessingDurationSeconds = NonCapturingLazyInitializer.CreateHistogram(
            "neo_block_processing_duration_seconds", "Histogram of time taken to process and persist a block (verification, commit, events).",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.01, 2, 15) }); // Kept name
        public readonly Lazy<Gauge> BlockProcessingTransactionsTotal = NonCapturingLazyInitializer.CreateGauge(
             "neo_block_processing_transactions_total", "Number of transactions in the last processed block."); // Kept name
        public readonly Lazy<Gauge> BlockProcessingSizeBytes = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_processing_size_bytes", "Size of the last processed block in bytes."); // Kept name

        // --- N3 Economics Metrics ---
        public readonly Lazy<Gauge> BlockGasGeneratedTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_gas_generated_total", "GAS generated in the last processed block (in 10^-8 units)."); // Kept name
        public readonly Lazy<Gauge> BlockSystemFeeTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_system_fee_total", "System fee collected in the last processed block (in 10^-8 units)."); // Kept name
        public readonly Lazy<Gauge> BlockNetworkFeeTotal = NonCapturingLazyInitializer.CreateGauge(
            "neo_block_network_fee_total", "Network fee collected in the last processed block (in 10^-8 units)."); // Kept name

        // --- Security Metrics ---
        public readonly Lazy<Counter> FailedAuthenticationAttemptsTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_failed_authentication_attempts_total", "Total number of failed authentication attempts.", "service");
        public readonly Lazy<Counter> InvalidP2PMessageCountTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_invalid_p2p_message_count_total", "Total number of invalid P2P messages received.", "reason");
        public readonly Lazy<Counter> UnexpectedShutdownsTotal = NonCapturingLazyInitializer.CreateCounter(
            "neo_unexpected_shutdowns_total", "Total number of unexpected node shutdowns detected (e.g., via recovery).", "reason");

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
        /// <param name="system">The NeoSystem instance.</param>
        public void Start(PrometheusSettings? settings, NeoSystem system)
        {
            // Use the Lazy<T> lock implicitly via Instance access, but add explicit lock for mutation safety
            lock (LazyInstance) // Lock on the lazy object itself for mutation
            {
                if (_enabled) { Log(nameof(PrometheusService), LogLevel.Warning, "Prometheus service already started."); return; }
                if (settings == null || !settings.Enabled) { _enabled = false; Log(nameof(PrometheusService), LogLevel.Info, "Prometheus monitoring is disabled."); return; }

                _neoSystem = system; // Store the NeoSystem instance

                try
                {
                    Log(nameof(PrometheusService), LogLevel.Info, $"Attempting to start Prometheus HTTP server on {settings.Host}:{settings.Port}");
                    // Verify the host and port
                    if (settings.Port <= 0 || settings.Port > 65535)
                    {
                        throw new ArgumentException($"Invalid port number: {settings.Port}. Must be between 1 and 65535.");
                    }
                    // _systemMetricsCollector = SystemMetrics.StartCollecting(); // Commented out due to compatibility issues
                    // Log(nameof(PrometheusService), LogLevel.Info, "Started collecting system metrics for Prometheus."); // Commented out
                    // Initialize MetricServer with proper settings
                    // For better network accessibility, we explicitly configure everything
                    Log(nameof(PrometheusService), LogLevel.Info, $"Creating Prometheus server with port: {settings.Port}");

                    // Use proper host binding based on settings
                    string host = settings.Host;

                    // Only use wildcard binding if specifically configured for all interfaces
                    bool useWildcardBinding = (host == "0.0.0.0" || host == "+");
                    var prefix = useWildcardBinding
                        ? $"http://+:{settings.Port}/"
                        : $"http://{host}:{settings.Port}/";

                    Log(nameof(PrometheusService), LogLevel.Info, $"Using HTTP prefix: {prefix}");
                    try
                    {
                        // Create MetricServer with explicit host and URL
                        if (useWildcardBinding)
                        {
                            // Use port-only constructor for wildcard binding
                            _metricServer = new MetricServer(port: settings.Port, url: "metrics/");
                        }
                        else
                        {
                            // Use specific hostname
                            _metricServer = new MetricServer(host, settings.Port, "metrics/");
                        }

                        Log(nameof(PrometheusService), LogLevel.Info, "Created MetricServer instance");
                        _metricServer.Start();
                        Log(nameof(PrometheusService), LogLevel.Info, "Started MetricServer instance");
                    }
                    catch (HttpListenerException hlex)
                    {
                        Log(nameof(PrometheusService), LogLevel.Error, $"HTTP Listener error: {hlex.ErrorCode} - {hlex.Message}");
                        if (hlex.ErrorCode == 5)
                        {
                            Log(nameof(PrometheusService), LogLevel.Error, "Access denied. You may need to run as administrator or use 'netsh http add urlacl'");
                            Log(nameof(PrometheusService), LogLevel.Error, $"Try running: netsh http add urlacl url={prefix} user=Everyone");
                        }
                        throw;
                    }

                    // Test if we can access the metrics endpoint ourselves as a validation step
                    try
                    {
                        Log(nameof(PrometheusService), LogLevel.Info, "Testing metrics endpoint connection...");
                        // Use HttpClient instead of WebClient (which is obsolete)
                        using var client = new System.Net.Http.HttpClient();
                        // Try connecting via localhost regardless of binding for the test
                        string testUrl = $"http://localhost:{settings.Port}/metrics";
                        // Use a short timeout to avoid hanging if there's an issue
                        client.Timeout = TimeSpan.FromSeconds(3);
                        var response = client.GetAsync(testUrl).GetAwaiter().GetResult();
                        if (response.IsSuccessStatusCode)
                        {
                            Log(nameof(PrometheusService), LogLevel.Info, $"Successfully verified metrics endpoint at {testUrl}");
                        }
                        else
                        {
                            Log(nameof(PrometheusService), LogLevel.Warning, $"Metrics endpoint returned status code {response.StatusCode}");
                        }
                    }
                    catch (Exception testEx)
                    {
                        // Log but don't fail - this is just a test
                        Log(nameof(PrometheusService), LogLevel.Warning, $"Unable to verify metrics endpoint: {testEx.Message}");
                        Log(nameof(PrometheusService), LogLevel.Warning, "The server may still be working but you might need to check firewall settings or permissions");
                    }
                    // Start manual process metrics collection
                    _processMetricsTimer = new Timer(UpdateProcessMetrics, null, _processMetricsInterval, _processMetricsInterval);
                    Log(nameof(PrometheusService), LogLevel.Info, $"Started manual process metrics collection (Update interval: {_processMetricsInterval.TotalSeconds}s).");

                    // Start node state metrics collection
                    _nodeStateMetricsTimer = new Timer(UpdateNodeStateMetrics, null, _nodeStateMetricsInterval, _nodeStateMetricsInterval);
                    Log(nameof(PrometheusService), LogLevel.Info, $"Started node state metrics collection (Update interval: {_nodeStateMetricsInterval.TotalSeconds}s).");

                    _enabled = true;
                    Log(nameof(PrometheusService), LogLevel.Info, $"Prometheus metrics server started at http://{settings.Host}:{settings.Port}/metrics");
                    // Add access note for 0.0.0.0 binding
                    if (settings.Host == "0.0.0.0")
                    {
                        Log(nameof(PrometheusService), LogLevel.Info, $"Note: When binding to 0.0.0.0, access metrics from a browser using http://127.0.0.1:{settings.Port}/metrics or http://localhost:{settings.Port}/metrics");
                        Log(nameof(PrometheusService), LogLevel.Info, $"To access metrics via command line, use: curl http://127.0.0.1:{settings.Port}/metrics");
                    }
                }
                catch (Exception ex)
                {
                    _enabled = false;
                    Log(nameof(PrometheusService), LogLevel.Error, $"Failed to start Prometheus service: {ex.GetBaseException().Message}");
                    Log(nameof(PrometheusService), LogLevel.Error, $"Exception details: {ex}");
                    if (ex is HttpListenerException httpEx)
                    {
                        Log(nameof(PrometheusService), LogLevel.Error, $"HTTP Listener error code: {httpEx.ErrorCode}");
                        if (httpEx.ErrorCode == 5)
                        {
                            Log(nameof(PrometheusService), LogLevel.Error, "Access denied. Check if you have permission to bind to this port or if another application is using it.");
                        }
                    }
                    _metricServer?.Stop();
                    _processMetricsTimer?.Dispose(); // Dispose timer on startup failure
                    _nodeStateMetricsTimer?.Dispose(); // Dispose node state timer on startup failure
                    // _metricServer?.Dispose(); // Dispose not available in prometheus-net v6.0.0
                    // _systemMetricsCollector?.Dispose(); // Commented out due to compatibility issues
                    _metricServer = null; // Clear refs on failure
                    _processMetricsTimer = null;
                    _nodeStateMetricsTimer = null;
                    // _systemMetricsCollector = null; // Commented out
                }
            }
        }

        #region Metric Recording Methods (Placeholder signatures might need core logic implementation)

        // These methods provide a safe way to interact with metrics,
        // checking if the service is enabled before accessing the Lazy<T>.Value.
        // TODO: Implement the actual logic within Neo core to call these methods.

        // --- Core Blockchain ---
        public void SetBlockchainBlockHeight(uint height)
        {
            if (!_enabled) return;
            BlockchainBlockHeight.Value.Set(height);
        }

        public void SetBlockchainSyncStatus(double status) // 0.0 to 1.0
        {
            if (!_enabled) return;
            BlockchainSyncStatus.Value.Set(status);
        }

        public void SetBlockchainChainTipLag(long lag)
        {
            if (!_enabled) return;
            BlockchainChainTipLag.Value.Set(lag);
        }

        // --- Node Performance ---
        // Note: ProcessWorkingSet and ProcessCpuTotal are updated by UpdateProcessMetrics timer below

        public void IncNodeApiRequests(string method, bool success)
        {
            if (!_enabled) return;
            NodeApiRequestsTotal.Value.WithLabels(method ?? "unknown", success ? "success" : "error").Inc();
        }

        public IDisposable MeasureNodeApiRequestDuration(string method)
        {
            return _enabled ? NodeApiRequestDurationSeconds.Value.WithLabels(method ?? "unknown").NewTimer() : NullDisposable.Instance;
        }

        // --- Network ---
        public void SetNetworkPeers(long count)
        {
            if (!_enabled) return;
            NetworkPeersCount.Value.Set(count);
        }

        public void IncNetworkP2PMessagesReceived(string type)
        {
            if (!_enabled) return;
            NetworkP2PMessagesReceivedTotal.Value.WithLabels(type ?? "unknown").Inc();
        }

        public void IncNetworkP2PMessagesSent(string type)
        {
            if (!_enabled) return;
            NetworkP2PMessagesSentTotal.Value.WithLabels(type ?? "unknown").Inc();
        }

        // --- Mempool ---
        public void SetMempoolSizeTransactions(long count)
        {
            if (!_enabled) return;
            MempoolSizeTransactions.Value.Set(count);
        }

        public void SetMempoolSizeBytes(long bytes)
        {
            if (!_enabled) return;
            MempoolSizeBytes.Value.Set(bytes);
        }

        public void IncMempoolTransactionsAdded()
        {
            if (!_enabled) return;
            MempoolTransactionsAddedTotal.Value.Inc();
        }

        public void IncMempoolTransactionsRejected(string reason)
        {
            if (!_enabled) return;
            MempoolTransactionsRejectedTotal.Value.WithLabels(reason ?? "unknown").Inc();
        }

        // --- Consensus ---
        public void SetConsensusHeight(uint height) // TODO: Check if this is still needed if BlockchainBlockHeight exists
        {
            if (!_enabled) return;
            ConsensusCurrentHeight.Value.Set(height);
        }

        public void SetConsensusView(byte view)
        {
            if (!_enabled) return;
            ConsensusCurrentView.Value.Set(view);
        }

        public void IncConsensusMessagesReceived(string type)
        {
            if (!_enabled) return;
            ConsensusP2PMessagesReceivedTotal.Value.WithLabels(type ?? "unknown").Inc();
        }

        public IDisposable MeasureConsensusBlockGeneration()
        {
            return _enabled ? ConsensusBlockGenerationDurationSeconds.Value.NewTimer() : NullDisposable.Instance;
        }

        public void IncConsensusNewBlockPersisted()
        {
            if (!_enabled) return;
            ConsensusNewBlockPersistedTotal.Value.Inc();
        }

        // --- Validator ---
        public void SetValidatorActive(bool isActive)
        {
            if (!_enabled) return;
            ValidatorActive.Value.Set(isActive ? 1 : 0);
        }

        public void IncValidatorMissedBlocks()
        {
            if (!_enabled) return;
            ValidatorMissedBlocksTotal.Value.Inc();
        }

        // --- Execution & Block Processing ---
        public void RecordTransactionExecutionTime(double seconds)
        {
            if (!_enabled) return;
            TransactionExecutionDurationSeconds.Value.Observe(seconds);
        }

        // Use: using (var timer = PrometheusService.Instance.MeasureBlockProcessing()) { ... timer.SetBlockDetails(...); ... return result; }
        public IBlockProcessingTimer MeasureBlockProcessing()
        {
            // Returns a timer that also allows setting block-specific gauges upon disposal
            return _enabled ? new BlockProcessingTimerImpl(
                                BlockProcessingDurationSeconds.Value, // Use renamed metric
                                BlockProcessingTransactionsTotal.Value, // Use renamed metric
                                BlockProcessingSizeBytes.Value, // Use renamed metric
                                BlockGasGeneratedTotal.Value, // Use renamed metric
                                BlockSystemFeeTotal.Value, // Use renamed metric
                                BlockNetworkFeeTotal.Value) // Use renamed metric
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
            private readonly IDisposable _histogramTimer;
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
                _histogramTimer.Dispose();
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

        // --- Security ---
        public void IncFailedAuthentication(string service)
        {
            if (!_enabled) return;
            FailedAuthenticationAttemptsTotal.Value.WithLabels(service ?? "unknown").Inc();
        }

        public void IncInvalidP2PMessage(string reason)
        {
            if (!_enabled) return;
            InvalidP2PMessageCountTotal.Value.WithLabels(reason ?? "unknown").Inc();
        }

        public void IncUnexpectedShutdown(string reason)
        {
            if (!_enabled) return;
            UnexpectedShutdownsTotal.Value.WithLabels(reason ?? "unknown").Inc();
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
                NodeMemoryWorkingSetBytes.Value.Set(currentProcess.WorkingSet64); // Use renamed metric
                NodeCpuSecondsTotal.Value.Set(currentProcess.TotalProcessorTime.TotalSeconds); // Corrected back to use Set for Gauge
            }
            catch (Exception ex)
            {
                // Log error but don't crash the timer thread
                Log(nameof(PrometheusService), LogLevel.Error, $"Failed to update process metrics: {ex.Message}");
            }
        }

        #endregion // Process Metrics Update

        #region Node State Metrics Update

        /// <summary>
        /// Callback method for the node state metrics timer. Updates various gauges like blockchain height, peers, etc.
        /// </summary>
        private void UpdateNodeStateMetrics(object? state)
        {
            if (!_enabled || _neoSystem == null) return; // Don't update if service is stopping/stopped or NeoSystem not available

            try
            {
                // Access data via the stored NeoSystem instance
                var snapshot = _neoSystem.StoreView; // Get a snapshot for consistent reads
                var headerCache = _neoSystem.HeaderCache;

                if (snapshot == null || headerCache == null)
                {
                    Log(nameof(PrometheusService), LogLevel.Debug, "Snapshot or HeaderCache not available yet for metrics update.");
                    return;
                }

                // --- Update Blockchain Metrics ---
                // Get current persisted block height using LedgerContract
                uint currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);

                // Get the highest known header height from the header cache
                // Use currentHeight as fallback if cache is empty or hasn't advanced yet
                uint headerHeight = headerCache.Last?.Index ?? currentHeight;

                // Calculate Sync Status (0.0 to 1.0)
                double syncStatus = 0.0;
                if (headerHeight == 0 || currentHeight >= headerHeight)
                {
                    // If no headers known or we are at/ahead of the header height, consider synced
                    syncStatus = 1.0;
                }
                else
                {
                    // Ensure headerHeight is not zero before dividing
                    syncStatus = (double)currentHeight / headerHeight;
                }

                // Calculate Chain Tip Lag (blocks behind)
                long chainTipLag = 0;
                if (headerHeight > currentHeight)
                {
                    chainTipLag = headerHeight - currentHeight;
                }

                // Update blockchain metrics using the existing public methods
                SetBlockchainSyncStatus(syncStatus);
                SetBlockchainChainTipLag(chainTipLag);
                SetBlockchainBlockHeight(currentHeight); // Update height here as well

                // --- Update Network Peer Count ---
                try
                {
                    // Ask the LocalNode actor for its peer count
                    // Using Ask().Result for simplicity, consider async if needed
                    var peerCountTask = _neoSystem.LocalNode.Ask<int>(new LocalNode.GetPeerCount(), TimeSpan.FromSeconds(1)); // Added timeout
                    int peerCount = peerCountTask.Result; // Blocking call
                    SetNetworkPeers(peerCount);
                }
                catch (Exception askEx)
                {
                    Log(nameof(PrometheusService), LogLevel.Warning, $"Failed to get peer count from LocalNode: {askEx.Message}");
                    // Optionally set peer count to a specific value like -1 on error, or leave it unchanged
                    // SetNetworkPeers(-1);
                }

                // --- Update Mempool Gauges ---
                try
                {
                    var memPool = _neoSystem.MemPool;
                    if (memPool != null)
                    {
                        SetMempoolSizeTransactions(memPool.VerifiedCount); // Use VerifiedCount for current tx in pool
                        SetMempoolSizeBytes(memPool.SizeBytes);
                    }
                    else
                    {
                        Log(nameof(PrometheusService), LogLevel.Debug, "Mempool not available yet for metrics update.");
                    }
                }
                catch (Exception mempoolEx)
                {
                    Log(nameof(PrometheusService), LogLevel.Warning, $"Failed to get mempool metrics: {mempoolEx.Message}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the timer thread
                Log(nameof(PrometheusService), LogLevel.Error, $"Failed to update node state metrics: {ex.Message}");
            }
        }

        #endregion // Node State Metrics Update

        /// <summary>
        /// Stops the metrics server and disposes resources.
        /// </summary>
        public void Dispose()
        {
            lock (LazyInstance) // Lock for mutation safety
            {
                if (!_enabled && _metricServer == null && _processMetricsTimer == null && _nodeStateMetricsTimer == null /* && _systemMetricsCollector == null */) return; // SystemMetrics part commented out

                Log(nameof(PrometheusService), LogLevel.Info, "Stopping Prometheus service...");

                // Stop and dispose the process metrics timer first
                _processMetricsTimer?.Dispose();
                _processMetricsTimer = null;

                // Stop and dispose the node state metrics timer
                _nodeStateMetricsTimer?.Dispose();
                _nodeStateMetricsTimer = null;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="PrometheusSettings"/> class.
        /// Default values: Enabled = false, Host = 127.0.0.1, Port = 9100
        /// </summary>
        public PrometheusSettings()
        {
            // Default configuration values are set by property initializers
        }
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
