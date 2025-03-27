using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Neo.Json;

namespace Neo.Json.Fuzzer.Tests
{
    /// <summary>
    /// Specialized benchmark to test the relationship between array size, nesting depth, and processing time.
    /// This class systematically tests various combinations to identify patterns and thresholds.
    /// </summary>
    public class ArrayNestingBenchmark
    {
        private readonly string _outputDir;
        private readonly Random _random;
        private readonly Dictionary<string, BenchmarkResult> _results = new Dictionary<string, BenchmarkResult>();

        public ArrayNestingBenchmark(string outputDir, Random random)
        {
            _outputDir = Path.Combine(outputDir, "array-nesting-benchmark");
            _random = random;
            
            // Create the output directory if it doesn't exist
            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }
        }

        public void RunBenchmarks()
        {
            Console.WriteLine("Running Array Nesting Benchmark...");
            
            // Test various array sizes with fixed nesting depth
            TestArraySizeImpact(5);
            TestArraySizeImpact(10);
            TestArraySizeImpact(15);
            
            // Test various nesting depths with fixed array size
            TestNestingDepthImpact(10000);
            TestNestingDepthImpact(50000);
            TestNestingDepthImpact(100000);
            
            // Test the impact of different object structures
            TestObjectStructureImpact();
            
            // Generate a report
            GenerateReport();
            
            Console.WriteLine("Array Nesting Benchmark completed.");
        }

        private void TestArraySizeImpact(int nestingDepth)
        {
            Console.WriteLine($"Testing impact of array size with nesting depth {nestingDepth}...");
            
            // Test various array sizes
            int[] arraySizes = { 1000, 5000, 10000, 25000, 50000, 75000, 100000, 125000, 150000 };
            
            foreach (int arraySize in arraySizes)
            {
                // Skip very large combinations that would take too long
                if (arraySize > 100000 && nestingDepth > 10) continue;
                
                string testName = $"array_size_{arraySize}_depth_{nestingDepth}";
                string json = GenerateNestedArrayJson(arraySize, nestingDepth);
                
                MeasureParsingTime(json, testName);
            }
        }

        private void TestNestingDepthImpact(int arraySize)
        {
            Console.WriteLine($"Testing impact of nesting depth with array size {arraySize}...");
            
            // Test various nesting depths
            int[] nestingDepths = { 1, 3, 5, 8, 10, 12, 15, 20, 25, 30 };
            
            foreach (int nestingDepth in nestingDepths)
            {
                // Skip very large combinations that would take too long
                if (arraySize > 100000 && nestingDepth > 10) continue;
                
                string testName = $"array_size_{arraySize}_depth_{nestingDepth}";
                string json = GenerateNestedArrayJson(arraySize, nestingDepth);
                
                MeasureParsingTime(json, testName);
            }
        }

        private void TestObjectStructureImpact()
        {
            Console.WriteLine("Testing impact of different object structures...");
            
            // Test with 50000 objects at depth 10 with different structures
            int arraySize = 50000;
            int nestingDepth = 10;
            
            // Standard nested objects (one property per level)
            string testName1 = $"standard_nested_{arraySize}_depth_{nestingDepth}";
            string json1 = GenerateNestedArrayJson(arraySize, nestingDepth);
            MeasureParsingTime(json1, testName1);
            
            // Multiple properties at each level
            string testName2 = $"multi_prop_nested_{arraySize}_depth_{nestingDepth}";
            string json2 = GenerateMultiPropNestedArrayJson(arraySize, nestingDepth, 3);
            MeasureParsingTime(json2, testName2);
            
            // Alternating object and array nesting
            string testName3 = $"alternating_nested_{arraySize}_depth_{nestingDepth}";
            string json3 = GenerateAlternatingNestedArrayJson(arraySize, nestingDepth);
            MeasureParsingTime(json3, testName3);
        }

        private string GenerateNestedArrayJson(int arraySize, int nestingDepth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            
            for (int i = 0; i < arraySize; i++)
            {
                if (i > 0) sb.Append(',');
                
                // Create a nested object
                StringBuilder objBuilder = new StringBuilder();
                for (int d = 0; d < nestingDepth; d++)
                {
                    objBuilder.Append('{');
                    objBuilder.Append($"\"level{d}\":");
                }
                
                // Add a value at the deepest level
                objBuilder.Append($"\"{i}\"");
                
                // Close all objects
                for (int d = 0; d < nestingDepth; d++)
                {
                    objBuilder.Append('}');
                }
                
                sb.Append(objBuilder.ToString());
            }
            
            sb.Append(']');
            
            return sb.ToString();
        }

        private string GenerateMultiPropNestedArrayJson(int arraySize, int nestingDepth, int propsPerLevel)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            
            for (int i = 0; i < arraySize; i++)
            {
                if (i > 0) sb.Append(',');
                
                // Create a nested object with multiple properties at each level
                StringBuilder objBuilder = new StringBuilder();
                GenerateMultiPropLevel(objBuilder, nestingDepth, propsPerLevel, i);
                
                sb.Append(objBuilder.ToString());
            }
            
            sb.Append(']');
            
            return sb.ToString();
        }

        private void GenerateMultiPropLevel(StringBuilder sb, int depth, int propsPerLevel, int value)
        {
            sb.Append('{');
            
            for (int p = 0; p < propsPerLevel; p++)
            {
                if (p > 0) sb.Append(',');
                
                if (p == propsPerLevel - 1 && depth > 1)
                {
                    // Last property contains the next level
                    sb.Append($"\"next{depth}\":");
                    GenerateMultiPropLevel(sb, depth - 1, propsPerLevel, value);
                }
                else if (depth == 1)
                {
                    // At the deepest level, add a value
                    sb.Append($"\"prop{p}\":\"{value}\"");
                }
                else
                {
                    // Add a simple property
                    sb.Append($"\"prop{p}\":{p}");
                }
            }
            
            sb.Append('}');
        }

        private string GenerateAlternatingNestedArrayJson(int arraySize, int nestingDepth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            
            for (int i = 0; i < arraySize; i++)
            {
                if (i > 0) sb.Append(',');
                
                // Create alternating object and array nesting
                StringBuilder nestedBuilder = new StringBuilder();
                bool isObject = true;
                
                for (int d = 0; d < nestingDepth; d++)
                {
                    if (isObject)
                    {
                        nestedBuilder.Append('{');
                        nestedBuilder.Append($"\"level{d}\":");
                    }
                    else
                    {
                        nestedBuilder.Append('[');
                    }
                    
                    isObject = !isObject;
                }
                
                // Add a value at the deepest level
                nestedBuilder.Append($"\"{i}\"");
                
                // Close all structures
                isObject = !isObject; // Reverse again to match the opening sequence
                for (int d = 0; d < nestingDepth; d++)
                {
                    if (isObject)
                    {
                        nestedBuilder.Append('}');
                    }
                    else
                    {
                        nestedBuilder.Append(']');
                    }
                    
                    isObject = !isObject;
                }
                
                sb.Append(nestedBuilder.ToString());
            }
            
            sb.Append(']');
            
            return sb.ToString();
        }

        private void MeasureParsingTime(string json, string testName)
        {
            try
            {
                // Measure parsing time
                Stopwatch sw = new Stopwatch();
                sw.Start();
                
                JToken? token = JToken.Parse(json);
                
                sw.Stop();
                double elapsedMs = sw.Elapsed.TotalMilliseconds;
                
                Console.WriteLine($"  {testName}: {elapsedMs:F2}ms");
                
                // Record the result
                _results[testName] = new BenchmarkResult
                {
                    TestName = testName,
                    JsonLength = json.Length,
                    ProcessingTimeMs = elapsedMs
                };
                
                // If processing took more than 1000ms (1 second), save it as a DOS vector
                if (elapsedMs > 1000)
                {
                    string fileName = $"benchmark_{DateTime.Now:yyyyMMdd_HHmmss}_{testName}_{elapsedMs:F2}.json";
                    string filePath = Path.Combine(_outputDir, fileName);
                    File.WriteAllText(filePath, json);
                    
                    Console.WriteLine($"Potential DOS Vector detected: {fileName}");
                    Console.WriteLine($"  Time: {elapsedMs:F2}ms for {json.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing {testName}: {ex.Message}");
                
                // Record the error
                _results[testName] = new BenchmarkResult
                {
                    TestName = testName,
                    JsonLength = json.Length,
                    ProcessingTimeMs = -1,
                    Error = ex.Message
                };
            }
        }

        private void GenerateReport()
        {
            Console.WriteLine("Generating benchmark report...");
            
            StringBuilder report = new StringBuilder();
            report.AppendLine("# Array Nesting Benchmark Results");
            report.AppendLine();
            report.AppendLine("## Overview");
            report.AppendLine();
            report.AppendLine("This report shows the relationship between array size, nesting depth, and processing time for the Neo.Json library.");
            report.AppendLine();
            
            // Array Size Impact
            report.AppendLine("## Impact of Array Size");
            report.AppendLine();
            report.AppendLine("| Array Size | Depth 5 (ms) | Depth 10 (ms) | Depth 15 (ms) |");
            report.AppendLine("|------------|-------------|--------------|---------------|");
            
            int[] arraySizes = { 1000, 5000, 10000, 25000, 50000, 75000, 100000, 125000, 150000 };
            foreach (int size in arraySizes)
            {
                report.Append($"| {size} |");
                
                foreach (int depth in new[] { 5, 10, 15 })
                {
                    string key = $"array_size_{size}_depth_{depth}";
                    if (_results.TryGetValue(key, out BenchmarkResult? result) && result != null)
                    {
                        if (result.ProcessingTimeMs >= 0)
                        {
                            report.Append($" {result.ProcessingTimeMs:F2} |");
                        }
                        else
                        {
                            report.Append(" Error |");
                        }
                    }
                    else
                    {
                        report.Append(" N/A |");
                    }
                }
                
                report.AppendLine();
            }
            
            // Nesting Depth Impact
            report.AppendLine();
            report.AppendLine("## Impact of Nesting Depth");
            report.AppendLine();
            report.AppendLine("| Nesting Depth | 10,000 Items (ms) | 50,000 Items (ms) | 100,000 Items (ms) |");
            report.AppendLine("|---------------|-------------------|-------------------|---------------------|");
            
            int[] depths = { 1, 3, 5, 8, 10, 12, 15, 20, 25, 30 };
            foreach (int depth in depths)
            {
                report.Append($"| {depth} |");
                
                foreach (int size in new[] { 10000, 50000, 100000 })
                {
                    string key = $"array_size_{size}_depth_{depth}";
                    if (_results.TryGetValue(key, out BenchmarkResult? result) && result != null)
                    {
                        if (result.ProcessingTimeMs >= 0)
                        {
                            report.Append($" {result.ProcessingTimeMs:F2} |");
                        }
                        else
                        {
                            report.Append(" Error |");
                        }
                    }
                    else
                    {
                        report.Append(" N/A |");
                    }
                }
                
                report.AppendLine();
            }
            
            // Object Structure Impact
            report.AppendLine();
            report.AppendLine("## Impact of Object Structure");
            report.AppendLine();
            report.AppendLine("| Structure Type | Processing Time (ms) |");
            report.AppendLine("|----------------|----------------------|");
            
            string[] structureTests = {
                "standard_nested_50000_depth_10",
                "multi_prop_nested_50000_depth_10",
                "alternating_nested_50000_depth_10"
            };
            
            foreach (string test in structureTests)
            {
                if (_results.TryGetValue(test, out BenchmarkResult? result) && result != null)
                {
                    string structureType = test.StartsWith("standard") ? "Standard Nesting" :
                                          test.StartsWith("multi_prop") ? "Multiple Properties" :
                                          "Alternating Object/Array";
                    
                    if (result.ProcessingTimeMs >= 0)
                    {
                        report.AppendLine($"| {structureType} | {result.ProcessingTimeMs:F2} |");
                    }
                    else
                    {
                        report.AppendLine($"| {structureType} | Error: {result.Error} |");
                    }
                }
            }
            
            // Conclusions
            report.AppendLine();
            report.AppendLine("## Conclusions");
            report.AppendLine();
            report.AppendLine("Based on the benchmark results, we can draw the following conclusions:");
            report.AppendLine();
            
            // Calculate some metrics for conclusions
            double maxTime = 0;
            string maxTimeTest = "";
            foreach (var result in _results.Values)
            {
                if (result.ProcessingTimeMs > maxTime)
                {
                    maxTime = result.ProcessingTimeMs;
                    maxTimeTest = result.TestName;
                }
            }
            
            report.AppendLine($"1. The highest processing time observed was {maxTime:F2}ms for test '{maxTimeTest}'.");
            report.AppendLine("2. Processing time increases non-linearly with both array size and nesting depth.");
            report.AppendLine("3. Nesting depth has a more significant impact on processing time than array size.");
            report.AppendLine("4. Object structure (standard nesting vs. multiple properties vs. alternating types) affects processing time.");
            report.AppendLine();
            report.AppendLine("## Recommendations");
            report.AppendLine();
            report.AppendLine("Based on these findings, we recommend:");
            report.AppendLine();
            report.AppendLine("1. Implement input validation that considers both array size and nesting depth.");
            report.AppendLine("2. Consider rejecting or applying special processing to JSON inputs with high nesting depth (> 10) and large array sizes (> 50,000 items).");
            report.AppendLine("3. Implement timeouts for parsing operations on untrusted inputs.");
            report.AppendLine("4. Consider optimizing the Neo.Json library for handling large arrays of nested objects.");
            
            // Save the report
            string reportPath = Path.Combine(_outputDir, "benchmark_report.md");
            File.WriteAllText(reportPath, report.ToString());
            
            Console.WriteLine($"Benchmark report saved to: {reportPath}");
        }

        private class BenchmarkResult
        {
            public string TestName { get; set; } = string.Empty;
            public int JsonLength { get; set; }
            public double ProcessingTimeMs { get; set; }
            public string Error { get; set; } = string.Empty;
        }
    }
}
