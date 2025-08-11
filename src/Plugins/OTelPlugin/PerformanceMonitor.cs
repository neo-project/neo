// Copyright (C) 2015-2025 The Neo Project.
//
// PerformanceMonitor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.ConsoleService;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Neo.Plugins.OpenTelemetry
{
    /// <summary>
    /// Performance monitoring with adaptive sampling to minimize overhead
    /// </summary>
    public class PerformanceMonitor : IDisposable
    {
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new();
        private readonly Timer _reportTimer;
        private readonly object _lock = new object();
        private bool _disposed;

        // Adaptive sampling rates based on load
        private double _samplingRate = 1.0; // Start with 100% sampling
        private const double MinSamplingRate = 0.01; // Minimum 1% sampling
        private const double MaxSamplingRate = 1.0; // Maximum 100% sampling

        public class PerformanceMetric
        {
            public long Count { get; set; }
            public double TotalTime { get; set; }
            public double MinTime { get; set; } = double.MaxValue;
            public double MaxTime { get; set; }
            public double LastTime { get; set; }
            public DateTime LastUpdate { get; set; }

            // Percentiles (approximated using reservoir sampling)
            private readonly double[] _reservoir = new double[1000];
            private int _reservoirIndex = 0;
            private readonly Random _random = new Random();

            public void RecordTime(double milliseconds)
            {
                Count++;
                TotalTime += milliseconds;
                MinTime = Math.Min(MinTime, milliseconds);
                MaxTime = Math.Max(MaxTime, milliseconds);
                LastTime = milliseconds;
                LastUpdate = DateTime.UtcNow;

                // Reservoir sampling for percentiles
                if (_reservoirIndex < _reservoir.Length)
                {
                    _reservoir[_reservoirIndex++] = milliseconds;
                }
                else
                {
                    int j = _random.Next(Count > int.MaxValue ? int.MaxValue : (int)Count);
                    if (j < _reservoir.Length)
                    {
                        _reservoir[j] = milliseconds;
                    }
                }
            }

            public double GetPercentile(double percentile)
            {
                if (_reservoirIndex == 0) return 0;

                var sorted = new double[Math.Min(_reservoirIndex, _reservoir.Length)];
                Array.Copy(_reservoir, sorted, sorted.Length);
                Array.Sort(sorted);

                int index = (int)(sorted.Length * percentile / 100.0);
                return sorted[Math.Min(index, sorted.Length - 1)];
            }

            public double Average => Count > 0 ? TotalTime / Count : 0;
        }

        public PerformanceMonitor(TimeSpan reportInterval)
        {
            _reportTimer = new Timer(
                ReportMetrics,
                null,
                reportInterval,
                reportInterval);
        }

        public IDisposable StartTimer(string operationName)
        {
            // Apply sampling
            if (!ShouldSample())
            {
                return new NoOpTimer();
            }

            return new OperationTimer(this, operationName);
        }

        private bool ShouldSample()
        {
            return _samplingRate >= 1.0 || new Random().NextDouble() < _samplingRate;
        }

        public void RecordOperation(string operationName, double milliseconds)
        {
            var metric = _metrics.GetOrAdd(operationName, _ => new PerformanceMetric());
            metric.RecordTime(milliseconds);

            // Adjust sampling rate based on load
            AdjustSamplingRate(metric);
        }

        private void AdjustSamplingRate(PerformanceMetric metric)
        {
            // If we're getting too many samples per second, reduce sampling
            if (metric.Count > 10000) // More than 10k operations
            {
                lock (_lock)
                {
                    _samplingRate = Math.Max(MinSamplingRate, _samplingRate * 0.9);
                }
            }
            // If load is light, increase sampling
            else if (metric.Count < 100)
            {
                lock (_lock)
                {
                    _samplingRate = Math.Min(MaxSamplingRate, _samplingRate * 1.1);
                }
            }
        }

        private void ReportMetrics(object? state)
        {
            try
            {
                foreach (var kvp in _metrics)
                {
                    var metric = kvp.Value;
                    if (metric.Count == 0) continue;

                    // Only report if there's significant activity
                    if ((DateTime.UtcNow - metric.LastUpdate).TotalMinutes > 5)
                        continue;

                    ConsoleHelper.Info($"Performance: {kvp.Key}");
                    ConsoleHelper.Info($"  Count: {metric.Count:N0} (sampling: {_samplingRate:P0})");
                    ConsoleHelper.Info($"  Avg: {metric.Average:F2}ms");
                    ConsoleHelper.Info($"  Min: {metric.MinTime:F2}ms");
                    ConsoleHelper.Info($"  Max: {metric.MaxTime:F2}ms");
                    ConsoleHelper.Info($"  P50: {metric.GetPercentile(50):F2}ms");
                    ConsoleHelper.Info($"  P95: {metric.GetPercentile(95):F2}ms");
                    ConsoleHelper.Info($"  P99: {metric.GetPercentile(99):F2}ms");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.Error($"Failed to report performance metrics: {ex.Message}");
            }
        }

        public PerformanceMetric? GetMetric(string operationName)
        {
            return _metrics.TryGetValue(operationName, out var metric) ? metric : null;
        }

        public void Reset()
        {
            _metrics.Clear();
            lock (_lock)
            {
                _samplingRate = MaxSamplingRate;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _reportTimer?.Dispose();
                _disposed = true;
            }
        }

        private class OperationTimer : IDisposable
        {
            private readonly PerformanceMonitor _monitor;
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;

            public OperationTimer(PerformanceMonitor monitor, string operationName)
            {
                _monitor = monitor;
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                _monitor.RecordOperation(_operationName, _stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        private class NoOpTimer : IDisposable
        {
            public void Dispose() { }
        }
    }
}
