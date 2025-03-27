// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzingStatistics.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json.Fuzzer.Runners;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Neo.Json.Fuzzer.Utils
{
    /// <summary>
    /// Tracks and reports statistics during fuzzing
    /// </summary>
    public class FuzzingStatistics
    {
        private readonly Stopwatch _totalStopwatch = new();
        private readonly string _outputDir;
        private readonly bool _verbose;
        private readonly Dictionary<string, int> _exceptionCounts = new();
        private readonly List<double> _executionTimes = new();
        private readonly List<long> _memoryUsages = new();
        private readonly List<DOSAnalysisResult> _dosVectors = new();
        private readonly object _lock = new();
        private int _totalRuns;
        private int _successfulRuns;
        private int _crashedRuns;
        private int _timedOutRuns;
        private int _dosVectorCount;
        private double _lastReportTime;
        private long _lastReportRuns;

        /// <summary>
        /// Gets the total number of runs
        /// </summary>
        public int TotalRuns => _totalRuns;

        /// <summary>
        /// Gets the number of successful runs
        /// </summary>
        public int SuccessfulRuns => _successfulRuns;

        /// <summary>
        /// Gets the number of crashed runs
        /// </summary>
        public int CrashedRuns => _crashedRuns;

        /// <summary>
        /// Gets the number of timed out runs
        /// </summary>
        public int TimedOutRuns => _timedOutRuns;

        /// <summary>
        /// Gets the number of potential DOS vectors found
        /// </summary>
        public int DOSVectorCount => _dosVectorCount;

        /// <summary>
        /// Gets the total elapsed time
        /// </summary>
        public TimeSpan TotalElapsedTime => _totalStopwatch.Elapsed;

        /// <summary>
        /// Gets the average runs per second
        /// </summary>
        public double RunsPerSecond => _totalRuns / Math.Max(0.001, _totalStopwatch.Elapsed.TotalSeconds);

        /// <summary>
        /// Initializes a new instance of the FuzzingStatistics class
        /// </summary>
        /// <param name="outputDir">Directory for storing statistics</param>
        /// <param name="verbose">Whether to output verbose information</param>
        public FuzzingStatistics(string outputDir, bool verbose = false)
        {
            _outputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
            _verbose = verbose;
            _totalStopwatch.Start();
            
            // Create statistics directory
            Directory.CreateDirectory(Path.Combine(_outputDir, "stats"));
        }

        /// <summary>
        /// Records the result of a fuzzing run
        /// </summary>
        /// <param name="result">The execution result</param>
        public void RecordResult(JsonExecutionResult result)
        {
            lock (_lock)
            {
                _totalRuns++;

                if (result.Success)
                {
                    _successfulRuns++;
                }

                if (result.Crashed)
                {
                    _crashedRuns++;
                    
                    // Record exception type
                    if (!string.IsNullOrEmpty(result.ExceptionType))
                    {
                        if (_exceptionCounts.TryGetValue(result.ExceptionType, out int count))
                        {
                            _exceptionCounts[result.ExceptionType] = count + 1;
                        }
                        else
                        {
                            _exceptionCounts[result.ExceptionType] = 1;
                        }
                    }
                }

                if (result.TimedOut)
                {
                    _timedOutRuns++;
                }

                // Record execution time and memory usage
                _executionTimes.Add(result.ExecutionTimeMs);
                _memoryUsages.Add(result.MemoryUsageBytes);

                // Record DOS vector
                if (result.DOSAnalysis != null && result.DOSAnalysis.IsPotentialDOSVector)
                {
                    _dosVectorCount++;
                    _dosVectors.Add(result.DOSAnalysis);
                }

                // Check if it's time to print a progress report
                double currentTime = _totalStopwatch.Elapsed.TotalSeconds;
                if (_verbose && (currentTime - _lastReportTime >= 5.0 || _totalRuns % 1000 == 0))
                {
                    PrintProgressReport();
                    _lastReportTime = currentTime;
                    _lastReportRuns = _totalRuns;
                }
            }
        }

        /// <summary>
        /// Prints a progress report
        /// </summary>
        public void PrintProgressReport()
        {
            double elapsedSeconds = _totalStopwatch.Elapsed.TotalSeconds;
            double currentRunsPerSecond = (_totalRuns - _lastReportRuns) / Math.Max(0.001, elapsedSeconds - _lastReportTime);
            double overallRunsPerSecond = _totalRuns / Math.Max(0.001, elapsedSeconds);
            
            Console.WriteLine();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Fuzzing Progress:");
            Console.WriteLine($"  Total Runs: {_totalRuns:N0}");
            Console.WriteLine($"  Successful: {_successfulRuns:N0} ({(double)_successfulRuns / _totalRuns * 100:F2}%)");
            Console.WriteLine($"  Crashed: {_crashedRuns:N0} ({(double)_crashedRuns / _totalRuns * 100:F2}%)");
            Console.WriteLine($"  Timed Out: {_timedOutRuns:N0} ({(double)_timedOutRuns / _totalRuns * 100:F2}%)");
            Console.WriteLine($"  DOS Vectors: {_dosVectorCount:N0}");
            Console.WriteLine($"  Elapsed Time: {_totalStopwatch.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"  Current Speed: {currentRunsPerSecond:F2} runs/sec");
            Console.WriteLine($"  Overall Speed: {overallRunsPerSecond:F2} runs/sec");
            
            if (_executionTimes.Count > 0)
            {
                Console.WriteLine($"  Avg Execution Time: {_executionTimes.Average():F2} ms");
            }
            
            if (_exceptionCounts.Count > 0)
            {
                Console.WriteLine("  Top Exceptions:");
                foreach (var ex in _exceptionCounts.OrderByDescending(kv => kv.Value).Take(3))
                {
                    Console.WriteLine($"    {ex.Key}: {ex.Value:N0}");
                }
            }
        }

        /// <summary>
        /// Saves statistics to a file
        /// </summary>
        public void SaveStatistics()
        {
            try
            {
                string statsDir = Path.Combine(_outputDir, "stats");
                Directory.CreateDirectory(statsDir);
                
                string reportPath = Path.Combine(statsDir, $"fuzzing_stats_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                
                using (StreamWriter writer = new(reportPath))
                {
                    writer.WriteLine("Neo.Json.Fuzzer Statistics Report");
                    writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Total Runs: {_totalRuns:N0}");
                    writer.WriteLine($"Successful Runs: {_successfulRuns:N0} ({(double)_successfulRuns / _totalRuns * 100:F2}%)");
                    writer.WriteLine($"Crashed Runs: {_crashedRuns:N0} ({(double)_crashedRuns / _totalRuns * 100:F2}%)");
                    writer.WriteLine($"Timed Out Runs: {_timedOutRuns:N0} ({(double)_timedOutRuns / _totalRuns * 100:F2}%)");
                    writer.WriteLine($"DOS Vectors Found: {_dosVectorCount:N0}");
                    writer.WriteLine($"Total Elapsed Time: {_totalStopwatch.Elapsed:hh\\:mm\\:ss}");
                    writer.WriteLine($"Runs Per Second: {RunsPerSecond:F2}");
                    writer.WriteLine();
                    
                    if (_executionTimes.Count > 0)
                    {
                        writer.WriteLine("Execution Time Statistics:");
                        writer.WriteLine($"  Average: {_executionTimes.Average():F2} ms");
                        writer.WriteLine($"  Minimum: {_executionTimes.Min():F2} ms");
                        writer.WriteLine($"  Maximum: {_executionTimes.Max():F2} ms");
                        writer.WriteLine($"  Median: {GetMedian(_executionTimes):F2} ms");
                        writer.WriteLine($"  95th Percentile: {GetPercentile(_executionTimes, 95):F2} ms");
                        writer.WriteLine();
                    }
                    
                    if (_memoryUsages.Count > 0)
                    {
                        writer.WriteLine("Memory Usage Statistics:");
                        writer.WriteLine($"  Average: {_memoryUsages.Average() / 1024:F2} KB");
                        writer.WriteLine($"  Minimum: {_memoryUsages.Min() / 1024:F2} KB");
                        writer.WriteLine($"  Maximum: {_memoryUsages.Max() / 1024:F2} KB");
                        writer.WriteLine($"  Median: {GetMedian(_memoryUsages) / 1024:F2} KB");
                        writer.WriteLine($"  95th Percentile: {GetPercentile(_memoryUsages, 95) / 1024:F2} KB");
                        writer.WriteLine();
                    }
                    
                    if (_exceptionCounts.Count > 0)
                    {
                        writer.WriteLine("Exception Statistics:");
                        foreach (var ex in _exceptionCounts.OrderByDescending(kv => kv.Value))
                        {
                            writer.WriteLine($"  {ex.Key}: {ex.Value:N0} ({(double)ex.Value / _crashedRuns * 100:F2}%)");
                        }
                        writer.WriteLine();
                    }
                    
                    if (_dosVectors.Count > 0)
                    {
                        writer.WriteLine("DOS Vector Statistics:");
                        writer.WriteLine($"  Total DOS Vectors: {_dosVectors.Count:N0}");
                        writer.WriteLine($"  Average DOS Score: {_dosVectors.Average(d => d.DOSScore):F4}");
                        writer.WriteLine($"  Maximum DOS Score: {_dosVectors.Max(d => d.DOSScore):F4}");
                        
                        // Group by detection reason
                        var reasonGroups = _dosVectors
                            .GroupBy(d => d.DetectionReason)
                            .OrderByDescending(g => g.Count());
                        
                        writer.WriteLine("  Detection Reasons:");
                        foreach (var group in reasonGroups.Take(10))
                        {
                            writer.WriteLine($"    {group.Key}: {group.Count():N0} ({(double)group.Count() / _dosVectors.Count * 100:F2}%)");
                        }
                        writer.WriteLine();
                    }
                }
                
                // Save raw data for further analysis
                SaveRawData(statsDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves raw data for further analysis
        /// </summary>
        private void SaveRawData(string statsDir)
        {
            // Save execution times
            string executionTimesPath = Path.Combine(statsDir, $"execution_times_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            using (StreamWriter writer = new(executionTimesPath))
            {
                writer.WriteLine("ExecutionTimeMs");
                foreach (double time in _executionTimes)
                {
                    writer.WriteLine(time.ToString("F2"));
                }
            }
            
            // Save memory usages
            string memoryUsagesPath = Path.Combine(statsDir, $"memory_usages_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            using (StreamWriter writer = new(memoryUsagesPath))
            {
                writer.WriteLine("MemoryUsageBytes");
                foreach (long memory in _memoryUsages)
                {
                    writer.WriteLine(memory);
                }
            }
            
            // Save DOS vector data
            if (_dosVectors.Count > 0)
            {
                string dosVectorsPath = Path.Combine(statsDir, $"dos_vectors_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                using (StreamWriter writer = new(dosVectorsPath))
                {
                    writer.WriteLine("DOSScore,DetectionReason,ExecutionTimeMs,MemoryUsageBytes,InputLength,NestingDepth,TimePerCharRatio,MemoryPerCharRatio");
                    foreach (var dos in _dosVectors)
                    {
                        StringBuilder sb = new();
                        sb.Append(dos.DOSScore.ToString("F4"));
                        sb.Append(",\"");
                        sb.Append(dos.DetectionReason?.Replace("\"", "\"\"") ?? "");
                        sb.Append("\"");
                        
                        // Add metrics
                        foreach (string metricName in new[] { "ExecutionTimeMs", "MemoryUsageBytes", "InputLength", "NestingDepth", "TimePerCharRatio", "MemoryPerCharRatio" })
                        {
                            sb.Append(",");
                            if (dos.Metrics.TryGetValue(metricName, out double value))
                            {
                                sb.Append(value.ToString("F4"));
                            }
                        }
                        
                        writer.WriteLine(sb.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Gets the median value of a list
        /// </summary>
        private static double GetMedian<T>(List<T> values) where T : IComparable<T>
        {
            if (values == null || values.Count == 0)
                return 0;

            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;
            
            if (count % 2 == 0)
            {
                // Even count, average the two middle values
                dynamic value1 = sortedValues[count / 2 - 1];
                dynamic value2 = sortedValues[count / 2];
                return (value1 + value2) / 2.0;
            }
            else
            {
                // Odd count, return the middle value
                return Convert.ToDouble(sortedValues[count / 2]);
            }
        }

        /// <summary>
        /// Gets a percentile value of a list
        /// </summary>
        private static double GetPercentile<T>(List<T> values, int percentile) where T : IComparable<T>
        {
            if (values == null || values.Count == 0)
                return 0;

            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;
            
            double rank = percentile / 100.0 * (count - 1);
            int lowerIndex = (int)Math.Floor(rank);
            int upperIndex = (int)Math.Ceiling(rank);
            
            if (lowerIndex == upperIndex)
            {
                return Convert.ToDouble(sortedValues[lowerIndex]);
            }
            
            dynamic lowerValue = sortedValues[lowerIndex];
            dynamic upperValue = sortedValues[upperIndex];
            double fraction = rank - lowerIndex;
            
            return lowerValue + fraction * (upperValue - lowerValue);
        }
    }
}
