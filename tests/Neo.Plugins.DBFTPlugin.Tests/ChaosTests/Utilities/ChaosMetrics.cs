// Copyright (C) 2015-2025 The Neo Project.
//
// ChaosMetrics.cs file belongs to the neo project and is free
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
using System.Linq;
using System.Text;

namespace Neo.Plugins.DBFTPlugin.Tests.ChaosTests.Utilities
{
    public class ChaosMetrics
    {
        private readonly ConcurrentBag<int> latencies = new ConcurrentBag<int>();
        private readonly ConcurrentDictionary<string, long> counters = new ConcurrentDictionary<string, long>();
        private readonly ConcurrentDictionary<string, ConcurrentBag<DateTime>> eventTimestamps = new ConcurrentDictionary<string, ConcurrentBag<DateTime>>();
        private readonly DateTime startTime = DateTime.UtcNow;

        public void RecordMessageLoss()
        {
            IncrementCounter("MessageLoss");
            RecordEvent("MessageLoss");
        }

        public void RecordMessageCorruption()
        {
            IncrementCounter("MessageCorruption");
            RecordEvent("MessageCorruption");
        }

        public void RecordMessageDuplication()
        {
            IncrementCounter("MessageDuplication");
            RecordEvent("MessageDuplication");
        }

        public void RecordMessageReordering()
        {
            IncrementCounter("MessageReordering");
            RecordEvent("MessageReordering");
        }

        public void RecordNodeFailure()
        {
            IncrementCounter("NodeFailure");
            RecordEvent("NodeFailure");
        }

        public void RecordNodeRecovery()
        {
            IncrementCounter("NodeRecovery");
            RecordEvent("NodeRecovery");
        }

        public void RecordNetworkPartition()
        {
            IncrementCounter("NetworkPartition");
            RecordEvent("NetworkPartition");
        }

        public void RecordByzantineBehavior()
        {
            IncrementCounter("ByzantineBehavior");
            RecordEvent("ByzantineBehavior");
        }

        public void RecordConsensusSuccess()
        {
            IncrementCounter("ConsensusSuccess");
            RecordEvent("ConsensusSuccess");
        }

        public void RecordConsensusFailure()
        {
            IncrementCounter("ConsensusFailure");
            RecordEvent("ConsensusFailure");
        }

        public void RecordViewChange()
        {
            IncrementCounter("ViewChange");
            RecordEvent("ViewChange");
        }

        public void RecordLatency(int latencyMs)
        {
            latencies.Add(latencyMs);
        }

        public void RecordClockSkew(TimeSpan skew)
        {
            IncrementCounter("ClockSkew");
            counters.AddOrUpdate("MaxClockSkewMs", (long)Math.Abs(skew.TotalMilliseconds),
                (key, old) => Math.Max(old, (long)Math.Abs(skew.TotalMilliseconds)));
        }

        private void IncrementCounter(string name)
        {
            counters.AddOrUpdate(name, 1, (key, old) => old + 1);
        }

        private void RecordEvent(string eventType)
        {
            var timestamps = eventTimestamps.GetOrAdd(eventType, _ => new ConcurrentBag<DateTime>());
            timestamps.Add(DateTime.UtcNow);
        }

        public void GenerateReport()
        {
            var report = new StringBuilder();
            var duration = DateTime.UtcNow - startTime;

            report.AppendLine("=== CHAOS TEST METRICS REPORT ===");
            report.AppendLine($"Test Duration: {duration.TotalSeconds:F2} seconds");
            report.AppendLine();

            report.AppendLine("## Fault Injection Summary");
            foreach (var counter in counters.OrderBy(c => c.Key))
            {
                report.AppendLine($"  {counter.Key}: {counter.Value}");
            }
            report.AppendLine();

            if (latencies.Any())
            {
                var latencyList = latencies.ToList();
                latencyList.Sort();

                report.AppendLine("## Network Latency Statistics");
                report.AppendLine($"  Min: {latencyList.Min()}ms");
                report.AppendLine($"  Max: {latencyList.Max()}ms");
                report.AppendLine($"  Average: {latencyList.Average():F2}ms");
                report.AppendLine($"  Median: {GetMedian(latencyList):F2}ms");
                report.AppendLine($"  P95: {GetPercentile(latencyList, 0.95):F2}ms");
                report.AppendLine($"  P99: {GetPercentile(latencyList, 0.99):F2}ms");
                report.AppendLine();
            }

            var consensusSuccess = counters.GetValueOrDefault("ConsensusSuccess", 0);
            var consensusFailure = counters.GetValueOrDefault("ConsensusFailure", 0);
            var totalConsensus = consensusSuccess + consensusFailure;

            if (totalConsensus > 0)
            {
                report.AppendLine("## Consensus Performance");
                report.AppendLine($"  Total Rounds: {totalConsensus}");
                report.AppendLine($"  Successful: {consensusSuccess} ({(double)consensusSuccess / totalConsensus:P2})");
                report.AppendLine($"  Failed: {consensusFailure} ({(double)consensusFailure / totalConsensus:P2})");
                report.AppendLine($"  View Changes: {counters.GetValueOrDefault("ViewChange", 0)}");
                report.AppendLine();
            }

            report.AppendLine("## Event Timeline Analysis");
            foreach (var eventType in eventTimestamps.Keys.OrderBy(k => k))
            {
                var timestamps = eventTimestamps[eventType].OrderBy(t => t).ToList();
                if (timestamps.Any())
                {
                    var intervals = GetEventIntervals(timestamps);
                    report.AppendLine($"  {eventType}:");
                    report.AppendLine($"    Count: {timestamps.Count}");
                    report.AppendLine($"    Rate: {timestamps.Count / duration.TotalSeconds:F2} per second");

                    if (intervals.Any())
                    {
                        report.AppendLine($"    Avg Interval: {intervals.Average():F2}ms");
                        report.AppendLine($"    Min Interval: {intervals.Min():F2}ms");
                        report.AppendLine($"    Max Interval: {intervals.Max():F2}ms");
                    }
                }
            }

            report.AppendLine();
            report.AppendLine("=== END OF REPORT ===");

            Console.WriteLine(report.ToString());
        }

        private double GetMedian(List<int> sortedValues)
        {
            int count = sortedValues.Count;
            if (count == 0) return 0;
            if (count % 2 == 0)
            {
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2.0;
            }
            return sortedValues[count / 2];
        }

        private double GetPercentile(List<int> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0;
            int index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
        }

        private List<double> GetEventIntervals(List<DateTime> timestamps)
        {
            var intervals = new List<double>();
            for (int i = 1; i < timestamps.Count; i++)
            {
                intervals.Add((timestamps[i] - timestamps[i - 1]).TotalMilliseconds);
            }
            return intervals;
        }

        public ChaosTestSummary GetSummary()
        {
            return new ChaosTestSummary
            {
                Duration = DateTime.UtcNow - startTime,
                MessageLossCount = counters.GetValueOrDefault("MessageLoss", 0),
                NodeFailureCount = counters.GetValueOrDefault("NodeFailure", 0),
                NetworkPartitionCount = counters.GetValueOrDefault("NetworkPartition", 0),
                ConsensusSuccessCount = counters.GetValueOrDefault("ConsensusSuccess", 0),
                ConsensusFailureCount = counters.GetValueOrDefault("ConsensusFailure", 0),
                ViewChangeCount = counters.GetValueOrDefault("ViewChange", 0),
                AverageLatencyMs = latencies.Any() ? latencies.Average() : 0
            };
        }
    }

    public class ChaosTestSummary
    {
        public TimeSpan Duration { get; set; }
        public long MessageLossCount { get; set; }
        public long NodeFailureCount { get; set; }
        public long NetworkPartitionCount { get; set; }
        public long ConsensusSuccessCount { get; set; }
        public long ConsensusFailureCount { get; set; }
        public long ViewChangeCount { get; set; }
        public double AverageLatencyMs { get; set; }

        public double ConsensusSuccessRate =>
            ConsensusSuccessCount + ConsensusFailureCount > 0
                ? (double)ConsensusSuccessCount / (ConsensusSuccessCount + ConsensusFailureCount)
                : 0;
    }
}
