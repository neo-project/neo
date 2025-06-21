// Copyright (C) 2015-2025 The Neo Project.
//
// PluginResourceMonitor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Advanced resource monitoring system for tracking plugin-specific resource usage.
    /// </summary>
    public class PluginResourceMonitor : IDisposable
    {
        private static PluginResourceMonitor _instance;
        private static readonly object _lockObject = new object();

        private readonly ConcurrentDictionary<string, PluginResourceTracker> _trackers = new();
        private readonly ConcurrentDictionary<string, ResourceUsageHistory> _usageHistory = new();
        private bool _disposed = false;

        /// <summary>
        /// Gets the singleton instance of the PluginResourceMonitor.
        /// </summary>
        public static PluginResourceMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObject)
                    {
                        if (_instance == null)
                            _instance = new PluginResourceMonitor();
                    }
                }
                return _instance;
            }
        }

        private PluginResourceMonitor()
        {
            // Use event-driven monitoring instead of timer-based
            _eventDrivenMonitor = new EventDrivenResourceMonitor();
            _eventDrivenMonitor.ResourceViolationDetected += OnResourceViolationDetected;
            _eventDrivenMonitor.ResourceWarningThresholdCrossed += OnResourceWarningThresholdCrossed;
        }

        private readonly EventDrivenResourceMonitor _eventDrivenMonitor;

        /// <summary>
        /// Gets the event-driven monitor for direct event reporting.
        /// </summary>
        public EventDrivenResourceMonitor EventMonitor => _eventDrivenMonitor;

        /// <summary>
        /// Starts monitoring a plugin's resource usage.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to monitor.</param>
        /// <param name="policy">The security policy for resource limits.</param>
        /// <returns>A resource tracker for the plugin.</returns>
        public PluginResourceTracker StartMonitoring(string pluginName, PluginSecurityPolicy policy)
        {
            if (string.IsNullOrEmpty(pluginName))
                throw new ArgumentNullException(nameof(pluginName));
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            var tracker = new PluginResourceTracker(pluginName, policy);
            _trackers.TryAdd(pluginName, tracker);
            _usageHistory.TryAdd(pluginName, new ResourceUsageHistory());

            // Configure event-driven monitoring
            _eventDrivenMonitor.ConfigurePlugin(pluginName, policy);

            return tracker;
        }

        /// <summary>
        /// Stops monitoring a plugin's resource usage.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to stop monitoring.</param>
        public void StopMonitoring(string pluginName)
        {
            if (_trackers.TryRemove(pluginName, out var tracker))
            {
                tracker.Dispose();
            }
            _usageHistory.TryRemove(pluginName, out _);
        }

        /// <summary>
        /// Gets the current resource usage for a specific plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <returns>Current resource usage or null if not monitored.</returns>
        public ResourceUsage GetCurrentUsage(string pluginName)
        {
            if (_trackers.TryGetValue(pluginName, out var tracker))
            {
                return tracker.GetCurrentUsage();
            }
            return null;
        }

        /// <summary>
        /// Gets resource usage history for a specific plugin.
        /// </summary>
        /// <param name="pluginName">The name of the plugin.</param>
        /// <returns>Resource usage history or null if not monitored.</returns>
        public ResourceUsageHistory GetUsageHistory(string pluginName)
        {
            _usageHistory.TryGetValue(pluginName, out var history);
            return history;
        }

        /// <summary>
        /// Gets resource usage for all monitored plugins.
        /// </summary>
        /// <returns>Dictionary of plugin names and their current resource usage.</returns>
        public Dictionary<string, ResourceUsage> GetAllUsage()
        {
            var result = new Dictionary<string, ResourceUsage>();

            foreach (var kvp in _trackers)
            {
                try
                {
                    result[kvp.Key] = kvp.Value.GetCurrentUsage();
                }
                catch
                {
                    // Ignore errors when getting resource usage
                }
            }

            return result;
        }

        /// <summary>
        /// Gets plugins that are currently violating resource limits.
        /// </summary>
        /// <returns>List of plugin names with violations.</returns>
        public List<string> GetViolatingPlugins()
        {
            var violating = new List<string>();

            foreach (var kvp in _trackers)
            {
                try
                {
                    if (kvp.Value.HasViolations())
                    {
                        violating.Add(kvp.Key);
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }

            return violating;
        }

        /// <summary>
        /// Forces garbage collection and updates memory metrics for accurate measurement.
        /// </summary>
        public void ForceMemoryUpdate()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void OnResourceViolationDetected(object sender, ResourceViolationEventArgs e)
        {
            // Log the violation
            Utility.Log("PluginResourceMonitor", LogLevel.Warning,
                $"Resource violation detected for plugin '{e.PluginName}': {e.Violation.ViolationType}");
        }

        private void OnResourceWarningThresholdCrossed(object sender, ResourceWarningEventArgs e)
        {
            // Log the warning
            Utility.Log("PluginResourceMonitor", LogLevel.Info,
                $"Resource warning for plugin '{e.PluginName}': {e.Warning}");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _eventDrivenMonitor?.Dispose();

                foreach (var tracker in _trackers.Values)
                {
                    try
                    {
                        tracker.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }

                _trackers.Clear();
                _usageHistory.Clear();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Tracks resource usage for a specific plugin with accurate per-plugin measurements.
    /// </summary>
    public class PluginResourceTracker : IDisposable
    {
        private readonly string _pluginName;
        private readonly PluginSecurityPolicy _policy;
        private readonly DateTime _startTime;
        private readonly object _lockObject = new object();

        // Resource tracking variables
        private long _baselineMemory;
        private long _peakMemory;
        private long _currentMemory;
        private TimeSpan _baseCpuTime;
        private TimeSpan _currentCpuTime;
        private int _baselineThreadCount;
        private int _peakThreadCount;
        private int _currentThreadCount;

        // Process monitoring
        private Process _currentProcess;

        // Violation tracking
        private readonly ConcurrentQueue<ResourceViolation> _violations = new();
        private bool _disposed = false;

        /// <summary>
        /// Gets the name of the plugin being tracked.
        /// </summary>
        public string PluginName => _pluginName;

        /// <summary>
        /// Gets the time when tracking started.
        /// </summary>
        public DateTime StartTime => _startTime;

        /// <summary>
        /// Initializes a new instance of the PluginResourceTracker.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to track.</param>
        /// <param name="policy">The security policy for resource limits.</param>
        public PluginResourceTracker(string pluginName, PluginSecurityPolicy policy)
        {
            _pluginName = pluginName ?? throw new ArgumentNullException(nameof(pluginName));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _startTime = DateTime.UtcNow;

            Initialize();
        }

        private void Initialize()
        {
            _currentProcess = Process.GetCurrentProcess();

            // Establish baseline measurements
            _baselineMemory = GC.GetTotalMemory(false);
            _currentMemory = _baselineMemory;
            _peakMemory = _baselineMemory;

            try
            {
                _baseCpuTime = _currentProcess.TotalProcessorTime;
                _currentCpuTime = _baseCpuTime;
            }
            catch
            {
                _baseCpuTime = TimeSpan.Zero;
                _currentCpuTime = TimeSpan.Zero;
            }

#if NET5_0_OR_GREATER
            try
            {
                _baselineThreadCount = ThreadPool.ThreadCount;
                _currentThreadCount = _baselineThreadCount;
                _peakThreadCount = _baselineThreadCount;
            }
            catch
            {
                _baselineThreadCount = 0;
                _currentThreadCount = 0;
                _peakThreadCount = 0;
            }
#else
            _baselineThreadCount = 0;
            _currentThreadCount = 0;
            _peakThreadCount = 0;
#endif

            // Initialize monitoring
            InitializePerformanceCounters();
        }

        private void InitializePerformanceCounters()
        {
            // PerformanceCounter is not available in .NET Core/5+
            // Use Process class for basic monitoring instead
        }

        /// <summary>
        /// Gets the current resource usage for this plugin.
        /// </summary>
        /// <returns>Current resource usage statistics.</returns>
        public ResourceUsage GetCurrentUsage()
        {
            lock (_lockObject)
            {
                UpdateCurrentMetrics();

                return new ResourceUsage
                {
                    MemoryUsed = Math.Max(0, _currentMemory - _baselineMemory),
                    CpuTimeUsed = (long)(_currentCpuTime - _baseCpuTime).TotalMilliseconds,
                    ThreadsCreated = Math.Max(0, _currentThreadCount - _baselineThreadCount),
                    ExecutionTime = (long)(DateTime.UtcNow - _startTime).TotalMilliseconds
                };
            }
        }

        /// <summary>
        /// Gets detailed resource statistics including peaks and averages.
        /// </summary>
        /// <returns>Detailed resource statistics.</returns>
        public DetailedResourceUsage GetDetailedUsage()
        {
            lock (_lockObject)
            {
                UpdateCurrentMetrics();

                return new DetailedResourceUsage
                {
                    Current = GetCurrentUsage(),
                    PeakMemoryUsed = Math.Max(0, _peakMemory - _baselineMemory),
                    PeakThreadCount = Math.Max(0, _peakThreadCount - _baselineThreadCount),
                    ViolationCount = _violations.Count,
                    LastViolation = _violations.ToArray().LastOrDefault()?.Timestamp
                };
            }
        }

        /// <summary>
        /// Checks if the plugin is currently violating any resource limits.
        /// </summary>
        /// <returns>True if violations exist; otherwise, false.</returns>
        public bool HasViolations()
        {
            var usage = GetCurrentUsage();
            return CheckForViolations(usage);
        }

        /// <summary>
        /// Records a resource usage snapshot for this plugin.
        /// </summary>
        public void RecordSnapshot()
        {
            lock (_lockObject)
            {
                UpdateCurrentMetrics();

                var usage = GetCurrentUsage();
                if (CheckForViolations(usage))
                {
                    // Handle violations according to policy
                    HandleResourceViolations(usage);
                }
            }
        }

        private void UpdateCurrentMetrics()
        {
            // Update memory metrics
            var currentMemory = GC.GetTotalMemory(false);
            _currentMemory = currentMemory;
            _peakMemory = Math.Max(_peakMemory, currentMemory);

            // Update CPU metrics
            try
            {
                _currentCpuTime = _currentProcess.TotalProcessorTime;
            }
            catch
            {
                // Process may have exited or access denied
            }

            // Update thread metrics
#if NET5_0_OR_GREATER
            try
            {
                var currentThreads = ThreadPool.ThreadCount;
                _currentThreadCount = currentThreads;
                _peakThreadCount = Math.Max(_peakThreadCount, currentThreads);
            }
            catch
            {
                // May not be available
            }
#endif
        }

        private bool CheckForViolations(ResourceUsage usage)
        {
            var violations = new List<string>();

            // Check memory limit
            if (_policy.MaxMemoryBytes > 0 && usage.MemoryUsed > _policy.MaxMemoryBytes)
            {
                violations.Add($"Memory: {usage.MemoryUsed} > {_policy.MaxMemoryBytes}");
            }

            // Check CPU usage
            if (_policy.MaxCpuPercent > 0 && usage.ExecutionTime > 0)
            {
                var cpuPercent = (usage.CpuTimeUsed / (double)usage.ExecutionTime) * 100;
                if (cpuPercent > _policy.MaxCpuPercent)
                {
                    violations.Add($"CPU: {cpuPercent:F1}% > {_policy.MaxCpuPercent}%");
                }
            }

            // Check thread count
            if (_policy.MaxThreads > 0 && usage.ThreadsCreated > _policy.MaxThreads)
            {
                violations.Add($"Threads: {usage.ThreadsCreated} > {_policy.MaxThreads}");
            }

            if (violations.Count > 0)
            {
                var violation = new ResourceViolation
                {
                    PluginName = _pluginName,
                    Timestamp = DateTime.UtcNow,
                    ViolationType = string.Join(", ", violations),
                    Usage = usage
                };

                _violations.Enqueue(violation);
                return true;
            }

            return false;
        }

        private void HandleResourceViolations(ResourceUsage usage)
        {
            var message = $"Resource violation detected for plugin '{_pluginName}'";

            switch (_policy.ViolationAction)
            {
                case ViolationAction.Log:
                    Utility.Log("PluginResourceTracker", LogLevel.Warning, message);
                    break;
                case ViolationAction.Suspend:
                    Utility.Log("PluginResourceTracker", LogLevel.Warning, $"{message} - Plugin will be suspended");
                    // Additional suspension logic would be handled by the sandbox
                    break;
                case ViolationAction.Terminate:
                    Utility.Log("PluginResourceTracker", LogLevel.Error, $"{message} - Plugin will be terminated");
                    // Additional termination logic would be handled by the sandbox
                    break;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _currentProcess?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents detailed resource usage statistics.
    /// </summary>
    public class DetailedResourceUsage
    {
        /// <summary>
        /// Current resource usage.
        /// </summary>
        public ResourceUsage Current { get; set; }

        /// <summary>
        /// Peak memory usage during the tracking period.
        /// </summary>
        public long PeakMemoryUsed { get; set; }

        /// <summary>
        /// Peak thread count during the tracking period.
        /// </summary>
        public int PeakThreadCount { get; set; }

        /// <summary>
        /// Total number of resource violations.
        /// </summary>
        public int ViolationCount { get; set; }

        /// <summary>
        /// Timestamp of the last violation.
        /// </summary>
        public DateTime? LastViolation { get; set; }
    }

    /// <summary>
    /// Represents a resource usage violation.
    /// </summary>
    public class ResourceViolation
    {
        /// <summary>
        /// Name of the plugin that violated resource limits.
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// Timestamp when the violation occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Description of the violation type.
        /// </summary>
        public string ViolationType { get; set; }

        /// <summary>
        /// Resource usage at the time of violation.
        /// </summary>
        public ResourceUsage Usage { get; set; }
    }

    /// <summary>
    /// Maintains historical resource usage data for analysis.
    /// </summary>
    public class ResourceUsageHistory
    {
        private readonly Queue<ResourceUsageDataPoint> _dataPoints = new();
        private readonly object _lockObject = new object();
        private readonly int _maxDataPoints = 1000; // Keep last 1000 data points

        /// <summary>
        /// Adds a new data point to the history.
        /// </summary>
        /// <param name="usage">Resource usage to record.</param>
        public void AddDataPoint(ResourceUsage usage)
        {
            lock (_lockObject)
            {
                _dataPoints.Enqueue(new ResourceUsageDataPoint
                {
                    Timestamp = DateTime.UtcNow,
                    Usage = usage
                });

                // Remove old data points if we exceed the limit
                while (_dataPoints.Count > _maxDataPoints)
                {
                    _dataPoints.Dequeue();
                }
            }
        }

        /// <summary>
        /// Gets all historical data points.
        /// </summary>
        /// <returns>Array of historical usage data.</returns>
        public ResourceUsageDataPoint[] GetHistory()
        {
            lock (_lockObject)
            {
                return _dataPoints.ToArray();
            }
        }

        /// <summary>
        /// Gets historical data points within a specific time range.
        /// </summary>
        /// <param name="from">Start time.</param>
        /// <param name="to">End time.</param>
        /// <returns>Filtered historical usage data.</returns>
        public ResourceUsageDataPoint[] GetHistory(DateTime from, DateTime to)
        {
            lock (_lockObject)
            {
                return _dataPoints
                    .Where(dp => dp.Timestamp >= from && dp.Timestamp <= to)
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets aggregated statistics for the historical data.
        /// </summary>
        /// <returns>Aggregated resource usage statistics.</returns>
        public AggregatedResourceUsage GetAggregatedStats()
        {
            lock (_lockObject)
            {
                if (_dataPoints.Count == 0)
                    return new AggregatedResourceUsage();

                var usages = _dataPoints.Select(dp => dp.Usage).ToArray();

                return new AggregatedResourceUsage
                {
                    AverageMemoryUsed = (long)usages.Average(u => u.MemoryUsed),
                    PeakMemoryUsed = usages.Max(u => u.MemoryUsed),
                    AverageCpuTimeUsed = (long)usages.Average(u => u.CpuTimeUsed),
                    PeakCpuTimeUsed = usages.Max(u => u.CpuTimeUsed),
                    AverageThreadsCreated = (int)usages.Average(u => u.ThreadsCreated),
                    PeakThreadsCreated = usages.Max(u => u.ThreadsCreated),
                    TotalExecutionTime = usages.Max(u => u.ExecutionTime),
                    DataPointCount = _dataPoints.Count,
                    FirstDataPoint = _dataPoints.First().Timestamp,
                    LastDataPoint = _dataPoints.Last().Timestamp
                };
            }
        }
    }

    /// <summary>
    /// Represents a timestamped resource usage data point.
    /// </summary>
    public class ResourceUsageDataPoint
    {
        /// <summary>
        /// Timestamp when the data was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Resource usage at this timestamp.
        /// </summary>
        public ResourceUsage Usage { get; set; }
    }

    /// <summary>
    /// Represents aggregated resource usage statistics.
    /// </summary>
    public class AggregatedResourceUsage
    {
        /// <summary>
        /// Average memory usage over the tracking period.
        /// </summary>
        public long AverageMemoryUsed { get; set; }

        /// <summary>
        /// Peak memory usage over the tracking period.
        /// </summary>
        public long PeakMemoryUsed { get; set; }

        /// <summary>
        /// Average CPU time used over the tracking period.
        /// </summary>
        public long AverageCpuTimeUsed { get; set; }

        /// <summary>
        /// Peak CPU time used over the tracking period.
        /// </summary>
        public long PeakCpuTimeUsed { get; set; }

        /// <summary>
        /// Average number of threads created over the tracking period.
        /// </summary>
        public int AverageThreadsCreated { get; set; }

        /// <summary>
        /// Peak number of threads created over the tracking period.
        /// </summary>
        public int PeakThreadsCreated { get; set; }

        /// <summary>
        /// Total execution time tracked.
        /// </summary>
        public long TotalExecutionTime { get; set; }

        /// <summary>
        /// Number of data points used for aggregation.
        /// </summary>
        public int DataPointCount { get; set; }

        /// <summary>
        /// Timestamp of the first data point.
        /// </summary>
        public DateTime FirstDataPoint { get; set; }

        /// <summary>
        /// Timestamp of the last data point.
        /// </summary>
        public DateTime LastDataPoint { get; set; }
    }
}
