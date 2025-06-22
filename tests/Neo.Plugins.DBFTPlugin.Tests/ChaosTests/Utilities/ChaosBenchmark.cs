// Copyright (C) 2015-2025 The Neo Project.
//
// ChaosBenchmark.cs file belongs to the neo project and is free
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
using System.Text;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Utilities
{
    public class ChaosBenchmark
    {
        private readonly ConcurrentDictionary<string, BenchmarkResult> benchmarks = new ConcurrentDictionary<string, BenchmarkResult>();
        private readonly Stopwatch totalStopwatch = new Stopwatch();

        public void Start()
        {
            totalStopwatch.Restart();
        }

        public IDisposable Measure(string operation)
        {
            return new BenchmarkScope(this, operation);
        }

        public void RecordThroughput(string metric, long count, TimeSpan duration)
        {
            var throughput = count / duration.TotalSeconds;
            var result = benchmarks.GetOrAdd(metric, _ => new BenchmarkResult { Name = metric });
            result.RecordThroughput(throughput);
        }

        public void RecordLatency(string operation, TimeSpan latency)
        {
            var result = benchmarks.GetOrAdd(operation, _ => new BenchmarkResult { Name = operation });
            result.RecordLatency(latency.TotalMilliseconds);
        }

        public void RecordMemoryUsage(string context, long bytes)
        {
            var result = benchmarks.GetOrAdd($"Memory_{context}", _ => new BenchmarkResult { Name = $"Memory_{context}" });
            result.RecordMemoryUsage(bytes);
        }

        public void GenerateBenchmarkReport()
        {
            var report = new StringBuilder();
            var totalDuration = totalStopwatch.Elapsed;

            report.AppendLine("=== CHAOS TEST BENCHMARK REPORT ===");
            report.AppendLine($"Total Duration: {totalDuration.TotalSeconds:F2} seconds");
            report.AppendLine();

            // Performance Metrics
            report.AppendLine("## Performance Metrics");
            foreach (var benchmark in benchmarks.Values.OrderBy(b => b.Name))
            {
                report.AppendLine($"\n### {benchmark.Name}");

                if (benchmark.LatencyMeasurements.Any())
                {
                    var latencies = benchmark.LatencyMeasurements.ToList();
                    latencies.Sort();

                    report.AppendLine($"  Count: {latencies.Count}");
                    report.AppendLine($"  Min: {latencies.Min():F2}ms");
                    report.AppendLine($"  Max: {latencies.Max():F2}ms");
                    report.AppendLine($"  Avg: {latencies.Average():F2}ms");
                    report.AppendLine($"  P50: {GetPercentile(latencies, 0.50):F2}ms");
                    report.AppendLine($"  P95: {GetPercentile(latencies, 0.95):F2}ms");
                    report.AppendLine($"  P99: {GetPercentile(latencies, 0.99):F2}ms");
                }

                if (benchmark.ThroughputMeasurements.Any())
                {
                    var throughputs = benchmark.ThroughputMeasurements.ToList();
                    report.AppendLine($"  Throughput: {throughputs.Average():F2} ops/sec");
                    report.AppendLine($"  Peak: {throughputs.Max():F2} ops/sec");
                }

                if (benchmark.MemoryMeasurements.Any())
                {
                    var memory = benchmark.MemoryMeasurements.ToList();
                    report.AppendLine($"  Memory Usage: {FormatBytes(memory.Average())} avg");
                    report.AppendLine($"  Peak Memory: {FormatBytes(memory.Max())}");
                }
            }

            // Resource Utilization
            report.AppendLine("\n## Resource Utilization");
            var process = Process.GetCurrentProcess();
            report.AppendLine($"  CPU Time: {process.TotalProcessorTime.TotalSeconds:F2} seconds");
            report.AppendLine($"  Working Set: {FormatBytes(process.WorkingSet64)}");
            report.AppendLine($"  Peak Working Set: {FormatBytes(process.PeakWorkingSet64)}");
            report.AppendLine($"  Thread Count: {process.Threads.Count}");

            // Chaos Test Efficiency
            report.AppendLine("\n## Chaos Test Efficiency");
            var totalOperations = benchmarks.Values.Sum(b => b.LatencyMeasurements.Count);
            report.AppendLine($"  Total Operations: {totalOperations}");
            report.AppendLine($"  Operations/Second: {totalOperations / totalDuration.TotalSeconds:F2}");

            report.AppendLine("\n=== END OF BENCHMARK REPORT ===");

            Console.WriteLine(report.ToString());
        }

        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0;
            int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
        }

        private string FormatBytes(double bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:F2} {sizes[order]}";
        }

        private class BenchmarkScope : IDisposable
        {
            private readonly ChaosBenchmark benchmark;
            private readonly string operation;
            private readonly Stopwatch stopwatch;

            public BenchmarkScope(ChaosBenchmark benchmark, string operation)
            {
                this.benchmark = benchmark;
                this.operation = operation;
                stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                stopwatch.Stop();
                benchmark.RecordLatency(operation, stopwatch.Elapsed);
            }
        }

        private class BenchmarkResult
        {
            public string Name { get; set; }
            public ConcurrentBag<double> LatencyMeasurements { get; } = new ConcurrentBag<double>();
            public ConcurrentBag<double> ThroughputMeasurements { get; } = new ConcurrentBag<double>();
            public ConcurrentBag<long> MemoryMeasurements { get; } = new ConcurrentBag<long>();

            public void RecordLatency(double milliseconds)
            {
                LatencyMeasurements.Add(milliseconds);
            }

            public void RecordThroughput(double opsPerSecond)
            {
                ThroughputMeasurements.Add(opsPerSecond);
            }

            public void RecordMemoryUsage(long bytes)
            {
                MemoryMeasurements.Add(bytes);
            }
        }
    }
}
