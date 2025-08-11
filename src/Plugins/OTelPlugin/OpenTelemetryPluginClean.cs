// Copyright (C) 2015-2025 The Neo Project.
//
// OpenTelemetryPluginClean.cs file belongs to the neo project and is free
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
using System.Reflection;
using System.Threading;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Production-ready OpenTelemetry plugin that collects metrics without modifying core classes
    /// </summary>
    public class OpenTelemetryPluginClean : Plugin, ICommittingHandler, ICommittedHandler
    {
        private MeterProvider? _meterProvider;
        private Meter? _meter;
        private OTelSettings _settings = OTelSettings.Default;
        private MetricsCollector? _metricsCollector;
        private NeoSystem? _neoSystem;

        // Block processing metrics
        private Counter<long>? _blocksProcessedCounter;
        private Counter<long>? _transactionsProcessedCounter;
        private Counter<long>? _contractInvocationsCounter;
        private Histogram<double>? _blockProcessingTimeHistogram;
        private ObservableGauge<long>? _blockHeightGauge;

        // MemPool metrics (collected via polling)
        private ObservableGauge<int>? _mempoolSizeGauge;
        private ObservableGauge<int>? _mempoolVerifiedGauge;
        private ObservableGauge<int>? _mempoolUnverifiedGauge;
        private ObservableGauge<double>? _mempoolCapacityRatioGauge;
        private ObservableGauge<long>? _mempoolEstimatedBytesGauge;

        // Network metrics (collected via polling)
        private ObservableGauge<int>? _connectedPeersGauge;
        private ObservableGauge<int>? _unconnectedPeersGauge;

        // System metrics
        private ObservableGauge<double>? _cpuUsageGauge;
        private ObservableGauge<long>? _memoryWorkingSetGauge;
        private ObservableGauge<long>? _gcHeapSizeGauge;
        private ObservableGauge<long>? _threadCountGauge;
        private ObservableGauge<long>? _nodeStartTimeGauge;
        private ObservableGauge<int>? _networkIdGauge;
        private ObservableGauge<int>? _isSyncingGauge;

        // Performance tracking
        private ObservableGauge<double>? _blockProcessingRateGauge;
        private Counter<long>? _transactionVerificationFailuresCounter;

        // State tracking
        private readonly object _metricsLock = new object();
        private Stopwatch? _blockProcessingStopwatch;
        private uint _currentBlockHeight = 0;
        private readonly Queue<(DateTime time, uint height)> _blockHistory = new Queue<(DateTime, uint)>();
        private readonly DateTime _nodeStartTime = DateTime.UtcNow;
        private Process? _currentProcess;
        private DateTime _lastCpuCheck = DateTime.UtcNow;
        private TimeSpan _lastProcessorTime = TimeSpan.Zero;
        private long _estimatedMemPoolBytes = 0;

        public override string Name => "OpenTelemetry";
        public override string Description => "Production-ready observability for Neo blockchain node using OpenTelemetry";

        protected override UnhandledExceptionPolicy ExceptionPolicy => _settings.UnhandledExceptionPolicy;

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

                // Subscribe to blockchain events (these are available without core modifications)
                Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
                Blockchain.Committed += ((ICommittedHandler)this).Blockchain_Committed_Handler;

                // Initialize metrics collector for polling-based metrics
                var collectionInterval = TimeSpan.FromMilliseconds(_settings.Metrics.Interval);
                _metricsCollector = new MetricsCollector(system, collectionInterval);

                // Subscribe to metrics updates
                _metricsCollector.MemPoolMetricsUpdated += OnMemPoolMetricsUpdated;
                _metricsCollector.BlockchainMetricsUpdated += OnBlockchainMetricsUpdated;

                ConsoleHelper.Info($"OpenTelemetry plugin initialized successfully (v{GetVersion()})");
                LogAvailableMetrics();
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
            var version = GetVersion();
            _meter = new Meter("Neo.Blockchain", version);

            // Initialize process for system metrics
            _currentProcess = Process.GetCurrentProcess();

            // Block processing metrics (available via events)
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

            _blockProcessingTimeHistogram = _meter.CreateHistogram<double>(
                "neo.block.processing_time",
                "milliseconds",
                "Time taken to process a block");

            _transactionVerificationFailuresCounter = _meter.CreateCounter<long>(
                "neo.transaction.verification_failures_total",
                "failures",
                "Total number of transaction verification failures");

            // Observable gauges for blockchain state
            _blockHeightGauge = _meter.CreateObservableGauge<long>(
                "neo.blockchain.height",
                () => _currentBlockHeight,
                "blocks",
                "Current blockchain height");

            _blockProcessingRateGauge = _meter.CreateObservableGauge<double>(
                "neo.block.processing_rate",
                () => CalculateBlockProcessingRate(),
                "blocks/second",
                "Current block processing rate");

            // MemPool metrics (via polling)
            _mempoolSizeGauge = _meter.CreateObservableGauge<int>(
                "neo.mempool.size",
                () => _neoSystem?.MemPool?.Count ?? 0,
                "transactions",
                "Current number of transactions in mempool");

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

            _mempoolCapacityRatioGauge = _meter.CreateObservableGauge<double>(
                "neo.mempool.capacity_ratio",
                () =>
                {
                    var memPool = _neoSystem?.MemPool;
                    return memPool != null && memPool.Capacity > 0
                        ? (double)memPool.Count / memPool.Capacity
                        : 0;
                },
                "ratio",
                "Ratio of mempool usage to capacity (0-1)");

            _mempoolEstimatedBytesGauge = _meter.CreateObservableGauge<long>(
                "neo.mempool.estimated_bytes",
                () => _estimatedMemPoolBytes,
                "bytes",
                "Estimated memory used by transactions in mempool");

            // Network metrics (via polling)
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

            // System metrics
            _cpuUsageGauge = _meter.CreateObservableGauge<double>(
                "process.cpu.usage",
                () => GetProcessCpuUsage(),
                "percent",
                "Process CPU usage percentage");

            _memoryWorkingSetGauge = _meter.CreateObservableGauge<long>(
                "process.memory.working_set",
                () =>
                {
                    try
                    {
                        _currentProcess?.Refresh();
                        return _currentProcess?.WorkingSet64 ?? 0;
                    }
                    catch { return 0; }
                },
                "bytes",
                "Process working set memory");

            _gcHeapSizeGauge = _meter.CreateObservableGauge<long>(
                "dotnet.gc.heap_size",
                () => GC.GetTotalMemory(false),
                "bytes",
                "GC heap size");

            _threadCountGauge = _meter.CreateObservableGauge<long>(
                "process.thread_count",
                () =>
                {
                    try
                    {
                        _currentProcess?.Refresh();
                        return _currentProcess?.Threads.Count ?? 0;
                    }
                    catch { return 0; }
                },
                "threads",
                "Process thread count");

            _nodeStartTimeGauge = _meter.CreateObservableGauge<long>(
                "neo.node.start_time",
                () => new DateTimeOffset(_nodeStartTime).ToUnixTimeSeconds(),
                "unixtime",
                "Node start time in Unix timestamp");

            _networkIdGauge = _meter.CreateObservableGauge<int>(
                "neo.network.id",
                () => (int)(_neoSystem?.Settings.Network ?? 0),
                "id",
                "Network ID");

            _isSyncingGauge = _meter.CreateObservableGauge<int>(
                "neo.blockchain.is_syncing",
                () => IsNodeSyncing() ? 1 : 0,
                "bool",
                "Whether the node is currently syncing (1=syncing, 0=synced)");

            // Initialize OpenTelemetry provider
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

            try
            {
                lock (_metricsLock)
                {
                    // Start timing block processing
                    _blockProcessingStopwatch = Stopwatch.StartNew();

                    // Count transactions
                    _transactionsProcessedCounter?.Add(block.Transactions.Length);

                    // Count contract invocations
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
            catch (Exception ex)
            {
                ConsoleHelper.Warning($"Error in Blockchain_Committing_Handler: {ex.Message}");
            }
        }

        void ICommittedHandler.Blockchain_Committed_Handler(NeoSystem system, Block block)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;

            try
            {
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
            catch (Exception ex)
            {
                ConsoleHelper.Warning($"Error in Blockchain_Committed_Handler: {ex.Message}");
            }
        }

        private void OnMemPoolMetricsUpdated(MemPoolMetrics metrics)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;

            lock (_metricsLock)
            {
                _estimatedMemPoolBytes = metrics.EstimatedMemoryBytes;
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
                return Math.Round(blockDiff / timeDiff, 2);
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
                            return Math.Round(Math.Min(100.0, Math.Max(0.0, cpuUsage)), 2);
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
                // Ignore CPU calculation errors
            }
            return 0;
        }

        private bool IsNodeSyncing()
        {
            if (_neoSystem == null) return false;

            try
            {
                using var snapshot = _neoSystem.GetSnapshotCache();
                var currentHeight = NativeContract.Ledger.CurrentIndex(snapshot);
                var headerHeight = _neoSystem.HeaderCache?.Count > 0
                    ? _neoSystem.HeaderCache.Last?.Index ?? currentHeight
                    : currentHeight;

                return headerHeight - currentHeight > 10;
            }
            catch
            {
                return false;
            }
        }

        private string GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }

        private void LogAvailableMetrics()
        {
            ConsoleHelper.Info("Available metrics:");
            ConsoleHelper.Info("  Blockchain: height, processing_rate, blocks_processed, transactions_processed");
            ConsoleHelper.Info("  MemPool: size, verified_count, unverified_count, capacity_ratio, estimated_bytes");
            ConsoleHelper.Info("  Network: connected_peers, unconnected_peers");
            ConsoleHelper.Info("  System: cpu_usage, memory, gc_heap, threads, start_time");
            ConsoleHelper.Info("  Performance: block_processing_time, verification_failures");
            ConsoleHelper.Warning("Note: Some metrics (bytes sent/received, message counts, conflicts) require core modifications and are not available");
        }

        public override void Dispose()
        {
            // Unsubscribe from events
            Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed -= ((ICommittedHandler)this).Blockchain_Committed_Handler;

            // Dispose resources
            _metricsCollector?.Dispose();
            _meterProvider?.Dispose();
            _meter?.Dispose();
            _currentProcess?.Dispose();
            _blockProcessingStopwatch = null;

            base.Dispose();
        }

        [ConsoleCommand("telemetry status", Category = "OpenTelemetry", Description = "Show telemetry status")]
        private void ShowTelemetryStatus()
        {
            ConsoleHelper.Info($"OpenTelemetry Status:");
            ConsoleHelper.Info($"  Enabled: {_settings.Enabled}");
            ConsoleHelper.Info($"  Service: {_settings.ServiceName} v{_settings.ServiceVersion}");
            ConsoleHelper.Info($"  Instance: {_settings.InstanceId}");
            ConsoleHelper.Info($"  Current Block Height: {_currentBlockHeight}");
            ConsoleHelper.Info($"  Block Processing Rate: {CalculateBlockProcessingRate():F2} blocks/sec");
            ConsoleHelper.Info($"  MemPool Size: {_neoSystem?.MemPool?.Count ?? 0}");
            ConsoleHelper.Info($"  Connected Peers: {(_neoSystem?.LocalNode as LocalNode)?.ConnectedCount ?? 0}");
            ConsoleHelper.Info($"  CPU Usage: {GetProcessCpuUsage():F2}%");
            ConsoleHelper.Info($"  Memory: {_currentProcess?.WorkingSet64 / 1024 / 1024 ?? 0} MB");

            if (_settings.Enabled && _meter != null)
            {
                ConsoleHelper.Info($"  Exporters:");
                if (_settings.Metrics.ConsoleExporter.Enabled)
                    ConsoleHelper.Info($"    - Console: Active");
                if (_settings.Metrics.PrometheusExporter.Enabled)
                    ConsoleHelper.Info($"    - Prometheus: Active on port {_settings.Metrics.PrometheusExporter.Port}");
                if (_settings.OtlpExporter.Enabled && _settings.OtlpExporter.ExportMetrics)
                    ConsoleHelper.Info($"    - OTLP: Active to {_settings.OtlpExporter.Endpoint}");
            }
        }

        [ConsoleCommand("telemetry metrics", Category = "OpenTelemetry", Description = "Show current metrics")]
        private void ShowMetrics()
        {
            if (!_settings.Enabled)
            {
                ConsoleHelper.Warning("OpenTelemetry is disabled");
                return;
            }

            ConsoleHelper.Info("Current Metrics:");
            ConsoleHelper.Info($"  Blockchain:");
            ConsoleHelper.Info($"    Height: {_currentBlockHeight}");
            ConsoleHelper.Info($"    Processing Rate: {CalculateBlockProcessingRate():F2} blocks/sec");
            ConsoleHelper.Info($"    Is Syncing: {IsNodeSyncing()}");

            var memPool = _neoSystem?.MemPool;
            if (memPool != null)
            {
                ConsoleHelper.Info($"  MemPool:");
                ConsoleHelper.Info($"    Total: {memPool.Count}");
                ConsoleHelper.Info($"    Verified: {memPool.VerifiedCount}");
                ConsoleHelper.Info($"    Unverified: {memPool.UnVerifiedCount}");
                ConsoleHelper.Info($"    Capacity: {memPool.Capacity}");
                ConsoleHelper.Info($"    Usage: {(memPool.Capacity > 0 ? (double)memPool.Count / memPool.Capacity * 100 : 0):F2}%");
            }

            if (_neoSystem?.LocalNode is LocalNode localNode)
            {
                ConsoleHelper.Info($"  Network:");
                ConsoleHelper.Info($"    Connected: {localNode.ConnectedCount}");
                ConsoleHelper.Info($"    Unconnected: {localNode.UnconnectedCount}");
            }

            ConsoleHelper.Info($"  System:");
            ConsoleHelper.Info($"    CPU: {GetProcessCpuUsage():F2}%");
            ConsoleHelper.Info($"    Memory: {_currentProcess?.WorkingSet64 / 1024 / 1024 ?? 0} MB");
            ConsoleHelper.Info($"    GC Heap: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
            ConsoleHelper.Info($"    Threads: {_currentProcess?.Threads.Count ?? 0}");
        }
    }
}
