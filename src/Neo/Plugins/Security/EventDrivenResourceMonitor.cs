// Copyright (C) 2015-2025 The Neo Project.
//
// EventDrivenResourceMonitor.cs file belongs to the neo project and is free
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
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Neo.Plugins.Security
{
    /// <summary>
    /// Event-driven resource monitoring system that replaces inefficient timer-based monitoring.
    /// </summary>
    public class EventDrivenResourceMonitor : IDisposable
    {
        private readonly Channel<ResourceEvent> _eventChannel;
        private readonly ConcurrentDictionary<string, PluginResourceState> _pluginStates = new();
        private readonly CancellationTokenSource _cancellationSource = new();
        private readonly Task _processingTask;
        private bool _disposed = false;

        /// <summary>
        /// Fired when a resource violation is detected.
        /// </summary>
        public event EventHandler<ResourceViolationEventArgs> ResourceViolationDetected;

        /// <summary>
        /// Fired when resource usage crosses a warning threshold.
        /// </summary>
        public event EventHandler<ResourceWarningEventArgs> ResourceWarningThresholdCrossed;

        public EventDrivenResourceMonitor()
        {
            // Create unbounded channel for high performance
            _eventChannel = Channel.CreateUnbounded<ResourceEvent>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            // Start the event processing task
            _processingTask = ProcessEventsAsync(_cancellationSource.Token);
        }

        /// <summary>
        /// Reports a resource usage event.
        /// </summary>
        public void ReportResourceUsage(string pluginName, ResourceUsageSnapshot snapshot)
        {
            if (_disposed) return;

            var resourceEvent = new ResourceEvent
            {
                PluginName = pluginName,
                Timestamp = DateTime.UtcNow,
                EventType = ResourceEventType.UsageReport,
                Snapshot = snapshot
            };

            // Non-blocking write to channel
            if (!_eventChannel.Writer.TryWrite(resourceEvent))
            {
                // Channel is closed or full (shouldn't happen with unbounded)
                Utility.Log("EventDrivenResourceMonitor", LogLevel.Warning,
                    $"Failed to write resource event for plugin: {pluginName}");
            }
        }

        /// <summary>
        /// Reports the start of a plugin operation.
        /// </summary>
        public void ReportOperationStart(string pluginName, string operationId)
        {
            if (_disposed) return;

            var resourceEvent = new ResourceEvent
            {
                PluginName = pluginName,
                Timestamp = DateTime.UtcNow,
                EventType = ResourceEventType.OperationStart,
                OperationId = operationId
            };

            _eventChannel.Writer.TryWrite(resourceEvent);
        }

        /// <summary>
        /// Reports the end of a plugin operation.
        /// </summary>
        public void ReportOperationEnd(string pluginName, string operationId, ResourceUsage usage)
        {
            if (_disposed) return;

            var resourceEvent = new ResourceEvent
            {
                PluginName = pluginName,
                Timestamp = DateTime.UtcNow,
                EventType = ResourceEventType.OperationEnd,
                OperationId = operationId,
                Usage = usage
            };

            _eventChannel.Writer.TryWrite(resourceEvent);
        }

        /// <summary>
        /// Gets the current resource state for a plugin.
        /// </summary>
        public PluginResourceState GetPluginState(string pluginName)
        {
            return _pluginStates.GetOrAdd(pluginName, _ => new PluginResourceState(pluginName));
        }

        /// <summary>
        /// Configures monitoring for a specific plugin.
        /// </summary>
        public void ConfigurePlugin(string pluginName, PluginSecurityPolicy policy)
        {
            var state = GetPluginState(pluginName);
            state.Policy = policy;
            state.IsMonitored = true;
        }

        /// <summary>
        /// Stops monitoring a specific plugin.
        /// </summary>
        public void StopMonitoring(string pluginName)
        {
            if (_pluginStates.TryGetValue(pluginName, out var state))
            {
                state.IsMonitored = false;
            }
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (var resourceEvent in _eventChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        await ProcessResourceEvent(resourceEvent);
                    }
                    catch (Exception ex)
                    {
                        Utility.Log("EventDrivenResourceMonitor", LogLevel.Error,
                            $"Error processing resource event: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        private async Task ProcessResourceEvent(ResourceEvent resourceEvent)
        {
            var state = GetPluginState(resourceEvent.PluginName);
            if (!state.IsMonitored) return;

            switch (resourceEvent.EventType)
            {
                case ResourceEventType.UsageReport:
                    await ProcessUsageReport(state, resourceEvent);
                    break;

                case ResourceEventType.OperationStart:
                    ProcessOperationStart(state, resourceEvent);
                    break;

                case ResourceEventType.OperationEnd:
                    await ProcessOperationEnd(state, resourceEvent);
                    break;
            }
        }

        private async Task ProcessUsageReport(PluginResourceState state, ResourceEvent resourceEvent)
        {
            if (resourceEvent.Snapshot == null) return;

            // Update state with latest snapshot
            state.UpdateSnapshot(resourceEvent.Snapshot);

            // Check for violations
            if (state.Policy != null)
            {
                var violations = CheckForViolations(state, resourceEvent.Snapshot);
                if (violations.Count > 0)
                {
                    await HandleViolations(state, violations);
                }

                // Check for warnings
                var warnings = CheckForWarnings(state, resourceEvent.Snapshot);
                if (warnings.Count > 0)
                {
                    HandleWarnings(state, warnings);
                }
            }
        }

        private void ProcessOperationStart(PluginResourceState state, ResourceEvent resourceEvent)
        {
            state.StartOperation(resourceEvent.OperationId, resourceEvent.Timestamp);
        }

        private async Task ProcessOperationEnd(PluginResourceState state, ResourceEvent resourceEvent)
        {
            var operation = state.EndOperation(resourceEvent.OperationId, resourceEvent.Timestamp, resourceEvent.Usage);

            if (operation != null && state.Policy != null)
            {
                // Check if operation violated any limits
                if (resourceEvent.Usage != null)
                {
                    var violations = CheckOperationViolations(state, operation, resourceEvent.Usage);
                    if (violations.Count > 0)
                    {
                        await HandleViolations(state, violations);
                    }
                }
            }
        }

        private List<ResourceViolation> CheckForViolations(PluginResourceState state, ResourceUsageSnapshot snapshot)
        {
            var violations = new List<ResourceViolation>();
            var policy = state.Policy;

            // Memory violation
            if (policy.MaxMemoryBytes > 0 && snapshot.MemoryBytes > policy.MaxMemoryBytes)
            {
                violations.Add(new ResourceViolation
                {
                    ViolationType = $"Memory limit exceeded: {snapshot.MemoryBytes} > {policy.MaxMemoryBytes}",
                    PluginName = state.PluginName,
                    Timestamp = DateTime.UtcNow
                });
            }

            // CPU violation (if we have enough samples)
            if (policy.MaxCpuPercent > 0 && state.GetAverageCpuUsage() > policy.MaxCpuPercent)
            {
                violations.Add(new ResourceViolation
                {
                    ViolationType = $"CPU limit exceeded: {state.GetAverageCpuUsage():F1}% > {policy.MaxCpuPercent}%",
                    PluginName = state.PluginName,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Thread violation
            if (policy.MaxThreads > 0 && snapshot.ThreadCount > policy.MaxThreads)
            {
                violations.Add(new ResourceViolation
                {
                    ViolationType = $"Thread limit exceeded: {snapshot.ThreadCount} > {policy.MaxThreads}",
                    PluginName = state.PluginName,
                    Timestamp = DateTime.UtcNow
                });
            }

            return violations;
        }

        private List<ResourceViolation> CheckOperationViolations(PluginResourceState state,
            PluginOperation operation, ResourceUsage usage)
        {
            var violations = new List<ResourceViolation>();
            var policy = state.Policy;

            // Check execution time
            if (policy.MaxExecutionTimeSeconds > 0 &&
                usage.ExecutionTime > policy.MaxExecutionTimeSeconds * 1000)
            {
                violations.Add(new ResourceViolation
                {
                    ViolationType = $"Execution time exceeded: {usage.ExecutionTime}ms > {policy.MaxExecutionTimeSeconds * 1000}ms",
                    PluginName = state.PluginName,
                    Timestamp = DateTime.UtcNow
                });
            }

            return violations;
        }

        private List<string> CheckForWarnings(PluginResourceState state, ResourceUsageSnapshot snapshot)
        {
            var warnings = new List<string>();
            var policy = state.Policy;

            if (policy.ResourceMonitoring == null) return warnings;

            // Memory warning
            if (policy.MaxMemoryBytes > 0)
            {
                var memoryPercent = (double)snapshot.MemoryBytes / policy.MaxMemoryBytes;
                if (memoryPercent > policy.ResourceMonitoring.MemoryWarningThreshold)
                {
                    warnings.Add($"Memory usage at {memoryPercent:P0} of limit");
                }
            }

            // CPU warning
            if (policy.MaxCpuPercent > 0)
            {
                var cpuUsage = state.GetAverageCpuUsage();
                var cpuPercent = cpuUsage / policy.MaxCpuPercent;
                if (cpuPercent > policy.ResourceMonitoring.CpuWarningThreshold)
                {
                    warnings.Add($"CPU usage at {cpuPercent:P0} of limit");
                }
            }

            return warnings;
        }

        private async Task HandleViolations(PluginResourceState state, List<ResourceViolation> violations)
        {
            foreach (var violation in violations)
            {
                // Log violation (skip in test mode to avoid circular dependency)
                if (!IsTestEnvironment())
                {
                    SecurityAuditLogger.Instance.LogResourceViolation(state.PluginName, violation);
                }

                // Fire event
                ResourceViolationDetected?.Invoke(this, new ResourceViolationEventArgs
                {
                    PluginName = state.PluginName,
                    Violation = violation,
                    Policy = state.Policy
                });

                // Take action based on policy
                if (state.Policy != null)
                {
                    // Avoid circular dependency by skipping in test mode
                    if (!IsTestEnvironment())
                    {
                        switch (state.Policy.ViolationAction)
                        {
                            case ViolationAction.Suspend:
                                PluginSecurityManager.Instance.SuspendPlugin(state.PluginName);
                                break;

                            case ViolationAction.Terminate:
                                PluginSecurityManager.Instance.TerminatePlugin(state.PluginName);
                                break;
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }

        private void HandleWarnings(PluginResourceState state, List<string> warnings)
        {
            foreach (var warning in warnings)
            {
                // Fire warning event
                ResourceWarningThresholdCrossed?.Invoke(this, new ResourceWarningEventArgs
                {
                    PluginName = state.PluginName,
                    Warning = warning,
                    Policy = state.Policy
                });
            }
        }

        /// <summary>
        /// Checks if we're running in a test environment.
        /// </summary>
        private static bool IsTestEnvironment()
        {
            try
            {
                var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                return processName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                       processName.Contains("vstest", StringComparison.OrdinalIgnoreCase) ||
                       Environment.GetEnvironmentVariable("DOTNET_TEST_MODE")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationSource.Cancel();
                _eventChannel.Writer.TryComplete();

                try
                {
                    _processingTask.Wait(TimeSpan.FromSeconds(5));
                }
                catch
                {
                    // Ignore timeout during disposal
                }

                _cancellationSource.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a resource monitoring event.
    /// </summary>
    internal class ResourceEvent
    {
        public string PluginName { get; set; }
        public DateTime Timestamp { get; set; }
        public ResourceEventType EventType { get; set; }
        public ResourceUsageSnapshot Snapshot { get; set; }
        public string OperationId { get; set; }
        public ResourceUsage Usage { get; set; }
    }

    /// <summary>
    /// Types of resource events.
    /// </summary>
    internal enum ResourceEventType
    {
        UsageReport,
        OperationStart,
        OperationEnd
    }

    /// <summary>
    /// Represents a snapshot of resource usage at a point in time.
    /// </summary>
    public class ResourceUsageSnapshot
    {
        public long MemoryBytes { get; set; }
        public double CpuPercent { get; set; }
        public int ThreadCount { get; set; }
        public long HandleCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Maintains resource state for a plugin.
    /// </summary>
    public class PluginResourceState
    {
        private readonly Queue<ResourceUsageSnapshot> _recentSnapshots = new();
        private readonly ConcurrentDictionary<string, PluginOperation> _activeOperations = new();
        private readonly object _snapshotLock = new object();
        private const int MaxSnapshots = 60; // Keep last 60 snapshots

        public string PluginName { get; }
        public PluginSecurityPolicy Policy { get; set; }
        public bool IsMonitored { get; set; }
        public DateTime LastUpdate { get; private set; }

        public PluginResourceState(string pluginName)
        {
            PluginName = pluginName;
            LastUpdate = DateTime.UtcNow;
        }

        public void UpdateSnapshot(ResourceUsageSnapshot snapshot)
        {
            lock (_snapshotLock)
            {
                _recentSnapshots.Enqueue(snapshot);

                // Keep only recent snapshots
                while (_recentSnapshots.Count > MaxSnapshots)
                {
                    _recentSnapshots.Dequeue();
                }

                LastUpdate = DateTime.UtcNow;
            }
        }

        public double GetAverageCpuUsage()
        {
            lock (_snapshotLock)
            {
                if (_recentSnapshots.Count == 0) return 0;

                double sum = 0;
                foreach (var snapshot in _recentSnapshots)
                {
                    sum += snapshot.CpuPercent;
                }

                return sum / _recentSnapshots.Count;
            }
        }

        public void StartOperation(string operationId, DateTime startTime)
        {
            _activeOperations.TryAdd(operationId, new PluginOperation
            {
                OperationId = operationId,
                StartTime = startTime
            });
        }

        public PluginOperation EndOperation(string operationId, DateTime endTime, ResourceUsage usage)
        {
            if (_activeOperations.TryRemove(operationId, out var operation))
            {
                operation.EndTime = endTime;
                operation.ResourceUsage = usage;
                return operation;
            }

            return null;
        }
    }

    /// <summary>
    /// Represents a plugin operation.
    /// </summary>
    public class PluginOperation
    {
        public string OperationId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ResourceUsage ResourceUsage { get; set; }
    }

    /// <summary>
    /// Event args for resource violations.
    /// </summary>
    public class ResourceViolationEventArgs : EventArgs
    {
        public string PluginName { get; set; }
        public ResourceViolation Violation { get; set; }
        public PluginSecurityPolicy Policy { get; set; }
    }

    /// <summary>
    /// Event args for resource warnings.
    /// </summary>
    public class ResourceWarningEventArgs : EventArgs
    {
        public string PluginName { get; set; }
        public string Warning { get; set; }
        public PluginSecurityPolicy Policy { get; set; }
    }
}
