// Copyright (C) 2015-2025 The Neo Project.
//
// OpenTelemetryPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo;
using Neo.ConsoleService;
using Neo.IEventHandlers;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract.Native;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins.OpenTelemetry
{
    public class OpenTelemetryPlugin : Plugin, ICommittingHandler, ICommittedHandler
    {
        private MeterProvider? _meterProvider;
        private Meter? _meter;
        private OTelSettings _settings = OTelSettings.Default;

        // Metrics
        private Counter<long>? _blocksProcessedCounter;
        private Counter<long>? _transactionsProcessedCounter;
        private Counter<long>? _contractInvocationsCounter;
        private Histogram<double>? _blockProcessingTimeHistogram;
        private ObservableGauge<long>? _blockHeightGauge;
        private ObservableGauge<int>? _mempoolSizeGauge;
        private ObservableGauge<int>? _connectedPeersGauge;

        // Network metrics
        private Counter<long>? _peerConnectedCounter;
        private Counter<long>? _peerDisconnectedCounter;
        private Counter<long>? _bytesSentCounter;
        private Counter<long>? _bytesReceivedCounter;
        private ObservableGauge<int>? _unconnectedPeersGauge;
        private Counter<long>? _messagesReceivedCounter;
        private Counter<long>? _messagesSentCounter;

        // MemPool metrics  
        private ObservableGauge<int>? _mempoolVerifiedGauge;
        private ObservableGauge<int>? _mempoolUnverifiedGauge;
        private ObservableGauge<long>? _mempoolMemoryBytesGauge;
        private Counter<long>? _mempoolConflictsCounter;
        private Histogram<int>? _mempoolBatchRemovedHistogram;
        private ObservableGauge<double>? _mempoolCapacityRatioGauge;

        // Performance metrics
        private Histogram<double>? _transactionVerificationTimeHistogram;
        private Counter<long>? _transactionVerificationFailuresCounter;
        private ObservableGauge<double>? _blockProcessingRateGauge;

        // Error tracking metrics
        private Counter<long>? _protocolErrorsCounter;
        private Counter<long>? _networkErrorsCounter;
        private Counter<long>? _storageErrorsCounter;

        // System metrics
        private ObservableGauge<double>? _cpuUsageGauge;
        private ObservableGauge<double>? _systemCpuUsageGauge;
        private ObservableGauge<long>? _memoryWorkingSetGauge;
        private ObservableGauge<long>? _memoryVirtualGauge;
        private ObservableGauge<long>? _gcHeapSizeGauge;
        private ObservableGauge<long>? _threadCountGauge;
        private ObservableGauge<long>? _nodeStartTimeGauge;
        private ObservableGauge<int>? _networkIdGauge;
        private ObservableGauge<int>? _isSyncingGauge;

        // State tracking
        private readonly object _metricsLock = new object();
        private Stopwatch? _blockProcessingStopwatch;
        private NeoSystem? _neoSystem;
        private uint _currentBlockHeight = 0;
        private readonly DateTime _lastBlockTime = DateTime.UtcNow;
        private readonly Queue<(DateTime time, uint height)> _blockHistory = new Queue<(DateTime, uint)>();
        private long _lastMemPoolMemoryBytes = 0;
        private readonly DateTime _nodeStartTime = DateTime.UtcNow;
        private Process? _currentProcess;
        private DateTime _lastCpuCheck = DateTime.UtcNow;
        private TimeSpan _lastProcessorTime = TimeSpan.Zero;
        private MetricsCollector? _metricsCollector;

        public override string Name => "OpenTelemetry";
        public override string Description => "Provides observability for Neo blockchain node using OpenTelemetry";

        protected override UnhandledExceptionPolicy ExceptionPolicy => UnhandledExceptionPolicy.StopPlugin;

        protected override void Configure()
        {
            try
            {
                var config = GetConfiguration();
                _settings = new OTelSettings(config);

                if (!_settings.Enabled)
                {
                    ConsoleHelper.Warning("OpenTelemetry plugin is disabled in configuration");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error($"Failed to load OpenTelemetry configuration: {ex.Message}");
                _settings = OTelSettings.Default;
            }
        }

        protected override void OnSystemLoaded(NeoSystem system)
        {
            if (!_settings.Enabled) return;

            try
            {
                _neoSystem = system;
                InitializeMetrics();

                // Subscribe to blockchain events
                Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
                Blockchain.Committed += ((ICommittedHandler)this).Blockchain_Committed_Handler;

                // Initialize metrics collector
                _metricsCollector = new MetricsCollector(system, TimeSpan.FromSeconds(5));
                
                // Subscribe to metrics updates
                _metricsCollector.NetworkMetricsUpdated += OnNetworkMetricsUpdated;
                _metricsCollector.MemPoolMetricsUpdated += OnMemPoolMetricsUpdated;
                _metricsCollector.BlockchainMetricsUpdated += OnBlockchainMetricsUpdated;

                ConsoleHelper.Info("OpenTelemetry plugin initialized successfully");
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error($"Failed to initialize OpenTelemetry: {ex.Message}");
                _settings = OTelSettings.Default;

                if (ExceptionPolicy == UnhandledExceptionPolicy.StopNode)
                    throw;
            }
        }

        private void InitializeMetrics()
        {
            // Create meter
            _meter = new Meter("Neo.Blockchain", GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0");

            // Create counters
            _blocksProcessedCounter = _meter.CreateCounter<long>(
                "neo.blocks.processed_total",
                "blocks",
                "Total number of blocks processed");

            _transactionsProcessedCounter = _meter.CreateCounter<long>(
                "neo.transactions.processed_total",
                "transactions",
                "Total number of transactions processed");

            _contractInvocationsCounter = _meter.CreateCounter<long>(
                "neo.contracts.invocations_total",
                "invocations",
                "Total number of contract invocations");

            // Create histogram
            _blockProcessingTimeHistogram = _meter.CreateHistogram<double>(
                "neo.block.processing_time",
                "milliseconds",
                "Time taken to process a block");

            // Create observable gauges
            _blockHeightGauge = _meter.CreateObservableGauge<long>(
                "neo.blockchain.height",
                () => _currentBlockHeight,
                "blocks",
                "Current blockchain height");

            _mempoolSizeGauge = _meter.CreateObservableGauge<int>(
                "neo.mempool.size",
                () => _neoSystem?.MemPool?.Count ?? 0,
                "transactions",
                "Current number of transactions in mempool");

            _connectedPeersGauge = _meter.CreateObservableGauge<int>(
                "neo.p2p.connected_peers",
                () =>
                {
                    if (_neoSystem?.LocalNode is LocalNode localNode)
                        return localNode.ConnectedCount;
                    return 0;
                },
                "peers",
                "Number of connected P2P peers");

            // Create network metrics
            _peerConnectedCounter = _meter.CreateCounter<long>(
                "neo.p2p.peer_connected_total",
                "peers",
                "Total number of peer connections");

            _peerDisconnectedCounter = _meter.CreateCounter<long>(
                "neo.p2p.peer_disconnected_total",
                "peers",
                "Total number of peer disconnections");

            _bytesSentCounter = _meter.CreateCounter<long>(
                "neo.p2p.bytes_sent_total",
                "bytes",
                "Total number of bytes sent");

            _bytesReceivedCounter = _meter.CreateCounter<long>(
                "neo.p2p.bytes_received_total",
                "bytes",
                "Total number of bytes received");

            // Create mempool metrics
            _mempoolVerifiedGauge = _meter.CreateObservableGauge<int>(
                "neo.mempool.verified_count",
                () => _neoSystem?.MemPool?.VerifiedCount ?? 0,
                "transactions",
                "Number of verified transactions in mempool");

            _mempoolUnverifiedGauge = _meter.CreateObservableGauge<int>(
                "neo.mempool.unverified_count",
                () => _neoSystem?.MemPool?.UnVerifiedCount ?? 0,
                "transactions",
                "Number of unverified transactions in mempool");

            _mempoolMemoryBytesGauge = _meter.CreateObservableGauge<long>(
                "neo.mempool.memory_bytes",
                () => _lastMemPoolMemoryBytes,
                "bytes",
                "Total memory used by transactions in mempool");

            _mempoolConflictsCounter = _meter.CreateCounter<long>(
                "neo.mempool.conflicts_total",
                "conflicts",
                "Total number of transaction conflicts detected");

            _mempoolBatchRemovedHistogram = _meter.CreateHistogram<int>(
                "neo.mempool.batch_removed_size",
                "transactions",
                "Number of transactions removed in batch operations");

            _mempoolCapacityRatioGauge = _meter.CreateObservableGauge<double>(
                "neo.mempool.capacity_ratio",
                () => _neoSystem?.MemPool != null ? (double)_neoSystem.MemPool.Count / _neoSystem.MemPool.Capacity : 0,
                "ratio",
                "Ratio of mempool usage to capacity (0-1)");

            // Additional network metrics
            _unconnectedPeersGauge = _meter.CreateObservableGauge<int>(
                "neo.p2p.unconnected_peers",
                () =>
                {
                    if (_neoSystem?.LocalNode is LocalNode localNode)
                        return localNode.UnconnectedCount;
                    return 0;
                },
                "peers",
                "Number of known but unconnected peers");

            _messagesReceivedCounter = _meter.CreateCounter<long>(
                "neo.p2p.messages_received_total",
                "messages",
                "Total number of P2P messages received");

            _messagesSentCounter = _meter.CreateCounter<long>(
                "neo.p2p.messages_sent_total",
                "messages",
                "Total number of P2P messages sent");

            // Performance metrics
            _transactionVerificationTimeHistogram = _meter.CreateHistogram<double>(
                "neo.transaction.verification_time",
                "milliseconds",
                "Time taken to verify transactions");

            _transactionVerificationFailuresCounter = _meter.CreateCounter<long>(
                "neo.transaction.verification_failures_total",
                "failures",
                "Total number of transaction verification failures");

            _blockProcessingRateGauge = _meter.CreateObservableGauge<double>(
                "neo.block.processing_rate",
                () => CalculateBlockProcessingRate(),
                "blocks/second",
                "Current block processing rate");

            // Error tracking metrics
            _protocolErrorsCounter = _meter.CreateCounter<long>(
                "neo.errors.protocol_total",
                "errors",
                "Total number of protocol errors");

            _networkErrorsCounter = _meter.CreateCounter<long>(
                "neo.errors.network_total",
                "errors",
                "Total number of network errors");

            _storageErrorsCounter = _meter.CreateCounter<long>(
                "neo.errors.storage_total",
                "errors",
                "Total number of storage errors");

            // System metrics
            _currentProcess = Process.GetCurrentProcess();

            _cpuUsageGauge = _meter.CreateObservableGauge<double>(
                "process_cpu_usage",
                () => GetProcessCpuUsage(),
                "percent",
                "Process CPU usage percentage");

            _systemCpuUsageGauge = _meter.CreateObservableGauge<double>(
                "system_cpu_usage",
                () => GetSystemCpuUsage(),
                "percent",
                "System CPU usage percentage");

            _memoryWorkingSetGauge = _meter.CreateObservableGauge<long>(
                "process_memory_working_set",
                () => _currentProcess?.WorkingSet64 ?? 0,
                "bytes",
                "Process working set memory");

            _memoryVirtualGauge = _meter.CreateObservableGauge<long>(
                "process_memory_virtual",
                () => _currentProcess?.VirtualMemorySize64 ?? 0,
                "bytes",
                "Process virtual memory");

            _gcHeapSizeGauge = _meter.CreateObservableGauge<long>(
                "dotnet_gc_heap_size",
                () => GC.GetTotalMemory(false),
                "bytes",
                "GC heap size");

            _threadCountGauge = _meter.CreateObservableGauge<long>(
                "process_thread_count",
                () => _currentProcess?.Threads.Count ?? 0,
                "threads",
                "Process thread count");

            _nodeStartTimeGauge = _meter.CreateObservableGauge<long>(
                "neo_node_start_time",
                () => new DateTimeOffset(_nodeStartTime).ToUnixTimeSeconds(),
                "unixtime",
                "Node start time in Unix timestamp");

            _networkIdGauge = _meter.CreateObservableGauge<int>(
                "neo_network_id",
                () => (int)(_neoSystem?.Settings.Network ?? 0),
                "id",
                "Network ID (0=TestNet, 1=MainNet)");

            _isSyncingGauge = _meter.CreateObservableGauge<int>(
                "neo_blockchain_is_syncing",
                () => _neoSystem != null && IsNodeSyncing() ? 1 : 0,
                "bool",
                "Whether the node is currently syncing (1=syncing, 0=synced)");

            // Initialize OpenTelemetry
            var config = GetConfiguration();
            _meterProvider = BuildMeterProvider(config);
        }

        private MeterProvider BuildMeterProvider(IConfigurationSection config)
        {
            var builder = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(BuildResource(config))
                .AddMeter("Neo.Blockchain");

            // Add configured exporters
            if (_settings.Metrics.ConsoleExporter.Enabled)
            {
                builder.AddConsoleExporter();
                ConsoleHelper.Info("OpenTelemetry: Console exporter enabled");
            }

            if (_settings.Metrics.PrometheusExporter.Enabled)
            {
                // Note: Prometheus HTTP listener is started automatically by the exporter
                // The AspNetCore version requires hosting in ASP.NET Core app
                // For standalone console app, we use the HttpListener version
                builder.AddPrometheusHttpListener(options =>
                {
                    options.UriPrefixes = new[] { $"http://+:{_settings.Metrics.PrometheusExporter.Port}/" };
                    options.ScrapeEndpointPath = _settings.Metrics.PrometheusExporter.Path;
                });
                ConsoleHelper.Info($"OpenTelemetry: Prometheus exporter enabled on port {_settings.Metrics.PrometheusExporter.Port}{_settings.Metrics.PrometheusExporter.Path}");
            }

            if (_settings.OtlpExporter.Enabled && _settings.OtlpExporter.ExportMetrics)
            {
                builder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(_settings.OtlpExporter.Endpoint);
                    options.Protocol = _settings.OtlpExporter.Protocol == OTelConstants.ProtocolGrpc
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;
                    options.TimeoutMilliseconds = _settings.OtlpExporter.Timeout;

                    if (!string.IsNullOrWhiteSpace(_settings.OtlpExporter.Headers))
                        options.Headers = _settings.OtlpExporter.Headers;
                });
                ConsoleHelper.Info($"OpenTelemetry: OTLP exporter enabled for metrics to {_settings.OtlpExporter.Endpoint}");
            }

            return builder.Build();
        }

        private ResourceBuilder BuildResource(IConfigurationSection config)
        {
            var instanceId = string.IsNullOrWhiteSpace(_settings.InstanceId)
                ? Environment.MachineName
                : _settings.InstanceId;

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(_settings.ServiceName,
                    serviceVersion: _settings.ServiceVersion,
                    serviceInstanceId: instanceId);

            // Add custom resource attributes
            var resourceAttributes = config.GetSection("ResourceAttributes");
            if (resourceAttributes.Exists())
            {
                var attributes = resourceAttributes.GetChildren()
                    .Select(x => new KeyValuePair<string, object>(x.Key, x.Value ?? string.Empty))
                    .ToList();

                if (attributes.Count > 0)
                    resourceBuilder.AddAttributes(attributes);
            }

            return resourceBuilder;
        }

        void ICommittingHandler.Blockchain_Committing_Handler(NeoSystem system, Block block,
            DataCache snapshot, IReadOnlyList<ApplicationExecuted> applicationExecutedList)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;

            lock (_metricsLock)
            {
                // Start timing block processing
                _blockProcessingStopwatch = Stopwatch.StartNew();

                // Count transactions by type
                if (_transactionsProcessedCounter != null)
                {
                    var transactionsByType = block.Transactions
                        .GroupBy(tx => tx.GetType().Name)
                        .Select(g => new { Type = g.Key, Count = g.Count() });

                    foreach (var group in transactionsByType)
                    {
                        _transactionsProcessedCounter.Add(group.Count,
                            new KeyValuePair<string, object?>("type", group.Type));
                    }
                }

                // Count contract invocations and track failures
                if (_contractInvocationsCounter != null || _transactionVerificationFailuresCounter != null)
                {
                    var invocations = applicationExecutedList
                        .Where(x => x.Transaction != null)
                        .Count();

                    if (invocations > 0)
                        _contractInvocationsCounter?.Add(invocations);

                    // Track execution failures
                    var failures = applicationExecutedList
                        .Where(x => x.VMState != VM.VMState.HALT)
                        .Count();
                    if (failures > 0)
                        _transactionVerificationFailuresCounter?.Add(failures);
                }
            }
        }

        void ICommittedHandler.Blockchain_Committed_Handler(NeoSystem system, Block block)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;

            lock (_metricsLock)
            {
                // Record block processing time
                if (_blockProcessingStopwatch != null)
                {
                    _blockProcessingTimeHistogram?.Record(_blockProcessingStopwatch.ElapsedMilliseconds);
                    _blockProcessingStopwatch = null;
                }

                // Update block height
                _currentBlockHeight = block.Index;

                // Track block processing history for rate calculation
                var now = DateTime.UtcNow;
                _blockHistory.Enqueue((now, block.Index));

                // Keep only last 60 seconds of history
                while (_blockHistory.Count > 0 && (now - _blockHistory.Peek().time).TotalSeconds > 60)
                {
                    _blockHistory.Dequeue();
                }

                // Increment blocks processed counter
                _blocksProcessedCounter?.Add(1);
            }
        }

        public override void Dispose()
        {
            // Unsubscribe from events
            Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed -= ((ICommittedHandler)this).Blockchain_Committed_Handler;

            // Dispose metrics collector
            _metricsCollector?.Dispose();
            
            _meterProvider?.Dispose();
            _meter?.Dispose();
            _blockProcessingStopwatch = null;
            base.Dispose();
        }

        [ConsoleCommand("telemetry status", Category = "OpenTelemetry", Description = "Show telemetry status")]
        private void ShowTelemetryStatus()
        {
            ConsoleHelper.Info($"OpenTelemetry Status:");
            ConsoleHelper.Info($"  Enabled: {_settings.Enabled}");
            ConsoleHelper.Info($"  Service: {_settings.ServiceName} v{_settings.ServiceVersion}");
            ConsoleHelper.Info($"  Current Block Height: {_currentBlockHeight}");
            ConsoleHelper.Info($"  MemPool Size: {_neoSystem?.MemPool?.Count ?? 0}");
            var connectedPeers = _neoSystem?.LocalNode is LocalNode localNode ? localNode.ConnectedCount : 0;
            ConsoleHelper.Info($"  Connected Peers: {connectedPeers}");

            if (_settings.Enabled && _meter != null)
            {
                ConsoleHelper.Info($"  Metrics:");
                ConsoleHelper.Info($"    - Blocks Processed Counter: Active");
                ConsoleHelper.Info($"    - Transactions Processed Counter: Active");
                ConsoleHelper.Info($"    - Contract Invocations Counter: Active");
                ConsoleHelper.Info($"    - Block Processing Time Histogram: Active");
                ConsoleHelper.Info($"    - Blockchain Height Gauge: Active");
                ConsoleHelper.Info($"    - MemPool Size Gauge: Active");
                ConsoleHelper.Info($"    - Connected Peers Gauge: Active");
                ConsoleHelper.Info($"  Exporters:");
                if (_settings.Metrics.ConsoleExporter.Enabled)
                    ConsoleHelper.Info($"    - Console Exporter: Active");
                if (_settings.Metrics.PrometheusExporter.Enabled)
                    ConsoleHelper.Info($"    - Prometheus Exporter: Active on port {_settings.Metrics.PrometheusExporter.Port}");
                if (_settings.OtlpExporter.Enabled && _settings.OtlpExporter.ExportMetrics)
                    ConsoleHelper.Info($"    - OTLP Exporter: Active to {_settings.OtlpExporter.Endpoint}");
            }
        }

        // Metrics update handlers
        private void OnNetworkMetricsUpdated(NetworkMetrics metrics)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;
            
            // Network metrics are now collected via polling
            // Connected/unconnected peer counts are updated through observable gauges
        }

        private void OnMemPoolMetricsUpdated(MemPoolMetrics metrics)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;

            lock (_metricsLock)
            {
                // Update memory bytes estimate
                _lastMemPoolMemoryBytes = metrics.EstimatedMemoryBytes;
            }
        }

        private void OnBlockchainMetricsUpdated(BlockchainMetrics metrics)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;
            
            lock (_metricsLock)
            {
                _currentBlockHeight = metrics.CurrentHeight;
            }
        }

        private double CalculateBlockProcessingRate()
        {
            lock (_metricsLock)
            {
                if (_blockHistory.Count < 2) return 0;

                var oldest = _blockHistory.First();
                var newest = _blockHistory.Last();
                var timeDiff = (newest.time - oldest.time).TotalSeconds;

                if (timeDiff <= 0) return 0;

                var blockDiff = newest.height - oldest.height;
                return blockDiff / timeDiff;
            }
        }

        private double GetProcessCpuUsage()
        {
            try
            {
                _currentProcess?.Refresh();
                if (_currentProcess != null)
                {
                    var currentTime = DateTime.UtcNow;
                    var currentProcessorTime = _currentProcess.TotalProcessorTime;

                    if (_lastProcessorTime != TimeSpan.Zero)
                    {
                        var timeDiff = (currentTime - _lastCpuCheck).TotalMilliseconds;
                        var cpuDiff = (currentProcessorTime - _lastProcessorTime).TotalMilliseconds;

                        if (timeDiff > 0)
                        {
                            var cpuUsage = (cpuDiff / timeDiff) * 100.0 / Environment.ProcessorCount;
                            _lastCpuCheck = currentTime;
                            _lastProcessorTime = currentProcessorTime;
                            return Math.Min(100.0, Math.Max(0.0, cpuUsage));
                        }
                    }
                    else
                    {
                        _lastCpuCheck = currentTime;
                        _lastProcessorTime = currentProcessorTime;
                    }
                }
            }
            catch
            {
                // Ignore errors in CPU calculation
            }
            return 0;
        }

        private double GetSystemCpuUsage()
        {
            try
            {
                // Use process idle time to calculate system CPU usage
                using (var process = Process.GetProcessesByName("Idle").FirstOrDefault())
                {
                    if (process != null)
                    {
                        var idleTime = process.TotalProcessorTime.TotalMilliseconds;
                        var totalTime = Environment.TickCount;
                        if (totalTime > 0)
                        {
                            var usage = 100.0 - (idleTime / totalTime * 100.0 / Environment.ProcessorCount);
                            return Math.Min(100.0, Math.Max(0.0, usage));
                        }
                    }
                }
            }
            catch
            {
                // Fallback to process CPU if system CPU cannot be determined
                return GetProcessCpuUsage();
            }
            return 0;
        }

        private bool IsNodeSyncing()
        {
            if (_neoSystem == null) return false;

            try
            {
                // Consider the node as syncing if it's more than 10 blocks behind
                var currentHeight = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView);
                var headerHeight = _neoSystem.HeaderCache.Count > 0 ? _neoSystem.HeaderCache.Last?.Index ?? currentHeight : currentHeight;

                return headerHeight - currentHeight > 10;
            }
            catch
            {
                return false;
            }
        }
    }
}
