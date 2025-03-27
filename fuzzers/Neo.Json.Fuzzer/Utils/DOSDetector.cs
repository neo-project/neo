// Copyright (C) 2015-2025 The Neo Project.
//
// DOSDetector.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.Json.Fuzzer.Utils
{
    /// <summary>
    /// Detects potential Denial of Service (DOS) vectors in JSON parsing
    /// </summary>
    public class DOSDetector
    {
        private readonly double _threshold;
        private readonly bool _trackMemory;
        private readonly Dictionary<string, double> _thresholds;

        /// <summary>
        /// Initializes a new instance of the DOSDetector class
        /// </summary>
        /// <param name="threshold">Overall threshold for DOS detection (0.0-1.0)</param>
        /// <param name="trackMemory">Whether to track memory usage</param>
        public DOSDetector(double threshold = 0.8, bool trackMemory = false)
        {
            _threshold = Math.Clamp(threshold, 0.0, 1.0);
            _trackMemory = trackMemory;

            // Define thresholds for different metrics
            _thresholds = new Dictionary<string, double>
            {
                // Time-based thresholds (in milliseconds)
                ["ExecutionTimeMs"] = 10.0, // Flag if parsing takes more than 10ms

                // Memory-based thresholds (in bytes)
                ["MemoryUsageBytes"] = 10 * 1024 * 1024, // Flag if memory usage exceeds 10MB

                // Structure-based thresholds
                ["InputLength"] = 10000, // Flag if input is longer than 10,000 characters
                ["NestingDepth"] = 32, // Flag if nesting depth exceeds 32 levels

                // Ratio-based thresholds
                ["TimePerCharRatio"] = 0.01, // Flag if time per character exceeds 0.01ms
                ["MemoryPerCharRatio"] = 1000 // Flag if memory per character exceeds 1000 bytes
            };
        }

        /// <summary>
        /// Resets the detector for a new analysis
        /// </summary>
        public void Reset()
        {
            // Reset any state if needed
        }

        /// <summary>
        /// Analyzes metrics to determine if a JSON input is a potential DOS vector
        /// </summary>
        /// <param name="metrics">Dictionary of metrics to analyze</param>
        /// <returns>Analysis result with DOS score and detection reason</returns>
        public DOSAnalysisResult Analyze(Dictionary<string, double> metrics)
        {
            var result = new DOSAnalysisResult
            {
                Metrics = new Dictionary<string, double>(metrics)
            };

            // Calculate derived metrics
            if (metrics.TryGetValue("ExecutionTimeMs", out double executionTime) && 
                metrics.TryGetValue("InputLength", out double inputLength) && 
                inputLength > 0)
            {
                metrics["TimePerCharRatio"] = executionTime / inputLength;
            }

            if (_trackMemory && 
                metrics.TryGetValue("MemoryUsageBytes", out double memoryUsage) && 
                metrics.TryGetValue("InputLength", out double inputLen) && 
                inputLen > 0)
            {
                metrics["MemoryPerCharRatio"] = memoryUsage / inputLen;
            }

            // Calculate DOS score components
            double timeScore = CalculateTimeScore(metrics);
            double memoryScore = _trackMemory ? CalculateMemoryScore(metrics) : 0.0;
            double complexityScore = CalculateComplexityScore(metrics);

            // Calculate weighted DOS score
            double dosScore;
            if (_trackMemory)
            {
                dosScore = (timeScore * 0.4) + (memoryScore * 0.3) + (complexityScore * 0.3);
            }
            else
            {
                dosScore = (timeScore * 0.6) + (complexityScore * 0.4);
            }

            result.DOSScore = Math.Clamp(dosScore, 0.0, 1.0);
            result.IsPotentialDOSVector = result.DOSScore >= _threshold;

            // Determine the primary reason for detection
            result.DetectionReason = DeterminePrimaryDetectionReason(metrics, timeScore, memoryScore, complexityScore);

            return result;
        }

        /// <summary>
        /// Calculates the time-based component of the DOS score
        /// </summary>
        private double CalculateTimeScore(Dictionary<string, double> metrics)
        {
            double timeScore = 0.0;

            // Check execution time
            if (metrics.TryGetValue("ExecutionTimeMs", out double executionTime))
            {
                timeScore = Math.Min(1.0, executionTime / _thresholds["ExecutionTimeMs"]);
            }

            // Check time per character ratio
            if (metrics.TryGetValue("TimePerCharRatio", out double timePerChar))
            {
                double ratioScore = Math.Min(1.0, timePerChar / _thresholds["TimePerCharRatio"]);
                timeScore = Math.Max(timeScore, ratioScore);
            }

            return timeScore;
        }

        /// <summary>
        /// Calculates the memory-based component of the DOS score
        /// </summary>
        private double CalculateMemoryScore(Dictionary<string, double> metrics)
        {
            double memoryScore = 0.0;

            // Check memory usage
            if (metrics.TryGetValue("MemoryUsageBytes", out double memoryUsage))
            {
                memoryScore = Math.Min(1.0, memoryUsage / _thresholds["MemoryUsageBytes"]);
            }

            // Check memory per character ratio
            if (metrics.TryGetValue("MemoryPerCharRatio", out double memoryPerChar))
            {
                double ratioScore = Math.Min(1.0, memoryPerChar / _thresholds["MemoryPerCharRatio"]);
                memoryScore = Math.Max(memoryScore, ratioScore);
            }

            return memoryScore;
        }

        /// <summary>
        /// Calculates the complexity-based component of the DOS score
        /// </summary>
        private double CalculateComplexityScore(Dictionary<string, double> metrics)
        {
            double complexityScore = 0.0;

            // Check input length
            if (metrics.TryGetValue("InputLength", out double inputLength))
            {
                double lengthScore = Math.Min(1.0, inputLength / _thresholds["InputLength"]);
                complexityScore = Math.Max(complexityScore, lengthScore);
            }

            // Check nesting depth
            if (metrics.TryGetValue("NestingDepth", out double nestingDepth))
            {
                double depthScore = Math.Min(1.0, nestingDepth / _thresholds["NestingDepth"]);
                complexityScore = Math.Max(complexityScore, depthScore);
            }

            return complexityScore;
        }

        /// <summary>
        /// Determines the primary reason for DOS detection
        /// </summary>
        private string DeterminePrimaryDetectionReason(Dictionary<string, double> metrics, double timeScore, double memoryScore, double complexityScore)
        {
            // Find the highest scoring component
            if (timeScore >= memoryScore && timeScore >= complexityScore && timeScore > 0.5)
            {
                if (metrics.TryGetValue("ExecutionTimeMs", out double executionTime) && 
                    executionTime > _thresholds["ExecutionTimeMs"])
                {
                    return $"High execution time: {executionTime:F2}ms (threshold: {_thresholds["ExecutionTimeMs"]:F2}ms)";
                }
                
                if (metrics.TryGetValue("TimePerCharRatio", out double timePerChar) && 
                    timePerChar > _thresholds["TimePerCharRatio"])
                {
                    return $"High time per character ratio: {timePerChar:F4}ms (threshold: {_thresholds["TimePerCharRatio"]:F4}ms)";
                }
                
                return "Excessive processing time";
            }
            
            if (_trackMemory && memoryScore >= timeScore && memoryScore >= complexityScore && memoryScore > 0.5)
            {
                if (metrics.TryGetValue("MemoryUsageBytes", out double memoryUsage) && 
                    memoryUsage > _thresholds["MemoryUsageBytes"])
                {
                    return $"High memory usage: {memoryUsage / (1024 * 1024):F2}MB (threshold: {_thresholds["MemoryUsageBytes"] / (1024 * 1024):F2}MB)";
                }
                
                if (metrics.TryGetValue("MemoryPerCharRatio", out double memoryPerChar) && 
                    memoryPerChar > _thresholds["MemoryPerCharRatio"])
                {
                    return $"High memory per character ratio: {memoryPerChar:F2} bytes (threshold: {_thresholds["MemoryPerCharRatio"]:F2} bytes)";
                }
                
                return "Excessive memory consumption";
            }
            
            if (complexityScore > 0.5)
            {
                if (metrics.TryGetValue("NestingDepth", out double nestingDepth) && 
                    nestingDepth > _thresholds["NestingDepth"])
                {
                    return $"Deep nesting: {nestingDepth:F0} levels (threshold: {_thresholds["NestingDepth"]:F0} levels)";
                }
                
                if (metrics.TryGetValue("InputLength", out double inputLength) && 
                    inputLength > _thresholds["InputLength"])
                {
                    return $"Large input: {inputLength:F0} characters (threshold: {_thresholds["InputLength"]:F0} characters)";
                }
                
                return "Excessive structural complexity";
            }
            
            return "Multiple factors combined";
        }
    }

    /// <summary>
    /// Result of a DOS analysis
    /// </summary>
    public class DOSAnalysisResult
    {
        /// <summary>
        /// Gets or sets the DOS score (0.0-1.0)
        /// </summary>
        public double DOSScore { get; set; }

        /// <summary>
        /// Gets or sets whether this is a potential DOS vector
        /// </summary>
        public bool IsPotentialDOSVector { get; set; }

        /// <summary>
        /// Gets or sets the primary reason for detection
        /// </summary>
        public string? DetectionReason { get; set; }

        /// <summary>
        /// Gets or sets the metrics used for analysis
        /// </summary>
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
    }
}
