// Copyright (C) 2015-2025 The Neo Project.
//
// CoverageTracker.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Fuzzer.Utils
{
    /// <summary>
    /// Tracks code coverage during fuzzing to identify interesting scripts
    /// </summary>
    public class CoverageTracker
    {
        private readonly HashSet<string> _globalCoverage = new HashSet<string>();
        private readonly Dictionary<string, int> _coverageFrequency = new Dictionary<string, int>();

        /// <summary>
        /// Gets the total number of unique coverage points seen
        /// </summary>
        public int TotalCoverage => _globalCoverage.Count;

        /// <summary>
        /// Gets the number of unique coverage points seen
        /// </summary>
        public int CoverageCount => _globalCoverage.Count;

        /// <summary>
        /// Checks if the provided coverage contains any new coverage points
        /// </summary>
        /// <param name="coverage">The coverage from a script execution</param>
        /// <returns>True if new coverage was found, false otherwise</returns>
        public bool HasNewCoverage(HashSet<string> coverage)
        {
            if (coverage == null || coverage.Count == 0)
            {
                return false;
            }

            bool hasNewCoverage = false;

            // Check for new coverage points
            foreach (var point in coverage)
            {
                if (_globalCoverage.Add(point))
                {
                    hasNewCoverage = true;
                }

                // Update frequency
                if (!_coverageFrequency.TryGetValue(point, out int count))
                {
                    _coverageFrequency[point] = 1;
                }
                else
                {
                    _coverageFrequency[point] = count + 1;
                }
            }

            return hasNewCoverage;
        }

        /// <summary>
        /// Adds a single coverage point and checks if it's new
        /// </summary>
        /// <param name="point">The coverage point to add</param>
        /// <returns>True if this is a new coverage point, false otherwise</returns>
        public bool AddCoveragePoint(string point)
        {
            if (string.IsNullOrEmpty(point))
            {
                return false;
            }

            bool isNew = _globalCoverage.Add(point);

            // Update frequency
            if (!_coverageFrequency.TryGetValue(point, out int count))
            {
                _coverageFrequency[point] = 1;
            }
            else
            {
                _coverageFrequency[point] = count + 1;
            }

            return isNew;
        }

        /// <summary>
        /// Gets a report of the current coverage statistics
        /// </summary>
        /// <returns>A string containing coverage statistics</returns>
        public string GetCoverageReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine($"Total Coverage Points: {TotalCoverage}");

            // Report OpCode coverage
            var opcodeCoverage = _globalCoverage
                .Where(c => c.StartsWith("OpCode:"))
                .Select(c => c.Substring(7))
                .ToList();

            report.AppendLine($"OpCode Coverage: {opcodeCoverage.Count} unique opcodes");

            // Top 10 most frequent coverage points
            var topFrequent = _coverageFrequency
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToList();

            report.AppendLine("\nTop 10 Most Frequent Coverage Points:");
            foreach (var kv in topFrequent)
            {
                report.AppendLine($"  {kv.Key}: {kv.Value} times");
            }

            // Top 10 least frequent coverage points
            var leastFrequent = _coverageFrequency
                .OrderBy(kv => kv.Value)
                .Take(10)
                .ToList();

            report.AppendLine("\nTop 10 Least Frequent Coverage Points:");
            foreach (var kv in leastFrequent)
            {
                report.AppendLine($"  {kv.Key}: {kv.Value} times");
            }

            return report.ToString();
        }

        /// <summary>
        /// Saves the coverage report to a file
        /// </summary>
        /// <param name="filePath">The path to save the report to</param>
        public void SaveCoverageReport(string filePath)
        {
            string report = GetCoverageReport();
            System.IO.File.WriteAllText(filePath, report);

            // Also save raw coverage data for further analysis
            string rawDataPath = System.IO.Path.ChangeExtension(filePath, ".csv");
            using var writer = new System.IO.StreamWriter(rawDataPath);

            writer.WriteLine("CoveragePoint,Frequency");
            foreach (var kv in _coverageFrequency.OrderByDescending(kv => kv.Value))
            {
                writer.WriteLine($"{kv.Key},{kv.Value}");
            }
        }
    }
}
