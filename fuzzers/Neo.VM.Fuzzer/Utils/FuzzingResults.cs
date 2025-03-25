// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzingResults.cs file belongs to the neo project and is free
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Neo.VM.Fuzzer.Utils
{
    /// <summary>
    /// Tracks and analyzes the results of fuzzing runs
    /// </summary>
    public class FuzzingResults
    {
        private readonly string _outputDirectory;
        private readonly ConcurrentDictionary<string, int> _exceptionCounts = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<OpCode, int> _opcodeFrequency = new ConcurrentDictionary<OpCode, int>();
        private readonly List<double> _executionTimes = new List<double>();
        private readonly object _lockObject = new object();

        private int _totalExecutions;
        private int _crashCount;
        private int _timeoutCount;
        private int _newCoverageCount;
        private int _dosVectorCount;
        private double _totalExecutionTime;
        private double _maxExecutionTime;
        private double _minExecutionTime = double.MaxValue;
        private readonly ConcurrentDictionary<string, int> _dosReasonCounts = new ConcurrentDictionary<string, int>();
        private readonly List<double> _dosScores = new List<double>();

        /// <summary>
        /// Gets the total number of script executions
        /// </summary>
        public int TotalExecutions => _totalExecutions;

        /// <summary>
        /// Gets the number of crashes detected
        /// </summary>
        public int CrashCount => _crashCount;

        /// <summary>
        /// Gets the number of timeouts detected
        /// </summary>
        public int TimeoutCount => _timeoutCount;

        /// <summary>
        /// Gets the number of scripts that found new coverage
        /// </summary>
        public int NewCoverageCount => _newCoverageCount;

        /// <summary>
        /// Gets the number of potential DOS vectors detected
        /// </summary>
        public int DOSVectorCount => _dosVectorCount;

        /// <summary>
        /// Gets the average execution time in milliseconds
        /// </summary>
        public double AverageExecutionTimeMs => _totalExecutions > 0 ? _totalExecutionTime / _totalExecutions : 0;

        /// <summary>
        /// Gets the maximum execution time in milliseconds
        /// </summary>
        public double MaxExecutionTimeMs => _maxExecutionTime;

        /// <summary>
        /// Gets the minimum execution time in milliseconds
        /// </summary>
        public double MinExecutionTimeMs => _minExecutionTime == double.MaxValue ? 0 : _minExecutionTime;

        /// <summary>
        /// Gets the average DOS score
        /// </summary>
        public double AverageDOSScore => _dosScores.Count > 0 ? _dosScores.Average() : 0;

        /// <summary>
        /// Initializes a new instance of the FuzzingResults class
        /// </summary>
        /// <param name="outputDirectory">Directory to save results to</param>
        public FuzzingResults(string outputDirectory)
        {
            _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));

            // Ensure output directory exists
            Directory.CreateDirectory(_outputDirectory);
        }

        /// <summary>
        /// Records the result of a fuzzing run
        /// </summary>
        /// <param name="executionTimeMs">Execution time in milliseconds</param>
        /// <param name="crashed">Whether the execution crashed</param>
        /// <param name="timedOut">Whether the execution timed out</param>
        /// <param name="foundNewCoverage">Whether the execution found new coverage</param>
        /// <param name="exceptionType">Type of exception if crashed</param>
        /// <param name="executedOpcodes">List of opcodes executed during the run</param>
        /// <param name="dosAnalysis">DOS analysis result if available</param>
        public void RecordResult(
            double executionTimeMs,
            bool crashed,
            bool timedOut,
            bool foundNewCoverage,
            string? exceptionType = null,
            IEnumerable<OpCode>? executedOpcodes = null,
            DOSDetector.DOSAnalysisResult? dosAnalysis = null)
        {
            lock (_lockObject)
            {
                _totalExecutions++;
                _totalExecutionTime += executionTimeMs;

                if (executionTimeMs > _maxExecutionTime)
                {
                    _maxExecutionTime = executionTimeMs;
                }

                if (executionTimeMs < _minExecutionTime)
                {
                    _minExecutionTime = executionTimeMs;
                }

                _executionTimes.Add(executionTimeMs);

                if (crashed)
                {
                    _crashCount++;

                    // Record exception type
                    if (!string.IsNullOrEmpty(exceptionType))
                    {
                        _exceptionCounts.AddOrUpdate(
                            exceptionType,
                            1,
                            (_, count) => count + 1);
                    }
                }

                if (timedOut)
                {
                    _timeoutCount++;
                }

                if (foundNewCoverage)
                {
                    _newCoverageCount++;
                }

                // Record DOS vector information
                if (dosAnalysis != null && dosAnalysis.IsPotentialDOSVector)
                {
                    _dosVectorCount++;
                    _dosScores.Add(dosAnalysis.DOSScore);

                    // Record DOS reason
                    if (!string.IsNullOrEmpty(dosAnalysis.DetectionReason))
                    {
                        _dosReasonCounts.AddOrUpdate(
                            dosAnalysis.DetectionReason,
                            1,
                            (_, count) => count + 1);
                    }
                }

                // Record opcode frequencies
                if (executedOpcodes != null)
                {
                    foreach (var opcode in executedOpcodes)
                    {
                        _opcodeFrequency.AddOrUpdate(
                            opcode,
                            1,
                            (_, count) => count + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the fuzzing results to a file
        /// </summary>
        /// <param name="filename">Name of the file to save to</param>
        public void SaveResults(string filename)
        {
            string filePath = Path.Combine(_outputDirectory, filename);

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("=== Neo VM Fuzzer Results ===");
                writer.WriteLine();

                writer.WriteLine("General Statistics:");
                writer.WriteLine($"Total Executions: {_totalExecutions}");
                writer.WriteLine($"Crashes: {_crashCount} ({PercentOf(_crashCount, _totalExecutions):F2}%)");
                writer.WriteLine($"Timeouts: {_timeoutCount} ({PercentOf(_timeoutCount, _totalExecutions):F2}%)");
                writer.WriteLine($"New Coverage: {_newCoverageCount} ({PercentOf(_newCoverageCount, _totalExecutions):F2}%)");
                writer.WriteLine($"DOS Vectors: {_dosVectorCount} ({PercentOf(_dosVectorCount, _totalExecutions):F2}%)");
                writer.WriteLine();

                writer.WriteLine("Execution Time Statistics:");
                writer.WriteLine($"Average Execution Time: {AverageExecutionTimeMs:F2} ms");
                writer.WriteLine($"Maximum Execution Time: {MaxExecutionTimeMs:F2} ms");
                writer.WriteLine($"Minimum Execution Time: {MinExecutionTimeMs:F2} ms");

                if (_executionTimes.Count > 0)
                {
                    var sortedTimes = _executionTimes.OrderBy(t => t).ToList();
                    double median = sortedTimes.Count % 2 == 0
                        ? (sortedTimes[sortedTimes.Count / 2 - 1] + sortedTimes[sortedTimes.Count / 2]) / 2
                        : sortedTimes[sortedTimes.Count / 2];

                    writer.WriteLine($"Median Execution Time: {median:F2} ms");

                    // Calculate percentiles
                    int p90Index = (int)Math.Ceiling(sortedTimes.Count * 0.9) - 1;
                    int p95Index = (int)Math.Ceiling(sortedTimes.Count * 0.95) - 1;
                    int p99Index = (int)Math.Ceiling(sortedTimes.Count * 0.99) - 1;

                    writer.WriteLine($"90th Percentile: {sortedTimes[p90Index]:F2} ms");
                    writer.WriteLine($"95th Percentile: {sortedTimes[p95Index]:F2} ms");
                    writer.WriteLine($"99th Percentile: {sortedTimes[p99Index]:F2} ms");
                }

                writer.WriteLine();

                // Exception statistics
                if (_exceptionCounts.Count > 0)
                {
                    writer.WriteLine("Exception Statistics:");
                    foreach (var exception in _exceptionCounts.OrderByDescending(e => e.Value))
                    {
                        writer.WriteLine($"{exception.Key}: {exception.Value} ({PercentOf(exception.Value, _crashCount):F2}%)");
                    }
                    writer.WriteLine();
                }

                // DOS vector statistics
                if (_dosReasonCounts.Count > 0)
                {
                    writer.WriteLine("DOS Vector Statistics:");
                    writer.WriteLine($"Total DOS Vectors: {_dosVectorCount}");
                    writer.WriteLine($"Average DOS Score: {AverageDOSScore:F2}");

                    if (_dosScores.Count > 0)
                    {
                        var sortedScores = _dosScores.OrderBy(s => s).ToList();
                        double median = sortedScores.Count % 2 == 0
                            ? (sortedScores[sortedScores.Count / 2 - 1] + sortedScores[sortedScores.Count / 2]) / 2
                            : sortedScores[sortedScores.Count / 2];

                        writer.WriteLine($"Median DOS Score: {median:F2}");
                        writer.WriteLine($"Maximum DOS Score: {sortedScores.Max():F2}");
                        writer.WriteLine($"Minimum DOS Score: {sortedScores.Min():F2}");
                    }

                    writer.WriteLine();
                    writer.WriteLine("DOS Reasons:");
                    foreach (var kvp in _dosReasonCounts.OrderByDescending(x => x.Value))
                    {
                        writer.WriteLine($"{kvp.Key}: {kvp.Value} ({PercentOf(kvp.Value, _dosVectorCount):F2}%)");
                    }
                }

                // Opcode frequency
                if (_opcodeFrequency.Count > 0)
                {
                    writer.WriteLine("Opcode Frequency (Top 20):");
                    foreach (var opcode in _opcodeFrequency.OrderByDescending(o => o.Value).Take(20))
                    {
                        writer.WriteLine($"{opcode.Key}: {opcode.Value} ({PercentOf(opcode.Value, _totalExecutions):F2}%)");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("=== End of Report ===");
            }
        }

        /// <summary>
        /// Calculates the percentage of a value relative to a total
        /// </summary>
        private static double PercentOf(int value, int total)
        {
            return total > 0 ? (double)value / total * 100 : 0;
        }

        /// <summary>
        /// Generates a histogram of execution times
        /// </summary>
        /// <param name="bucketCount">Number of buckets in the histogram</param>
        /// <returns>A dictionary mapping time ranges to counts</returns>
        public Dictionary<string, int> GenerateExecutionTimeHistogram(int bucketCount = 10)
        {
            var histogram = new Dictionary<string, int>();

            if (_executionTimes.Count == 0)
            {
                return histogram;
            }

            double min = _executionTimes.Min();
            double max = _executionTimes.Max();
            double bucketSize = (max - min) / bucketCount;

            // Initialize buckets
            for (int i = 0; i < bucketCount; i++)
            {
                double bucketStart = min + i * bucketSize;
                double bucketEnd = bucketStart + bucketSize;
                string bucketLabel = $"{bucketStart:F2}-{bucketEnd:F2}";
                histogram[bucketLabel] = 0;
            }

            // Fill buckets
            foreach (var time in _executionTimes)
            {
                int bucketIndex = Math.Min(bucketCount - 1, (int)((time - min) / bucketSize));
                double bucketStart = min + bucketIndex * bucketSize;
                double bucketEnd = bucketStart + bucketSize;
                string bucketLabel = $"{bucketStart:F2}-{bucketEnd:F2}";
                histogram[bucketLabel]++;
            }

            return histogram;
        }

        /// <summary>
        /// Saves a detailed histogram of execution times to a file
        /// </summary>
        /// <param name="filename">Name of the file to save to</param>
        /// <param name="bucketCount">Number of buckets in the histogram</param>
        public void SaveExecutionTimeHistogram(string filename, int bucketCount = 20)
        {
            string filePath = Path.Combine(_outputDirectory, filename);
            var histogram = GenerateExecutionTimeHistogram(bucketCount);

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("=== Execution Time Histogram ===");
                writer.WriteLine("Time Range (ms),Count,Percentage");

                foreach (var bucket in histogram.OrderBy(b => double.Parse(b.Key.Split('-')[0])))
                {
                    double percentage = PercentOf(bucket.Value, _totalExecutions);
                    writer.WriteLine($"{bucket.Key},{bucket.Value},{percentage:F2}%");
                }
            }
        }
    }
}
