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
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.Json.Fuzzer.Utils
{
    /// <summary>
    /// Tracks code coverage during fuzzing
    /// </summary>
    public class CoverageTracker
    {
        private readonly HashSet<string> _coveredPoints = new();
        private readonly Dictionary<string, int> _hitCounts = new();
        private readonly string _outputDir;
        private readonly bool _verbose;
        private int _totalRuns = 0;
        private int _newCoverageRuns = 0;

        /// <summary>
        /// Gets the number of unique coverage points found
        /// </summary>
        public int UniquePoints => _coveredPoints.Count;

        /// <summary>
        /// Gets the total number of runs
        /// </summary>
        public int TotalRuns => _totalRuns;

        /// <summary>
        /// Gets the number of runs that found new coverage
        /// </summary>
        public int NewCoverageRuns => _newCoverageRuns;

        /// <summary>
        /// Gets the coverage percentage (if total points is known)
        /// </summary>
        public double CoveragePercentage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the CoverageTracker class
        /// </summary>
        /// <param name="outputDir">Directory for storing coverage information</param>
        /// <param name="verbose">Whether to output verbose information</param>
        /// <param name="estimatedTotalPoints">Estimated total number of coverage points</param>
        public CoverageTracker(string outputDir, bool verbose = false, int estimatedTotalPoints = 100)
        {
            _outputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
            _verbose = verbose;
            
            // Initialize coverage percentage
            CoveragePercentage = 0.0;
            
            if (estimatedTotalPoints > 0)
            {
                // Create coverage directory
                Directory.CreateDirectory(Path.Combine(_outputDir, "coverage"));
            }
        }

        /// <summary>
        /// Records coverage points from a run
        /// </summary>
        /// <param name="coveragePoints">List of coverage points from the run</param>
        /// <returns>True if new coverage was found, false otherwise</returns>
        public bool RecordCoverage(List<string> coveragePoints)
        {
            bool foundNewCoverage = false;
            _totalRuns++;

            // Record each coverage point
            foreach (string point in coveragePoints)
            {
                // Update hit count
                if (_hitCounts.TryGetValue(point, out int count))
                {
                    _hitCounts[point] = count + 1;
                }
                else
                {
                    _hitCounts[point] = 1;
                }

                // Check if this is a new coverage point
                if (_coveredPoints.Add(point))
                {
                    foundNewCoverage = true;
                    
                    if (_verbose)
                    {
                        Console.WriteLine($"New coverage point: {point}");
                    }
                }
            }

            // Update statistics
            if (foundNewCoverage)
            {
                _newCoverageRuns++;
            }

            return foundNewCoverage;
        }

        /// <summary>
        /// Updates the coverage percentage based on the estimated total points
        /// </summary>
        /// <param name="estimatedTotalPoints">Estimated total number of coverage points</param>
        public void UpdateCoveragePercentage(int estimatedTotalPoints)
        {
            if (estimatedTotalPoints > 0)
            {
                CoveragePercentage = Math.Min(100.0, (double)_coveredPoints.Count / estimatedTotalPoints * 100.0);
            }
        }

        /// <summary>
        /// Saves coverage information to a file
        /// </summary>
        public void SaveCoverageReport()
        {
            try
            {
                string coverageDir = Path.Combine(_outputDir, "coverage");
                Directory.CreateDirectory(coverageDir);
                
                string reportPath = Path.Combine(coverageDir, $"coverage_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                
                using (StreamWriter writer = new(reportPath))
                {
                    writer.WriteLine("Neo.Json.Fuzzer Coverage Report");
                    writer.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    writer.WriteLine($"Total Runs: {_totalRuns}");
                    writer.WriteLine($"Runs with New Coverage: {_newCoverageRuns}");
                    writer.WriteLine($"Unique Coverage Points: {_coveredPoints.Count}");
                    writer.WriteLine($"Coverage Percentage: {CoveragePercentage:F2}%");
                    writer.WriteLine();
                    
                    writer.WriteLine("Coverage Points by Category:");
                    
                    // Group coverage points by category
                    var categories = _coveredPoints
                        .GroupBy(p => p.Split(':')[0])
                        .OrderBy(g => g.Key);
                    
                    foreach (var category in categories)
                    {
                        writer.WriteLine($"  {category.Key}: {category.Count()} points");
                        
                        // List all points in this category with hit counts
                        foreach (var point in category.OrderBy(p => p))
                        {
                            int hitCount = _hitCounts.TryGetValue(point, out int count) ? count : 0;
                            writer.WriteLine($"    {point} - {hitCount} hits");
                        }
                        
                        writer.WriteLine();
                    }
                    
                    writer.WriteLine("Top 20 Most Hit Coverage Points:");
                    foreach (var point in _hitCounts.OrderByDescending(kv => kv.Value).Take(20))
                    {
                        writer.WriteLine($"  {point.Key}: {point.Value} hits");
                    }
                    
                    writer.WriteLine();
                    writer.WriteLine("Least Hit Coverage Points (1 hit):");
                    foreach (var point in _hitCounts.Where(kv => kv.Value == 1).OrderBy(kv => kv.Key).Take(20))
                    {
                        writer.WriteLine($"  {point.Key}: {point.Value} hit");
                    }
                }
                
                // Save coverage points to a CSV file for further analysis
                string csvPath = Path.Combine(coverageDir, $"coverage_data_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                using (StreamWriter writer = new(csvPath))
                {
                    writer.WriteLine("CoveragePoint,HitCount");
                    foreach (var point in _hitCounts.OrderBy(kv => kv.Key))
                    {
                        writer.WriteLine($"\"{point.Key}\",{point.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving coverage report: {ex.Message}");
            }
        }

        /// <summary>
        /// Prints a summary of the coverage information
        /// </summary>
        public void PrintSummary()
        {
            Console.WriteLine();
            Console.WriteLine("Coverage Summary:");
            Console.WriteLine($"  Total Runs: {_totalRuns}");
            Console.WriteLine($"  Unique Coverage Points: {_coveredPoints.Count}");
            Console.WriteLine($"  Coverage Percentage: {CoveragePercentage:F2}%");
            Console.WriteLine($"  Runs with New Coverage: {_newCoverageRuns} ({(double)_newCoverageRuns / _totalRuns * 100:F2}%)");
            
            // Print coverage by category
            var categories = _coveredPoints
                .GroupBy(p => p.Split(':')[0])
                .OrderBy(g => g.Key);
            
            Console.WriteLine();
            Console.WriteLine("Coverage by Category:");
            foreach (var category in categories)
            {
                Console.WriteLine($"  {category.Key}: {category.Count()} points");
            }
        }
    }
}
