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
    public sealed partial class PrometheusService : IDisposable
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

        /// <summary>
        /// Private constructor for singleton pattern.
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

                    // Internal endpoint test was removed during development.

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
                currentProcess.Refresh();

                // Calls are to metrics defined in PrometheusService.Metrics.cs
                NodeMemoryWorkingSetBytes.Value.Set(currentProcess.WorkingSet64);
                NodeCpuSecondsTotal.Value.Set(currentProcess.TotalProcessorTime.TotalSeconds);
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
                // Calls are to methods defined in PrometheusService.Recording.cs
                uint currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
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
}
