// Copyright (C) 2015-2025 The Neo Project.
//
// SystemMetricsCollector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Telemetry.Metrics;
using System.Diagnostics;

namespace Neo.Plugins.Telemetry.Collectors
{
    /// <summary>
    /// Collects system resource metrics including CPU, memory, and thread pool statistics.
    /// </summary>
    public sealed class SystemMetricsCollector : IDisposable
    {
        private readonly string _nodeId;
        private readonly string _network;
        private readonly Process _currentProcess;
        private readonly DateTime _processStartTime;
        private TimeSpan _lastTotalProcessorTime;
        private DateTime _lastCpuCheckTime;
        private volatile bool _disposed;

        public SystemMetricsCollector(string nodeId, string network)
        {
            _nodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _currentProcess = Process.GetCurrentProcess();
            _processStartTime = _currentProcess.StartTime.ToUniversalTime();
            _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
            _lastCpuCheckTime = DateTime.UtcNow;

            // Set node start time
            MetricsDefinitions.NodeStartTime.WithLabels(_nodeId, _network)
                .Set(new DateTimeOffset(_processStartTime).ToUnixTimeSeconds());
        }

        /// <summary>
        /// Collects current system resource metrics.
        /// </summary>
        public void CollectCurrentState()
        {
            if (_disposed) return;

            try
            {
                CollectCpuMetrics();
                CollectMemoryMetrics();
                CollectGcMetrics();
                CollectThreadPoolMetrics();
                CollectUptimeMetrics();
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(SystemMetricsCollector), LogLevel.Debug,
                    $"Error collecting system metrics: {ex.Message}");
            }
        }

        private void CollectCpuMetrics()
        {
            try
            {
                _currentProcess.Refresh();
                var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;
                var currentTime = DateTime.UtcNow;

                var cpuUsedMs = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
                var totalMsPassed = (currentTime - _lastCpuCheckTime).TotalMilliseconds;

                if (totalMsPassed > 0)
                {
                    // Calculate CPU usage as a ratio (0-1) normalized by processor count
                    var cpuUsageRatio = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
                    cpuUsageRatio = Math.Min(1.0, Math.Max(0.0, cpuUsageRatio)); // Clamp to 0-1

                    MetricsDefinitions.CpuUsage.WithLabels(_nodeId, _network).Set(cpuUsageRatio);
                }

                _lastTotalProcessorTime = currentTotalProcessorTime;
                _lastCpuCheckTime = currentTime;
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(SystemMetricsCollector), LogLevel.Debug,
                    $"Error collecting CPU metrics: {ex.Message}");
            }
        }

        private void CollectMemoryMetrics()
        {
            try
            {
                _currentProcess.Refresh();

                // Working set (physical memory)
                MetricsDefinitions.MemoryUsageBytes.WithLabels(_nodeId, _network, "working_set")
                    .Set(_currentProcess.WorkingSet64);

                // Private memory
                MetricsDefinitions.MemoryUsageBytes.WithLabels(_nodeId, _network, "private")
                    .Set(_currentProcess.PrivateMemorySize64);

                // Virtual memory
                MetricsDefinitions.MemoryUsageBytes.WithLabels(_nodeId, _network, "virtual")
                    .Set(_currentProcess.VirtualMemorySize64);

                // GC heap size
                var gcMemoryInfo = GC.GetGCMemoryInfo();
                MetricsDefinitions.MemoryUsageBytes.WithLabels(_nodeId, _network, "gc_heap")
                    .Set(gcMemoryInfo.HeapSizeBytes);

                // Total allocated bytes
                MetricsDefinitions.MemoryUsageBytes.WithLabels(_nodeId, _network, "total_allocated")
                    .Set(GC.GetTotalMemory(false));
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(SystemMetricsCollector), LogLevel.Debug,
                    $"Error collecting memory metrics: {ex.Message}");
            }
        }

        private void CollectGcMetrics()
        {
            try
            {
                // GC collection counts by generation
                for (int gen = 0; gen <= GC.MaxGeneration; gen++)
                {
                    var collectionCount = GC.CollectionCount(gen);
                    MetricsDefinitions.GcCollectionCount.WithLabels(_nodeId, _network, gen.ToString())
                        .Set(collectionCount);
                }
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(SystemMetricsCollector), LogLevel.Debug,
                    $"Error collecting GC metrics: {ex.Message}");
            }
        }

        private void CollectThreadPoolMetrics()
        {
            try
            {
                ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
                ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);

                // Active worker threads = max - available
                var activeWorkerThreads = maxWorkerThreads - workerThreads;
                var activeCompletionPortThreads = maxCompletionPortThreads - completionPortThreads;

                MetricsDefinitions.ThreadPoolWorkerThreads.WithLabels(_nodeId, _network)
                    .Set(activeWorkerThreads);
                MetricsDefinitions.ThreadPoolCompletionPortThreads.WithLabels(_nodeId, _network)
                    .Set(activeCompletionPortThreads);
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(SystemMetricsCollector), LogLevel.Debug,
                    $"Error collecting thread pool metrics: {ex.Message}");
            }
        }

        private void CollectUptimeMetrics()
        {
            try
            {
                var uptime = (DateTime.UtcNow - _processStartTime).TotalSeconds;
                MetricsDefinitions.ProcessUptime.WithLabels(_nodeId, _network).Set(uptime);
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(SystemMetricsCollector), LogLevel.Debug,
                    $"Error collecting uptime metrics: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Process is a shared resource, don't dispose it
        }
    }
}
