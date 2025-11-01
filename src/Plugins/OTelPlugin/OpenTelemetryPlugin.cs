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
using Neo.SmartContract;
using Neo.VM;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using static Neo.Ledger.Blockchain;

namespace Neo.Plugins.OpenTelemetry
{
    public class OpenTelemetryPlugin : Plugin, ICommittingHandler, ICommittedHandler, IConsensusDiagnosticsHandler, IStateServiceDiagnosticsHandler, IRpcDiagnosticsHandler
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
        private ObservableGauge<int>? _unconnectedPeersGauge;

        // MemPool metrics
        private ObservableGauge<int>? _mempoolVerifiedGauge;
        private ObservableGauge<int>? _mempoolUnverifiedGauge;
        private ObservableGauge<long>? _mempoolMemoryBytesGauge;
        private Counter<long>? _mempoolConflictsCounter;
        private Histogram<int>? _mempoolBatchRemovedHistogram;
        private ObservableGauge<double>? _mempoolCapacityRatioGauge;

        // Performance metrics
        private Counter<long>? _transactionVerificationFailuresCounter;
        private ObservableGauge<double>? _blockProcessingRateGauge;

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
        private ObservableGauge<int>? _nodeHealthGauge;
        private ObservableGauge<int>? _nodeReadinessGauge;
        private ObservableGauge<long>? _nodeLastActivityGauge;
        private VmEventCounterListener? _vmCounterListener;
        private VmTraceProfileStore? _traceProfileStore;
        private Counter<long>? _consensusMessagesSentCounter;
        private Counter<long>? _consensusMessagesReceivedCounter;
        private Counter<long>? _consensusViewChangeCounter;
        private ObservableGauge<double>? _consensusFinalityGauge;
        private ObservableGauge<double>? _consensusRoundGauge;
        private ObservableGauge<double>? _consensusViewGauge;
        private ObservableGauge<int>? _consensusStateGauge;
        private double _lastConsensusFinalityMs;
        private double _lastConsensusHeight;
        private double _lastConsensusView;
        private int _lastConsensusState = -1;
        private ObservableGauge<double>? _stateRootHeightGauge;
        private ObservableGauge<double>? _stateValidatedRootHeightGauge;
        private ObservableGauge<double>? _stateRootLagGauge;
        private ObservableGauge<double>? _stateSnapshotApplyGauge;
        private ObservableGauge<double>? _stateSnapshotCommitGauge;
        private ObservableGauge<double>? _stateSnapshotHealthGauge;
        private Counter<long>? _stateValidationCounter;
        private Counter<long>? _stateValidationErrorCounter;
        private double _stateLocalRootHeight;
        private double _stateValidatedRootHeight;
        private double _stateRootLag;
        private double _stateSnapshotApplyMs;
        private double _stateSnapshotCommitMs;
        private double _stateSnapshotHealth;
        private Counter<long>? _rpcRequestsCounter;
        private Counter<long>? _rpcRequestErrorCounter;
        private Histogram<double>? _rpcRequestDurationHistogram;
        private ObservableGauge<int>? _rpcActiveRequestsGauge;
        private int _rpcActiveRequests;

        // State tracking
        private readonly object _metricsLock = new object();
        private Stopwatch? _blockProcessingStopwatch;
        private NeoSystem? _neoSystem;
        private MemoryPool? _memoryPool;
        private uint _currentBlockHeight = 0;
        private readonly Queue<(DateTime time, uint height)> _blockHistory = new Queue<(DateTime, uint)>();
        private long _lastMemPoolMemoryBytes = 0;
        private readonly DateTime _nodeStartTime = DateTime.UtcNow;
        private Process? _currentProcess;
        private DateTime _lastCpuCheck = DateTime.UtcNow;
        private TimeSpan _lastProcessorTime = TimeSpan.Zero;
        private long _lastSystemCpuIdleTicks;
        private long _lastSystemCpuTotalTicks;
        private MetricsCollector? _metricsCollector;
        private TelemetryHealthCheck? _healthCheck;
        private DateTime _lastBlockTimestamp = DateTime.UtcNow;
        private TelemetryHealthCheck.HealthStatus _lastHealthStatus = TelemetryHealthCheck.HealthStatus.Healthy;
        private string? _chainStoragePath;
        private DateTime _lastChainSizeRefresh = DateTime.MinValue;
        private long _lastChainSizeBytes;
        private double _lastDiskFreeBytes;

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
                _chainStoragePath = DiscoverChainStoragePath();
                _memoryPool = system.MemPool;

                if (!_settings.Metrics.Enabled)
                {
                    ConsoleHelper.Warning("OpenTelemetry metrics collection is disabled in configuration");
                    return;
                }

                _healthCheck = new TelemetryHealthCheck(TimeSpan.FromMinutes(1));

                InitializeMetrics();

                // Subscribe to blockchain events
                var categories = _settings.Metrics.Categories;

                if (categories.Blockchain)
                {
                    Blockchain.Committing += ((ICommittingHandler)this).Blockchain_Committing_Handler;
                }

                if (categories.Blockchain || categories.System)
                {
                    Blockchain.Committed += ((ICommittedHandler)this).Blockchain_Committed_Handler;
                }

                if (_memoryPool != null && categories.Mempool)
                {
                    _memoryPool.TransactionRemoved += OnMemPoolTransactionsRemoved;
                }

                var needsCollector = categories.Network || categories.Mempool || categories.Blockchain || categories.System || categories.State;
                if (needsCollector)
                {
                    var interval = TimeSpan.FromMilliseconds(Math.Max(1000, _settings.Metrics.Interval));
                    _metricsCollector = new MetricsCollector(system, interval);

                    if (categories.Network)
                        _metricsCollector.NetworkMetricsUpdated += OnNetworkMetricsUpdated;

                    if (categories.Mempool)
                        _metricsCollector.MemPoolMetricsUpdated += OnMemPoolMetricsUpdated;

                    if (categories.Blockchain || categories.System || categories.State)
                        _metricsCollector.BlockchainMetricsUpdated += OnBlockchainMetricsUpdated;
                }

                ConsoleHelper.Info("OpenTelemetry plugin initialized successfully");
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error($"Failed to initialize OpenTelemetry: {ex.Message}");
                _settings = OTelSettings.Default;
                RecordMetricError(AdditionalMetricNames.NodeHealthScore, ex);

                if (ExceptionPolicy == UnhandledExceptionPolicy.StopNode)
                    throw;
            }
        }

        private void InitializeMetrics()
        {
            _meter = new Meter("Neo.Blockchain", GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0");
            var categories = _settings.Metrics.Categories;

            if (categories.Blockchain)
                InitializeBlockchainMetrics();

            if (categories.Mempool)
                InitializeMempoolMetrics();

            if (categories.Network)
                InitializeNetworkMetrics();

            if (categories.System)
                InitializeSystemMetrics();

            if (categories.State)
                InitializeStateMetrics();

            if (categories.Consensus)
                InitializeConsensusMetrics();

            if (categories.Rpc)
                InitializeRpcMetrics();

            if (categories.Vm)
                InitializeVmMetrics();

            var config = GetConfiguration();
            _meterProvider = BuildMeterProvider(config);
        }

        private void InitializeBlockchainMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");

            _blocksProcessedCounter = meter.CreateCounter<long>(
                MetricNames.BlocksProcessedTotal,
                "blocks",
                "Total number of blocks processed");

            _transactionsProcessedCounter = meter.CreateCounter<long>(
                MetricNames.TransactionsProcessedTotal,
                "transactions",
                "Total number of transactions processed");

            _contractInvocationsCounter = meter.CreateCounter<long>(
                MetricNames.ContractInvocationsTotal,
                "invocations",
                "Total number of contract invocations");

            _blockProcessingTimeHistogram = meter.CreateHistogram<double>(
                MetricNames.BlockProcessingTime,
                "milliseconds",
                "Time taken to process a block");

            _blockHeightGauge = meter.CreateObservableGauge<long>(
                MetricNames.BlockchainHeight,
                () => _currentBlockHeight,
                "blocks",
                "Current blockchain height");

            _transactionVerificationFailuresCounter = meter.CreateCounter<long>(
                MetricNames.TransactionVerificationFailuresTotal,
                "failures",
                "Total number of transaction verification failures");

            _blockProcessingRateGauge = meter.CreateObservableGauge<double>(
                MetricNames.BlockProcessingRate,
                () => CalculateBlockProcessingRate(),
                "blocks/second",
                "Current block processing rate");
        }

        private void InitializeMempoolMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");

            _mempoolSizeGauge = meter.CreateObservableGauge<int>(
                MetricNames.MempoolSize,
                () => _neoSystem?.MemPool?.Count ?? 0,
                "transactions",
                "Current number of transactions in mempool");

            _mempoolVerifiedGauge = meter.CreateObservableGauge<int>(
                MetricNames.MempoolVerifiedCount,
                () => _neoSystem?.MemPool?.VerifiedCount ?? 0,
                "transactions",
                "Number of verified transactions in mempool");

            _mempoolUnverifiedGauge = meter.CreateObservableGauge<int>(
                MetricNames.MempoolUnverifiedCount,
                () => _neoSystem?.MemPool?.UnVerifiedCount ?? 0,
                "transactions",
                "Number of unverified transactions in mempool");

            _mempoolMemoryBytesGauge = meter.CreateObservableGauge<long>(
                MetricNames.MempoolMemoryBytes,
                () => _lastMemPoolMemoryBytes,
                "bytes",
                "Total memory used by transactions in mempool");

            _mempoolConflictsCounter = meter.CreateCounter<long>(
                MetricNames.MempoolConflictsTotal,
                "conflicts",
                "Total number of transaction conflicts detected");

            _mempoolBatchRemovedHistogram = meter.CreateHistogram<int>(
                MetricNames.MempoolBatchRemovedSize,
                "transactions",
                "Number of transactions removed in batch operations");

            _mempoolCapacityRatioGauge = meter.CreateObservableGauge<double>(
                MetricNames.MempoolCapacityRatio,
                () => _neoSystem?.MemPool != null ? (double)_neoSystem.MemPool.Count / _neoSystem.MemPool.Capacity : 0,
                "ratio",
                "Ratio of mempool usage to capacity (0-1)");
        }

        private void InitializeNetworkMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");

            _connectedPeersGauge = meter.CreateObservableGauge<int>(
                MetricNames.P2PConnectedPeers,
                () =>
                {
                    if (_neoSystem?.LocalNode is LocalNode localNode)
                        return localNode.ConnectedCount;
                    return 0;
                },
                "peers",
                "Number of connected P2P peers");

            _unconnectedPeersGauge = meter.CreateObservableGauge<int>(
                MetricNames.P2PUnconnectedPeers,
                () =>
                {
                    if (_neoSystem?.LocalNode is LocalNode localNode)
                        return localNode.UnconnectedCount;
                    return 0;
                },
                "peers",
                "Number of known but unconnected peers");
        }

        private void InitializeSystemMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");
            _currentProcess = Process.GetCurrentProcess();

            _cpuUsageGauge = meter.CreateObservableGauge<double>(
                MetricNames.ProcessCpuUsage,
                () => GetProcessCpuUsage(),
                "percent",
                "Process CPU usage percentage");

            _systemCpuUsageGauge = meter.CreateObservableGauge<double>(
                MetricNames.SystemCpuUsage,
                () => GetSystemCpuUsage(),
                "percent",
                "System CPU usage percentage");

            _memoryWorkingSetGauge = meter.CreateObservableGauge<long>(
                MetricNames.ProcessMemoryWorkingSet,
                () => _currentProcess?.WorkingSet64 ?? 0,
                "bytes",
                "Process working set memory");

            _memoryVirtualGauge = meter.CreateObservableGauge<long>(
                MetricNames.ProcessMemoryVirtual,
                () => _currentProcess?.VirtualMemorySize64 ?? 0,
                "bytes",
                "Process virtual memory");

            _gcHeapSizeGauge = meter.CreateObservableGauge<long>(
                MetricNames.DotnetGcHeapSize,
                () => GC.GetTotalMemory(false),
                "bytes",
                "GC heap size");

            _threadCountGauge = meter.CreateObservableGauge<long>(
                MetricNames.ProcessThreadCount,
                () => _currentProcess?.Threads.Count ?? 0,
                "threads",
                "Process thread count");

            _nodeStartTimeGauge = meter.CreateObservableGauge<long>(
                MetricNames.NodeStartTime,
                () => new DateTimeOffset(_nodeStartTime).ToUnixTimeSeconds(),
                "unixtime",
                "Node start time in Unix timestamp");

            _networkIdGauge = meter.CreateObservableGauge<int>(
                MetricNames.NetworkId,
                () => (int)(_neoSystem?.Settings.Network ?? 0),
                "id",
                "Neo network magic identifier");

            _isSyncingGauge = meter.CreateObservableGauge<int>(
                MetricNames.IsSyncing,
                () => _neoSystem != null && IsNodeSyncing() ? 1 : 0,
                "bool",
                "Whether the node is currently syncing (1=syncing, 0=synced)");

            _nodeHealthGauge = meter.CreateObservableGauge<int>(
                AdditionalMetricNames.NodeHealthScore,
                () => GetNodeHealthScore(),
                "score",
                "Overall telemetry health score (-1=unhealthy, 0=degraded, 1=healthy)");

            _nodeReadinessGauge = meter.CreateObservableGauge<int>(
                AdditionalMetricNames.NodeReadiness,
                () => GetNodeReadiness(),
                "bool",
                "Node readiness for serving RPC/consensus traffic (1=ready, 0=not ready)");

            _nodeLastActivityGauge = meter.CreateObservableGauge<long>(
                AdditionalMetricNames.NodeLastActivity,
                () => new DateTimeOffset(_lastBlockTimestamp).ToUnixTimeSeconds(),
                "unixtime",
                "UTC timestamp when the last block was persisted");

            meter.CreateObservableGauge<long>(
                AdditionalMetricNames.FileDescriptors,
                () => GetOpenFileDescriptorCount(),
                "count",
                "Number of file descriptors/handles held by the process");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.DiskFreeBytes,
                () => GetDiskFreeBytes(),
                "bytes",
                "Available disk bytes on the volume hosting the chain data");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.ChainDbSize,
                () => GetChainDbSizeBytes(),
                "bytes",
                "Approximate on-disk size of the chain database");
        }

        private void InitializeStateMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");

            _stateRootHeightGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.StateRootHeight,
                () => _stateLocalRootHeight,
                "blocks",
                "Latest state root height produced locally");

            _stateValidatedRootHeightGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.StateValidatedRootHeight,
                () => _stateValidatedRootHeight,
                "blocks",
                "Latest validated state root height observed");

            _stateRootLagGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.StateRootLag,
                () => _stateRootLag,
                "blocks",
                "Difference between blockchain height and validated state root height");

            _stateSnapshotApplyGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.StateSnapshotApplyDuration,
                () => _stateSnapshotApplyMs,
                "ms",
                "Duration to materialize state snapshot changes during block commit");

            _stateSnapshotCommitGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.StateSnapshotCommitDuration,
                () => _stateSnapshotCommitMs,
                "ms",
                "Duration spent committing state snapshots to persistent storage");

            _stateSnapshotHealthGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.StateSnapshotHealth,
                () => _stateSnapshotHealth,
                "ratio",
                "Ratio of validated to produced state roots (1 = healthy)");

            _stateValidationCounter = meter.CreateCounter<long>(
                AdditionalMetricNames.StateValidations,
                "events",
                "Total count of remote state root validations applied");

            _stateValidationErrorCounter = meter.CreateCounter<long>(
                AdditionalMetricNames.StateValidationErrors,
                "events",
                "Total count of state service validation failures");
        }

        private void InitializeConsensusMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");

            _consensusMessagesSentCounter = meter.CreateCounter<long>(
                AdditionalMetricNames.ConsensusMessagesSent,
                "messages",
                "Total number of consensus messages sent by this node");

            _consensusMessagesReceivedCounter = meter.CreateCounter<long>(
                AdditionalMetricNames.ConsensusMessagesReceived,
                "messages",
                "Total number of consensus messages received by this node");

            _consensusViewChangeCounter = meter.CreateCounter<long>(
                AdditionalMetricNames.ConsensusViewChangesTotal,
                "changes",
                "Total number of consensus view changes observed");

            _consensusFinalityGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.ConsensusTimeToFinality,
                () => _lastConsensusFinalityMs,
                "ms",
                "Time from proposal to block persistence (milliseconds)");

            _consensusRoundGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.ConsensusRound,
                () => _lastConsensusHeight,
                "blocks",
                "Latest block height observed through consensus");

            _consensusViewGauge = meter.CreateObservableGauge<double>(
                AdditionalMetricNames.ConsensusView,
                () => _lastConsensusView,
                "view",
                "Current consensus view number");

            _consensusStateGauge = meter.CreateObservableGauge<int>(
                AdditionalMetricNames.ConsensusState,
                () => _lastConsensusState,
                "index",
                "Current primary validator index as seen by this node");
        }

        private void InitializeRpcMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");

            _rpcRequestsCounter = meter.CreateCounter<long>(
                AdditionalMetricNames.RpcRequestsTotal,
                "requests",
                "Total RPC requests processed by this node");

            _rpcRequestErrorCounter = meter.CreateCounter<long>(
                AdditionalMetricNames.RpcRequestErrorsTotal,
                "requests",
                "RPC requests that returned errors");

            _rpcRequestDurationHistogram = meter.CreateHistogram<double>(
                AdditionalMetricNames.RpcRequestDuration,
                "ms",
                "RPC request latency in milliseconds");

            _rpcActiveRequestsGauge = meter.CreateObservableGauge<int>(
                AdditionalMetricNames.RpcActiveRequests,
                () => _rpcActiveRequests,
                "requests",
                "Number of in-flight RPC requests");
        }

        private void InitializeVmMetrics()
        {
            var meter = _meter ?? throw new InvalidOperationException("Meter must be initialised before creating instruments.");

            _vmCounterListener = new VmEventCounterListener();
            _traceProfileStore = new VmTraceProfileStore(System.IO.Path.Combine(RootPath, "profiles"));

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmInstructionRate,
                () => GetVmCounter("vm-instruction-rate"),
                "ops/s",
                "Average Neo VM instruction dispatch rate");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmInstructionLatency,
                () => GetVmCounter("vm-instruction-duration"),
                "ms",
                "Average Neo VM instruction execution latency in milliseconds");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmEvaluationStackDepth,
                () => GetVmCounter("vm-evaluation-stack-depth"),
                "items",
                "Current evaluation stack depth observed by EventCounters");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmInvocationStackDepth,
                () => GetVmCounter("vm-invocation-stack-depth"),
                "frames",
                "Current invocation stack depth observed by EventCounters");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmResultStackDepth,
                () => GetVmCounter("vm-result-stack-depth"),
                "items",
                "Current result stack depth observed by EventCounters");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmReferenceSweepRate,
                () => GetVmCounter("vm-ref-sweep-rate"),
                "ops/s",
                "Neo VM reference sweep operations per second");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmHotTraceRatio,
                () => GetHotTraceRatioMeasurements(),
                "ratio",
                "Hot opcode trace hit ratios per script");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmHotTraceHits,
                () => GetHotTraceHitMeasurements(),
                "hits",
                "Hot opcode trace hit counts per script");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmHotTraceMaxRatio,
                () => _traceProfileStore?.GetMaxHotRatio() ?? 0d,
                "ratio",
                "Maximum hot trace hit ratio across tracked scripts");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmHotTraceMaxHits,
                () => _traceProfileStore?.GetMaxHotHits() ?? 0d,
                "hits",
                "Maximum hot trace hit count across tracked scripts");

            meter.CreateObservableGauge<double>(
                AdditionalMetricNames.VmTraceProfileCount,
                () => _traceProfileStore?.GetProfileCount() ?? 0d,
                "profiles",
                "Number of hot opcode trace profiles maintained");

            meter.CreateObservableGauge<long>(
                AdditionalMetricNames.VmSuperInstructionPlanCount,
                () => (long)VmSuperInstructionPlanner.GetPlanCount(),
                "plans",
                "Number of super-instruction plans derived from runtime traces");
        }

        private string DiscoverChainStoragePath()
        {
            if (!string.IsNullOrWhiteSpace(_chainStoragePath))
                return _chainStoragePath;

            try
            {
                var candidates = new List<string>();
                var baseDir = AppContext.BaseDirectory;

                if (_neoSystem != null)
                {
                    var network = _neoSystem.Settings.Network.ToString("X8");
                    candidates.Add(System.IO.Path.Combine(Environment.CurrentDirectory, "Chains", network));
                    candidates.Add(System.IO.Path.Combine(baseDir, "Chains", network));
                }

                candidates.Add(System.IO.Path.Combine(Environment.CurrentDirectory, "Chains"));
                candidates.Add(System.IO.Path.Combine(baseDir, "Chains"));
                candidates.Add(Environment.CurrentDirectory);
                candidates.Add(baseDir);

                foreach (var candidate in candidates)
                {
                    if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                    {
                        _chainStoragePath = candidate;
                        return candidate;
                    }
                }
            }
            catch
            {
                // ignored
            }

            _chainStoragePath = AppContext.BaseDirectory;
            return _chainStoragePath;
        }

        private long GetOpenFileDescriptorCount()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return _currentProcess?.HandleCount ?? Process.GetCurrentProcess().HandleCount;
                }

                const string procFd = "/proc/self/fd";
                if (Directory.Exists(procFd))
                {
                    return Directory.EnumerateFiles(procFd).LongCount();
                }
            }
            catch
            {
            }

            return 0;
        }

        private double GetDiskFreeBytes()
        {
            try
            {
                RefreshStorageMetricsIfNeeded();
                return _lastDiskFreeBytes;
            }
            catch
            {
                return 0;
            }
        }

        private double GetChainDbSizeBytes()
        {
            try
            {
                RefreshStorageMetricsIfNeeded();
                return _lastChainSizeBytes;
            }
            catch
            {
                return 0;
            }
        }

        private void RefreshStorageMetricsIfNeeded()
        {
            var now = DateTime.UtcNow;
            if (now - _lastChainSizeRefresh < TimeSpan.FromSeconds(30))
                return;

            var path = DiscoverChainStoragePath();
            double diskFree = 0;
            long chainSize = 0;

            try
            {
                var root = System.IO.Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root))
                {
                    root = path;
                }

                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    if (drive.IsReady)
                    {
                        diskFree = drive.AvailableFreeSpace;
                    }
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            chainSize += new FileInfo(file).Length;
                        }
                        catch
                        {
                            // ignore transient IO errors
                        }
                    }
                }
            }
            catch
            {
                chainSize = _lastChainSizeBytes;
            }

            _lastDiskFreeBytes = diskFree;
            _lastChainSizeBytes = chainSize;
            _lastChainSizeRefresh = now;
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
            var resourceBuilder = ResourceBuilder.CreateDefault();

            if (_neoSystem != null)
            {
                var neoResource = NeoResourceAttributes.BuildNeoResource(_settings, _neoSystem).Build();
                resourceBuilder.AddAttributes(neoResource.Attributes);
            }
            else
            {
                var instanceId = string.IsNullOrWhiteSpace(_settings.InstanceId)
                    ? Environment.MachineName
                    : _settings.InstanceId;

                resourceBuilder.AddService(_settings.ServiceName,
                    serviceVersion: _settings.ServiceVersion,
                    serviceInstanceId: instanceId);
            }

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
            if (!_settings.Metrics.Categories.Blockchain) return;

            lock (_metricsLock)
            {
                // Start timing block processing
                _blockProcessingStopwatch = Stopwatch.StartNew();

                // Count transactions processed in this block
                _transactionsProcessedCounter?.Add(block.Transactions.Length);
                RecordMetricUpdate(MetricNames.TransactionsProcessedTotal);

                // Count contract invocations and track failures
                if (_contractInvocationsCounter != null || _transactionVerificationFailuresCounter != null)
                {
                    var invocations = applicationExecutedList
                        .Count(x => x.Transaction != null && x.Trigger == TriggerType.Application);

                    if (invocations > 0)
                    {
                        _contractInvocationsCounter?.Add(invocations);
                    }

                    // Track execution failures
                    var failures = applicationExecutedList
                        .Count(x => x.Trigger == TriggerType.Application && x.VMState != VM.VMState.HALT);
                    if (failures > 0)
                        _transactionVerificationFailuresCounter?.Add(failures);
                }
            }
        }

        void ICommittedHandler.Blockchain_Committed_Handler(NeoSystem system, Block block)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;

            var categories = _settings.Metrics.Categories;
            if (!categories.Blockchain && !categories.System && !categories.State)
            {
                return;
            }

            lock (_metricsLock)
            {
                var now = DateTime.UtcNow;
                _lastBlockTimestamp = now;

                // Record block processing time
                if (categories.Blockchain && _blockProcessingStopwatch != null)
                {
                    _blockProcessingTimeHistogram?.Record(_blockProcessingStopwatch.ElapsedMilliseconds);
                    _blockProcessingStopwatch = null;
                    RecordMetricUpdate(MetricNames.BlockProcessingTime);
                }

                // Update block height
                _currentBlockHeight = block.Index;

                if (categories.Blockchain)
                {
                    // Track block processing history for rate calculation
                    _blockHistory.Enqueue((now, block.Index));

                    // Keep only last 60 seconds of history
                    while (_blockHistory.Count > 0 && (now - _blockHistory.Peek().time).TotalSeconds > 60)
                    {
                        _blockHistory.Dequeue();
                    }

                    // Increment blocks processed counter
                    _blocksProcessedCounter?.Add(1);
                    RecordMetricUpdate(MetricNames.BlocksProcessedTotal);
                }

                if (categories.System)
                {
                    RecordMetricUpdate(AdditionalMetricNames.NodeLastActivity);
                }
            }
        }

        public override void Dispose()
        {
            // Unsubscribe from events
            Blockchain.Committing -= ((ICommittingHandler)this).Blockchain_Committing_Handler;
            Blockchain.Committed -= ((ICommittedHandler)this).Blockchain_Committed_Handler;
            if (_memoryPool != null)
            {
                _memoryPool.TransactionRemoved -= OnMemPoolTransactionsRemoved;
                _memoryPool = null;
            }

            // Dispose metrics collector
            if (_metricsCollector != null)
            {
                _metricsCollector.NetworkMetricsUpdated -= OnNetworkMetricsUpdated;
                _metricsCollector.MemPoolMetricsUpdated -= OnMemPoolMetricsUpdated;
                _metricsCollector.BlockchainMetricsUpdated -= OnBlockchainMetricsUpdated;
                _metricsCollector.Dispose();
                _metricsCollector = null;
            }

            _healthCheck?.Dispose();
            _healthCheck = null;

            _vmCounterListener?.Shutdown();
            _vmCounterListener = null;

            _traceProfileStore?.Dispose();
            _traceProfileStore = null;

            _meterProvider?.Dispose();
            _meter?.Dispose();
            _blockProcessingStopwatch = null;
            base.Dispose();
        }

        [ConsoleCommand("telemetry plans", Category = "OpenTelemetry", Description = "List super-instruction plan suggestions")]
        private void ShowTelemetryPlans(int count = 10)
        {
            var suggestions = VmSuperInstructionPlanner.GetPlanSuggestions(Math.Max(1, count));
            if (suggestions.Count == 0)
            {
                ConsoleHelper.Info("No super-instruction plans currently recorded.");
                return;
            }

            ConsoleHelper.Info($"Top {suggestions.Count} super-instruction plans:");
            foreach (var suggestion in suggestions)
            {
                ConsoleHelper.Info($"  Script={suggestion.Script} | Hits={suggestion.HitCount} | Ratio={suggestion.HitRatio:P2} | Sequence={suggestion.Sequence}");
            }
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
            ConsoleHelper.Info($"  Health: {_lastHealthStatus} (last block at {_lastBlockTimestamp:u})");
            ConsoleHelper.Info($"  Ready: {(IsNodeSyncing() ? "No" : "Yes")}");
            var planCount = VmSuperInstructionPlanner.GetPlanCount();
            ConsoleHelper.Info($"  VM Super-Instructions: {planCount} active plan(s)");
            if (planCount > 0)
            {
                foreach (var plan in VmSuperInstructionPlanner.GetPlanSuggestions(3))
                {
                    ConsoleHelper.Info($"    - {plan.Sequence} (hash={plan.Script}, hits={plan.HitCount}, ratio={plan.HitRatio:P1})");
                }
            }

            if (_settings.Enabled && _meter != null)
            {
                ConsoleHelper.Info("  Metrics Categories:");
                var categories = _settings.Metrics.Categories;
                ConsoleHelper.Info($"    - Blockchain: {(categories.Blockchain ? "Enabled" : "Disabled")}");
                ConsoleHelper.Info($"    - Mempool: {(categories.Mempool ? "Enabled" : "Disabled")}");
                ConsoleHelper.Info($"    - Network: {(categories.Network ? "Enabled" : "Disabled")}");
                ConsoleHelper.Info($"    - System: {(categories.System ? "Enabled" : "Disabled")}");
                ConsoleHelper.Info($"    - Consensus: {(categories.Consensus ? "Enabled" : "Disabled")}");
                ConsoleHelper.Info($"    - State: {(categories.State ? "Enabled" : "Disabled")}");
                ConsoleHelper.Info($"    - VM: {(categories.Vm ? "Enabled" : "Disabled")}");
                ConsoleHelper.Info($"    - RPC: {(categories.Rpc ? "Enabled" : "Disabled")}");
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
            if (!_settings.Metrics.Categories.Network) return;
            RecordMetricUpdate(MetricNames.P2PConnectedPeers);
            RecordMetricUpdate(MetricNames.P2PUnconnectedPeers);
        }

        private void OnMemPoolMetricsUpdated(MemPoolMetrics metrics)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;
            if (!_settings.Metrics.Categories.Mempool) return;

            lock (_metricsLock)
            {
                // Update memory bytes estimate
                _lastMemPoolMemoryBytes = metrics.EstimatedMemoryBytes;
            }

            RecordMetricUpdate(MetricNames.MempoolSize);
            RecordMetricUpdate(MetricNames.MempoolVerifiedCount);
            RecordMetricUpdate(MetricNames.MempoolUnverifiedCount);
            RecordMetricUpdate(MetricNames.MempoolMemoryBytes);
            RecordMetricUpdate(MetricNames.MempoolCapacityRatio);
        }

        private void OnMemPoolTransactionsRemoved(object? sender, TransactionRemovedEventArgs e)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;
            if (!_settings.Metrics.Categories.Mempool) return;
            if (e.Transactions == null || e.Transactions.Count == 0) return;

            lock (_metricsLock)
            {
                _mempoolBatchRemovedHistogram?.Record(e.Transactions.Count);

                if (e.Reason == TransactionRemovalReason.Conflict)
                {
                    _mempoolConflictsCounter?.Add(e.Transactions.Count);
                }
            }
        }

        private void OnBlockchainMetricsUpdated(BlockchainMetrics metrics)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;

            var categories = _settings.Metrics.Categories;
            if (!categories.Blockchain && !categories.State && !categories.System) return;

            lock (_metricsLock)
            {
                _currentBlockHeight = metrics.CurrentHeight;
            }

            if (categories.Blockchain)
                RecordMetricUpdate(MetricNames.BlockchainHeight);

            if (categories.System)
                RecordMetricUpdate(MetricNames.IsSyncing);
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

        private double GetVmCounter(string counterName)
        {
            return _vmCounterListener?.GetValue(counterName) ?? 0d;
        }

        private IEnumerable<Measurement<double>> GetHotTraceRatioMeasurements()
        {
            if (_traceProfileStore == null)
                yield break;

            foreach (var profile in _traceProfileStore.GetTopProfiles(10))
            {
                yield return new Measurement<double>(
                    profile.HitRatio,
                    new KeyValuePair<string, object?>("script", profile.ScriptHash),
                    new KeyValuePair<string, object?>("sequence", profile.HotSequence),
                    new KeyValuePair<string, object?>("hits", profile.HitCount),
                    new KeyValuePair<string, object?>("total_instructions", profile.TotalInstructions),
                    new KeyValuePair<string, object?>("last_seen", profile.LastUpdatedUtc.ToUnixTimeSeconds()));
            }
        }

        private IEnumerable<Measurement<double>> GetHotTraceHitMeasurements()
        {
            if (_traceProfileStore == null)
                yield break;

            foreach (var profile in _traceProfileStore.GetTopProfiles(10))
            {
                yield return new Measurement<double>(
                    profile.HitCount,
                    new KeyValuePair<string, object?>("script", profile.ScriptHash),
                    new KeyValuePair<string, object?>("sequence", profile.HotSequence),
                    new KeyValuePair<string, object?>("total_instructions", profile.TotalInstructions),
                    new KeyValuePair<string, object?>("last_seen", profile.LastUpdatedUtc.ToUnixTimeSeconds()));
            }
        }

        private void RecordMetricUpdate(string metricName)
        {
            try
            {
                _healthCheck?.RecordMetricUpdate(metricName);
            }
            catch
            {
                // Intentionally ignored; health tracking should not interfere with metrics
            }
        }

        private void RecordMetricError(string metricName, Exception ex)
        {
            try
            {
                _healthCheck?.RecordMetricError(metricName, ex);
            }
            catch
            {
                // Ignore health tracking errors
            }
        }

        private int GetNodeHealthScore()
        {
            if (_healthCheck == null) return 0;

            try
            {
                var report = _healthCheck.GetHealthReport();
                _lastHealthStatus = report.Status;
                RecordMetricUpdate(AdditionalMetricNames.NodeHealthScore);
                return report.Status switch
                {
                    TelemetryHealthCheck.HealthStatus.Healthy => 1,
                    TelemetryHealthCheck.HealthStatus.Degraded => 0,
                    TelemetryHealthCheck.HealthStatus.Unhealthy => -1,
                    _ => 0
                };
            }
            catch
            {
                return 0;
            }
        }

        private int GetNodeReadiness()
        {
            var readiness = _neoSystem != null && !IsNodeSyncing() ? 1 : 0;
            RecordMetricUpdate(AdditionalMetricNames.NodeReadiness);
            return readiness;
        }

        void IConsensusDiagnosticsHandler.OnConsensusTelemetry(ConsensusTelemetryEventArgs args)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;
            if (!_settings.Metrics.Categories.Consensus) return;

            lock (_metricsLock)
            {
                switch (args.EventType)
                {
                    case ConsensusTelemetryEventType.ConsensusStarted:
                        _lastConsensusHeight = args.Height;
                        _lastConsensusView = args.ViewNumber;
                        _lastConsensusState = args.PrimaryIndex;
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusRound);
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusView);
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusState);
                        break;
                    case ConsensusTelemetryEventType.ViewChanged:
                        _lastConsensusView = args.ViewNumber;
                        _lastConsensusState = args.PrimaryIndex;
                        _consensusViewChangeCounter?.Add(1, new KeyValuePair<string, object?>("reason", args.Reason ?? "unknown"));
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusViewChangesTotal);
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusView);
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusState);
                        break;
                    case ConsensusTelemetryEventType.BlockPersisted:
                        _lastConsensusHeight = args.Height;
                        _lastConsensusState = args.PrimaryIndex;
                        if (args.Duration.HasValue && args.Duration.Value >= TimeSpan.Zero)
                        {
                            _lastConsensusFinalityMs = args.Duration.Value.TotalMilliseconds;
                            RecordMetricUpdate(AdditionalMetricNames.ConsensusTimeToFinality);
                        }
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusRound);
                        RecordMetricUpdate(AdditionalMetricNames.ConsensusState);
                        break;
                    case ConsensusTelemetryEventType.MessageSent:
                        if (args.MessageKind.HasValue)
                        {
                            _consensusMessagesSentCounter?.Add(1, new KeyValuePair<string, object?>("type", args.MessageKind.Value.ToString()));
                            RecordMetricUpdate(AdditionalMetricNames.ConsensusMessagesSent);
                        }
                        break;
                    case ConsensusTelemetryEventType.MessageReceived:
                        if (args.MessageKind.HasValue)
                        {
                            _consensusMessagesReceivedCounter?.Add(1, new KeyValuePair<string, object?>("type", args.MessageKind.Value.ToString()));
                            RecordMetricUpdate(AdditionalMetricNames.ConsensusMessagesReceived);
                        }
                        break;
                    case ConsensusTelemetryEventType.RecoveryRequested:
                        // No additional gauge state to update beyond message metrics which are already recorded.
                        break;
                }
            }
        }

        void IStateServiceDiagnosticsHandler.OnStateServiceTelemetry(StateServiceTelemetryEventArgs args)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;
            if (!_settings.Metrics.Categories.State) return;

            lock (_metricsLock)
            {
                switch (args.EventType)
                {
                    case StateServiceTelemetryEventType.SnapshotApplied:
                        _stateLocalRootHeight = args.LocalRootIndex ?? args.Height;
                        if (args.Duration.HasValue)
                        {
                            _stateSnapshotApplyMs = Math.Max(0d, args.Duration.Value.TotalMilliseconds);
                            RecordMetricUpdate(AdditionalMetricNames.StateSnapshotApplyDuration);
                        }
                        break;
                    case StateServiceTelemetryEventType.LocalRootCommitted:
                        _stateLocalRootHeight = args.LocalRootIndex ?? args.Height;
                        if (args.Duration.HasValue)
                        {
                            _stateSnapshotCommitMs = Math.Max(0d, args.Duration.Value.TotalMilliseconds);
                            RecordMetricUpdate(AdditionalMetricNames.StateSnapshotCommitDuration);
                        }
                        break;
                    case StateServiceTelemetryEventType.ValidatedRootAdvanced:
                        _stateValidatedRootHeight = args.ValidatedRootIndex ?? args.Height;
                        _stateValidationCounter?.Add(1);
                        RecordMetricUpdate(AdditionalMetricNames.StateValidations);
                        break;
                    case StateServiceTelemetryEventType.SnapshotError:
                        _stateValidationErrorCounter?.Add(1,
                            new KeyValuePair<string, object?>("stage", args.Stage ?? "unknown"),
                            new KeyValuePair<string, object?>("reason", args.Reason ?? "unknown"));
                        RecordMetricUpdate(AdditionalMetricNames.StateValidationErrors);
                        break;
                }

                var blockchainHeight = _currentBlockHeight;
                _stateRootLag = Math.Max(0d, blockchainHeight - _stateValidatedRootHeight);

                if (_stateLocalRootHeight <= 0d)
                {
                    _stateSnapshotHealth = 0d;
                }
                else
                {
                    var validated = Math.Clamp(_stateValidatedRootHeight, 0d, _stateLocalRootHeight);
                    _stateSnapshotHealth = Math.Clamp(validated / Math.Max(1d, _stateLocalRootHeight), 0d, 1d);
                }

                RecordMetricUpdate(AdditionalMetricNames.StateRootHeight);
                RecordMetricUpdate(AdditionalMetricNames.StateValidatedRootHeight);
                RecordMetricUpdate(AdditionalMetricNames.StateRootLag);
                RecordMetricUpdate(AdditionalMetricNames.StateSnapshotHealth);
            }
        }

        void IRpcDiagnosticsHandler.OnRpcTelemetry(RpcTelemetryEventArgs args)
        {
            if (!_settings.Enabled || !_settings.Metrics.Enabled) return;
            if (!_settings.Metrics.Categories.Rpc) return;

            lock (_metricsLock)
            {
                switch (args.EventType)
                {
                    case RpcTelemetryEventType.Started:
                        _rpcActiveRequests++;
                        RecordMetricUpdate(AdditionalMetricNames.RpcActiveRequests);
                        break;
                    case RpcTelemetryEventType.Completed:
                        if (_rpcActiveRequests > 0)
                            _rpcActiveRequests--;
                        else
                            _rpcActiveRequests = 0;

                        var methodTag = new KeyValuePair<string, object?>("method", args.Method);
                        _rpcRequestsCounter?.Add(1, methodTag);
                        RecordMetricUpdate(AdditionalMetricNames.RpcRequestsTotal);

                        if (args.Success == false)
                        {
                            _rpcRequestErrorCounter?.Add(1,
                                methodTag,
                                new KeyValuePair<string, object?>("code", args.ErrorCode ?? 0),
                                new KeyValuePair<string, object?>("message", args.ErrorMessage ?? string.Empty));
                            RecordMetricUpdate(AdditionalMetricNames.RpcRequestErrorsTotal);
                        }

                        if (args.Duration.HasValue)
                        {
                            _rpcRequestDurationHistogram?.Record(
                                args.Duration.Value.TotalMilliseconds,
                                methodTag,
                                new KeyValuePair<string, object?>("result", args.Success == true ? "success" : "error"));
                        }
                        break;
                }
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
                if (OperatingSystem.IsWindows())
                {
                    if (GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
                    {
                        var idle = FileTimeToLong(idleTime);
                        var kernel = FileTimeToLong(kernelTime);
                        var user = FileTimeToLong(userTime);
                        var total = kernel + user;
                        return CalculateSystemCpuUsage(idle, total);
                    }
                }
                else if (OperatingSystem.IsLinux() && File.Exists("/proc/stat"))
                {
                    var line = File.ReadLines("/proc/stat").FirstOrDefault();
                    if (line != null && line.StartsWith("cpu "))
                    {
                        var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Skip(1)
                            .Select(value => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0)
                            .ToArray();

                        if (values.Length >= 4)
                        {
                            long user = values[0];
                            long nice = values.Length > 1 ? values[1] : 0;
                            long system = values.Length > 2 ? values[2] : 0;
                            long idle = values.Length > 3 ? values[3] : 0;
                            long iowait = values.Length > 4 ? values[4] : 0;
                            long irq = values.Length > 5 ? values[5] : 0;
                            long softirq = values.Length > 6 ? values[6] : 0;
                            long steal = values.Length > 7 ? values[7] : 0;

                            long idleAll = idle + iowait;
                            long total = user + nice + system + idleAll + irq + softirq + steal;

                            return CalculateSystemCpuUsage(idleAll, total);
                        }
                    }
                }
            }
            catch
            {
                // ignored, we'll fall back to process CPU usage
            }

            // Fallback to process CPU if system CPU cannot be determined
            return GetProcessCpuUsage();
        }

        private double CalculateSystemCpuUsage(long idleTicks, long totalTicks)
        {
            if (totalTicks <= 0) return 0;

            if (_lastSystemCpuTotalTicks != 0 && totalTicks > _lastSystemCpuTotalTicks)
            {
                var totalDiff = totalTicks - _lastSystemCpuTotalTicks;
                var idleDiff = idleTicks - _lastSystemCpuIdleTicks;

                _lastSystemCpuIdleTicks = idleTicks;
                _lastSystemCpuTotalTicks = totalTicks;

                if (totalDiff <= 0) return 0;

                var usage = (double)(totalDiff - idleDiff) / totalDiff * 100.0;
                return Math.Min(100.0, Math.Max(0.0, usage));
            }

            _lastSystemCpuIdleTicks = idleTicks;
            _lastSystemCpuTotalTicks = totalTicks;
            return 0;
        }

        [SupportedOSPlatform("windows")]
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        private static long FileTimeToLong(FILETIME fileTime)
        {
            return ((long)fileTime.dwHighDateTime << 32) + fileTime.dwLowDateTime;
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
