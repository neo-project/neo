// Copyright (C) 2015-2025 The Neo Project.
//
// TelemetryHealthCheck.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Health check system for telemetry to ensure metrics are being collected properly
    /// </summary>
    public class TelemetryHealthCheck : IDisposable
    {
        private readonly Timer _healthCheckTimer;
        private readonly Dictionary<string, DateTime> _lastMetricUpdate = new();
        private readonly Dictionary<string, long> _metricErrorCount = new();
        private readonly TimeSpan _staleThreshold = TimeSpan.FromMinutes(5);
        private readonly object _lock = new object();
        private bool _disposed;

        public enum HealthStatus
        {
            Healthy,
            Degraded,
            Unhealthy
        }

        public class HealthReport
        {
            public HealthStatus Status { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, string> Details { get; set; } = new();
            public List<string> Issues { get; set; } = new();
        }

        public TelemetryHealthCheck(TimeSpan checkInterval)
        {
            _healthCheckTimer = new Timer(
                PerformHealthCheck,
                null,
                TimeSpan.Zero,
                checkInterval);
        }

        public void RecordMetricUpdate(string metricName)
        {
            lock (_lock)
            {
                _lastMetricUpdate[metricName] = DateTime.UtcNow;
            }
        }

        public void RecordMetricError(string metricName, Exception ex)
        {
            lock (_lock)
            {
                if (!_metricErrorCount.ContainsKey(metricName))
                    _metricErrorCount[metricName] = 0;

                _metricErrorCount[metricName]++;

                // Log error if it's happening frequently
                if (_metricErrorCount[metricName] % 100 == 0)
                {
                    ConsoleHelper.Warning($"Metric {metricName} has failed {_metricErrorCount[metricName]} times: {ex.Message}");
                }
            }
        }

        public HealthReport GetHealthReport()
        {
            var report = new HealthReport
            {
                Timestamp = DateTime.UtcNow,
                Status = HealthStatus.Healthy
            };

            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // Check for stale metrics
                foreach (var kvp in _lastMetricUpdate)
                {
                    var staleness = now - kvp.Value;
                    if (staleness > _staleThreshold)
                    {
                        report.Issues.Add($"Metric {kvp.Key} is stale (last updated {staleness.TotalMinutes:F1} minutes ago)");
                        report.Status = HealthStatus.Degraded;
                    }
                }

                // Check for high error rates
                foreach (var kvp in _metricErrorCount)
                {
                    if (kvp.Value > 1000)
                    {
                        report.Issues.Add($"Metric {kvp.Key} has high error count: {kvp.Value}");
                        report.Status = HealthStatus.Unhealthy;
                    }
                    else if (kvp.Value > 100)
                    {
                        report.Issues.Add($"Metric {kvp.Key} has elevated error count: {kvp.Value}");
                        if (report.Status == HealthStatus.Healthy)
                            report.Status = HealthStatus.Degraded;
                    }
                }

                // Add system resource checks
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / (1024 * 1024);
                var threadCount = process.Threads.Count;

                report.Details["memory_mb"] = memoryMB.ToString();
                report.Details["thread_count"] = threadCount.ToString();
                report.Details["total_metrics"] = _lastMetricUpdate.Count.ToString();
                report.Details["error_metrics"] = _metricErrorCount.Count.ToString();

                // Check resource thresholds
                if (memoryMB > 4096) // More than 4GB
                {
                    report.Issues.Add($"High memory usage: {memoryMB}MB");
                    if (report.Status == HealthStatus.Healthy)
                        report.Status = HealthStatus.Degraded;
                }

                if (threadCount > 1000)
                {
                    report.Issues.Add($"High thread count: {threadCount}");
                    if (report.Status == HealthStatus.Healthy)
                        report.Status = HealthStatus.Degraded;
                }
            }

            return report;
        }

        private void PerformHealthCheck(object? state)
        {
            try
            {
                var report = GetHealthReport();

                if (report.Status != HealthStatus.Healthy)
                {
                    ConsoleHelper.Warning($"Telemetry health check: {report.Status}");
                    foreach (var issue in report.Issues)
                    {
                        ConsoleHelper.Warning($"  - {issue}");
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error($"Health check failed: {ex.Message}");
            }
        }

        public void ResetErrorCounts()
        {
            lock (_lock)
            {
                _metricErrorCount.Clear();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _healthCheckTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}
