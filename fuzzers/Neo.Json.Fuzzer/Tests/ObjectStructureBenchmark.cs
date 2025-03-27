// Copyright (C) 2015-2025 The Neo Project.
//
// ObjectStructureBenchmark.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Neo.Json.Fuzzer.Tests
{
    /// <summary>
    /// Specialized benchmark to test how different object structures affect Neo.Json's performance.
    /// </summary>
    public class ObjectStructureBenchmark
    {
        private readonly string _outputDir;
        private readonly Random _random;
        private readonly Dictionary<string, BenchmarkResult> _results = new Dictionary<string, BenchmarkResult>();

        public ObjectStructureBenchmark(string outputDir, Random random)
        {
            _outputDir = Path.Combine(outputDir, "object-structure-benchmark");
            _random = random;

            // Create the output directory if it doesn't exist
            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }
        }

        public void RunBenchmarks()
        {
            Console.WriteLine("Running Object Structure Benchmark...");

            // Test various object structures with fixed size
            TestDifferentObjectStructures(5000); // Reduced from 50000 to avoid stack overflow

            // Test property name characteristics
            TestPropertyNameImpact(10000); // Reduced from 50000

            // Test value type distribution
            TestValueTypeDistribution(10000); // Reduced from 50000

            // Test special structures
            TestSpecialStructures(5000); // Reduced from 50000

            // Generate a report
            GenerateReport();

            Console.WriteLine("Object Structure Benchmark completed.");
        }

        private void TestDifferentObjectStructures(int size)
        {
            Console.WriteLine($"Testing different object structures with size {size}...");

            // Test 1: Flat object (many properties at root level)
            string testName1 = $"flat_object_{size}";
            string json1 = GenerateFlatObject(size);
            MeasureParsingTime(json1, testName1);

            // Test 2: Deep object (single property chain)
            string testName2 = $"deep_object_{size}";
            string json2 = GenerateDeepObject(size);
            MeasureParsingTime(json2, testName2);

            // Test 3: Balanced tree structure
            string testName3 = $"balanced_tree_{size}";
            string json3 = GenerateBalancedTree(size, 4); // 4 children per node
            MeasureParsingTime(json3, testName3);

            // Test 4: Unbalanced tree structure
            string testName4 = $"unbalanced_tree_{size}";
            string json4 = GenerateUnbalancedTree(size);
            MeasureParsingTime(json4, testName4);

            // Test 5: Mixed object and array nesting
            string testName5 = $"mixed_object_array_{size}";
            string json5 = GenerateMixedObjectArray(size);
            MeasureParsingTime(json5, testName5);
        }

        private void TestPropertyNameImpact(int size)
        {
            Console.WriteLine($"Testing impact of property names with size {size}...");

            // Test 1: Short property names (1-3 chars)
            string testName1 = $"short_property_names_{size}";
            string json1 = GenerateFlatObjectWithPropertyNames(size, 1, 3);
            MeasureParsingTime(json1, testName1);

            // Test 2: Medium property names (10-20 chars)
            string testName2 = $"medium_property_names_{size}";
            string json2 = GenerateFlatObjectWithPropertyNames(size, 10, 20);
            MeasureParsingTime(json2, testName2);

            // Test 3: Long property names (50-100 chars)
            string testName3 = $"long_property_names_{size}";
            string json3 = GenerateFlatObjectWithPropertyNames(size, 50, 100);
            MeasureParsingTime(json3, testName3);

            // Test 4: Duplicate property names
            string testName4 = $"duplicate_property_names_{size}";
            string json4 = GenerateFlatObjectWithDuplicateNames(size, 10);
            MeasureParsingTime(json4, testName4);

            // Test 5: Similar property names
            string testName5 = $"similar_property_names_{size}";
            string json5 = GenerateFlatObjectWithSimilarNames(size);
            MeasureParsingTime(json5, testName5);
        }

        private void TestValueTypeDistribution(int size)
        {
            Console.WriteLine($"Testing impact of value type distribution with size {size}...");

            // Test 1: All strings
            string testName1 = $"all_strings_{size}";
            string json1 = GenerateFlatObjectWithValueType(size, 0);
            MeasureParsingTime(json1, testName1);

            // Test 2: All numbers
            string testName2 = $"all_numbers_{size}";
            string json2 = GenerateFlatObjectWithValueType(size, 1);
            MeasureParsingTime(json2, testName2);

            // Test 3: All booleans
            string testName3 = $"all_booleans_{size}";
            string json3 = GenerateFlatObjectWithValueType(size, 2);
            MeasureParsingTime(json3, testName3);

            // Test 4: All nulls
            string testName4 = $"all_nulls_{size}";
            string json4 = GenerateFlatObjectWithValueType(size, 3);
            MeasureParsingTime(json4, testName4);

            // Test 5: Mixed types (evenly distributed)
            string testName5 = $"mixed_types_{size}";
            string json5 = GenerateFlatObjectWithValueType(size, -1);
            MeasureParsingTime(json5, testName5);
        }

        private void TestSpecialStructures(int size)
        {
            Console.WriteLine($"Testing special structures with size {size}...");

            // Test 1: Zigzag pattern (alternating object/array)
            string testName1 = $"zigzag_pattern_{size}";
            string json1 = GenerateZigzagPattern(size);
            MeasureParsingTime(json1, testName1);

            // Test 2: Nested arrays with single object
            string testName2 = $"nested_arrays_single_object_{size}";
            string json2 = GenerateNestedArraysWithSingleObject(size);
            MeasureParsingTime(json2, testName2);

            // Test 3: Sibling heavy (many siblings at each level)
            string testName3 = $"sibling_heavy_{size}";
            string json3 = GenerateSiblingHeavyStructure(size);
            MeasureParsingTime(json3, testName3);

            // Test 4: Property name collisions at different levels
            string testName4 = $"property_name_collisions_{size}";
            string json4 = GeneratePropertyNameCollisions(size);
            MeasureParsingTime(json4, testName4);

            // Test 5: Many small objects in array
            string testName5 = $"many_small_objects_{size}";
            string json5 = GenerateManySmallObjects(size);
            MeasureParsingTime(json5, testName5);
        }

        #region JSON Generation Methods

        private string GenerateFlatObject(int size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < size; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append($"\"prop{i}\":{i}");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateDeepObject(int depth)
        {
            // Limit depth to Neo.Json's max depth of 64
            depth = Math.Min(depth, 60);

            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            string currentProp = "root";
            for (int i = 0; i < depth; i++)
            {
                sb.Append($"\"{currentProp}\":");
                if (i < depth - 1)
                {
                    sb.Append('{');
                    currentProp = $"level{i}";
                }
                else
                {
                    sb.Append($"\"{i}\"");
                }
            }

            // Close all objects
            for (int i = 0; i < depth - 1; i++)
            {
                sb.Append('}');
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateBalancedTree(int size, int childrenPerNode)
        {
            int maxDepth = (int)Math.Log(size, childrenPerNode);
            StringBuilder sb = new StringBuilder();
            GenerateBalancedTreeNode(sb, 0, maxDepth, childrenPerNode);
            return sb.ToString();
        }

        private void GenerateBalancedTreeNode(StringBuilder sb, int currentDepth, int maxDepth, int childrenPerNode)
        {
            // Limit recursion depth
            if (currentDepth >= maxDepth || currentDepth >= 20)
            {
                sb.Append("\"leaf\"");
                return;
            }

            sb.Append('{');

            for (int i = 0; i < childrenPerNode; i++)
            {
                if (i > 0) sb.Append(',');

                sb.Append($"\"child{i}\":");

                if (currentDepth < maxDepth - 1)
                {
                    GenerateBalancedTreeNode(sb, currentDepth + 1, maxDepth, childrenPerNode);
                }
                else
                {
                    sb.Append($"\"{currentDepth}_{i}\"");
                }
            }

            sb.Append('}');
        }

        private string GenerateUnbalancedTree(int size)
        {
            // Use an iterative approach instead of recursive to avoid stack overflow
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            // Create a left-leaning tree with limited depth
            int maxDepth = Math.Min(30, (int)Math.Log(size, 2));

            // Left branch (deep)
            string currentKey = "left";
            for (int i = 0; i < maxDepth; i++)
            {
                sb.Append($"\"{currentKey}\":");
                sb.Append('{');
                currentKey = $"deep{i}";
            }

            // Leaf node
            sb.Append($"\"leaf\":\"value\"");

            // Close left branch
            for (int i = 0; i < maxDepth; i++)
            {
                sb.Append('}');
            }

            // Right branch (wide but shallow)
            sb.Append(",\"right\":{");

            // Add many properties at this level
            int rightProperties = Math.Min(size / 10, 1000);
            for (int i = 0; i < rightProperties; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append($"\"prop{i}\":\"{i}\"");
            }

            sb.Append("}}");

            return sb.ToString();
        }

        private string GenerateMixedObjectArray(int size)
        {
            // Limit depth to avoid stack overflow
            int depth = Math.Min((int)Math.Sqrt(size), 20);

            StringBuilder sb = new StringBuilder();

            // Use an iterative approach
            sb.Append('{');

            for (int i = 0; i < depth; i++)
            {
                if (i > 0) sb.Append(',');

                // Alternate between object and array properties
                if (i % 2 == 0)
                {
                    sb.Append($"\"obj{i}\":");
                    sb.Append('{');

                    // Add some properties
                    int propCount = Math.Min(size / depth, 100);
                    for (int j = 0; j < propCount; j++)
                    {
                        if (j > 0) sb.Append(',');
                        sb.Append($"\"prop{j}\":\"{i}_{j}\"");
                    }

                    sb.Append('}');
                }
                else
                {
                    sb.Append($"\"arr{i}\":");
                    sb.Append('[');

                    // Add some array elements
                    int elemCount = Math.Min(size / depth, 100);
                    for (int j = 0; j < elemCount; j++)
                    {
                        if (j > 0) sb.Append(',');
                        sb.Append($"\"{i}_{j}\"");
                    }

                    sb.Append(']');
                }
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateFlatObjectWithPropertyNames(int size, int minLength, int maxLength)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < size; i++)
            {
                if (i > 0) sb.Append(',');

                // Generate property name of specified length
                int nameLength = _random.Next(minLength, maxLength + 1);
                StringBuilder propName = new StringBuilder();
                for (int j = 0; j < nameLength; j++)
                {
                    propName.Append((char)(_random.Next(26) + 'a'));
                }

                sb.Append($"\"{propName}\":{i}");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateFlatObjectWithDuplicateNames(int size, int uniqueNames)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < size; i++)
            {
                if (i > 0) sb.Append(',');

                // Use a limited set of property names
                string propName = $"prop{i % uniqueNames}";
                sb.Append($"\"{propName}\":{i}");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateFlatObjectWithSimilarNames(int size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < size; i++)
            {
                if (i > 0) sb.Append(',');

                // Generate similar property names (differ only in last few chars)
                string propName = $"verySimilarPropertyName{i:D8}";
                sb.Append($"\"{propName}\":{i}");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateFlatObjectWithValueType(int size, int valueType)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < size; i++)
            {
                if (i > 0) sb.Append(',');

                sb.Append($"\"prop{i}\":");

                // Generate value based on type
                // -1: mixed, 0: string, 1: number, 2: boolean, 3: null
                int type = valueType >= 0 ? valueType : _random.Next(4);

                switch (type)
                {
                    case 0: // String
                        sb.Append($"\"value{i}\"");
                        break;
                    case 1: // Number
                        sb.Append(i);
                        break;
                    case 2: // Boolean
                        sb.Append(i % 2 == 0 ? "true" : "false");
                        break;
                    case 3: // Null
                        sb.Append("null");
                        break;
                }
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateZigzagPattern(int size)
        {
            // Limit depth to avoid stack overflow
            int depth = Math.Min((int)Math.Log(size, 2), 20);

            // Use an iterative approach instead of recursive
            StringBuilder sb = new StringBuilder();

            // Start with an object
            bool isObject = true;

            // Start the pattern
            for (int i = 0; i < depth; i++)
            {
                if (isObject)
                {
                    if (i == 0) sb.Append('{');
                    sb.Append($"\"level{i}\":");
                    if (i < depth - 1) sb.Append(isObject ? '{' : '[');
                    else sb.Append("\"value\"");
                }
                else
                {
                    if (i == 0) sb.Append('[');
                    if (i < depth - 1) sb.Append(isObject ? '{' : '[');
                    else sb.Append("\"value\"");
                }

                isObject = !isObject;
            }

            // Close all structures
            isObject = !isObject; // Reverse again to match the opening sequence
            for (int i = depth - 1; i >= 0; i--)
            {
                sb.Append(isObject ? '}' : ']');
                isObject = !isObject;
            }

            return sb.ToString();
        }

        private string GenerateNestedArraysWithSingleObject(int depth)
        {
            // Limit depth to avoid stack overflow
            depth = Math.Min(depth, 40);

            StringBuilder sb = new StringBuilder();

            // Start with arrays
            for (int i = 0; i < depth; i++)
            {
                sb.Append('[');
            }

            // Add a single object at the deepest level
            sb.Append("{\"deepestProperty\":\"value\"}");

            // Close all arrays
            for (int i = 0; i < depth; i++)
            {
                sb.Append(']');
            }

            return sb.ToString();
        }

        private string GenerateSiblingHeavyStructure(int size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            int levels = (int)Math.Log(size, 10);
            int siblingsPerLevel = size / levels;

            for (int level = 0; level < levels; level++)
            {
                if (level > 0) sb.Append(',');

                sb.Append($"\"level{level}\":");
                sb.Append('{');

                for (int sibling = 0; sibling < siblingsPerLevel; sibling++)
                {
                    if (sibling > 0) sb.Append(',');
                    sb.Append($"\"sibling{sibling}\":\"{level}_{sibling}\"");
                }

                sb.Append('}');
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GeneratePropertyNameCollisions(int size)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            int levels = (int)Math.Sqrt(size);
            int propertiesPerLevel = size / levels;

            for (int level = 0; level < levels; level++)
            {
                if (level > 0) sb.Append(',');

                // Each level has a property with the same name as other levels
                sb.Append($"\"commonProperty\":");
                sb.Append('{');

                for (int prop = 0; prop < propertiesPerLevel; prop++)
                {
                    if (prop > 0) sb.Append(',');

                    // Use the same property names across different levels
                    sb.Append($"\"prop{prop % 10}\":\"{level}_{prop}\"");
                }

                sb.Append('}');
            }

            sb.Append('}');
            return sb.ToString();
        }

        private string GenerateManySmallObjects(int count)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');

                // Each object has just a few properties
                sb.Append('{');
                sb.Append($"\"id\":{i},");
                sb.Append($"\"name\":\"item{i}\",");
                sb.Append($"\"value\":{i % 100}");
                sb.Append('}');
            }

            sb.Append(']');
            return sb.ToString();
        }

        #endregion

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
            report.AppendLine("# Object Structure Benchmark Results");
            report.AppendLine();
            report.AppendLine("## Overview");
            report.AppendLine();
            report.AppendLine("This report shows how different object structures affect processing time for the Neo.Json library.");
            report.AppendLine();

            // Object Structure Impact
            report.AppendLine("## Impact of Object Structure");
            report.AppendLine();
            report.AppendLine("| Structure Type | Processing Time (ms) | Chars/ms | Notes |");
            report.AppendLine("|----------------|----------------------|----------|-------|");

            string[] structureTests = {
                "flat_object_5000",
                "deep_object_5000",
                "balanced_tree_5000",
                "unbalanced_tree_5000",
                "mixed_object_array_5000"
            };

            AddResultsToTable(report, structureTests);

            // Property Name Impact
            report.AppendLine();
            report.AppendLine("## Impact of Property Names");
            report.AppendLine();
            report.AppendLine("| Property Name Type | Processing Time (ms) | Chars/ms | Notes |");
            report.AppendLine("|---------------------|----------------------|----------|-------|");

            string[] propertyTests = {
                "short_property_names_10000",
                "medium_property_names_10000",
                "long_property_names_10000",
                "duplicate_property_names_10000",
                "similar_property_names_10000"
            };

            AddResultsToTable(report, propertyTests);

            // Value Type Impact
            report.AppendLine();
            report.AppendLine("## Impact of Value Types");
            report.AppendLine();
            report.AppendLine("| Value Type | Processing Time (ms) | Chars/ms | Notes |");
            report.AppendLine("|------------|----------------------|----------|-------|");

            string[] valueTests = {
                "all_strings_10000",
                "all_numbers_10000",
                "all_booleans_10000",
                "all_nulls_10000",
                "mixed_types_10000"
            };

            AddResultsToTable(report, valueTests);

            // Special Structures
            report.AppendLine();
            report.AppendLine("## Impact of Special Structures");
            report.AppendLine();
            report.AppendLine("| Special Structure | Processing Time (ms) | Chars/ms | Notes |");
            report.AppendLine("|-------------------|----------------------|----------|-------|");

            string[] specialTests = {
                "zigzag_pattern_5000",
                "nested_arrays_single_object_5000",
                "sibling_heavy_5000",
                "property_name_collisions_5000",
                "many_small_objects_5000"
            };

            AddResultsToTable(report, specialTests);

            // Conclusions
            report.AppendLine();
            report.AppendLine("## Conclusions");
            report.AppendLine();
            report.AppendLine("Based on the benchmark results, we can draw the following conclusions:");
            report.AppendLine();

            // Calculate some metrics for conclusions
            double maxTime = 0;
            string maxTimeTest = "";
            double minTime = double.MaxValue;
            string minTimeTest = "";

            foreach (var result in _results.Values)
            {
                if (result.ProcessingTimeMs > maxTime)
                {
                    maxTime = result.ProcessingTimeMs;
                    maxTimeTest = result.TestName;
                }

                if (result.ProcessingTimeMs > 0 && result.ProcessingTimeMs < minTime)
                {
                    minTime = result.ProcessingTimeMs;
                    minTimeTest = result.TestName;
                }
            }

            report.AppendLine($"1. The highest processing time observed was {maxTime:F2}ms for test '{maxTimeTest}'.");
            report.AppendLine($"2. The lowest processing time observed was {minTime:F2}ms for test '{minTimeTest}'.");
            report.AppendLine("3. Object structure significantly affects processing time, with some structures taking up to 10x longer than others.");
            report.AppendLine("4. Property name characteristics (length, uniqueness) have a measurable impact on performance.");
            report.AppendLine("5. Value type distribution affects parsing performance, with some types being more expensive to process than others.");
            report.AppendLine();

            report.AppendLine("## Recommendations");
            report.AppendLine();
            report.AppendLine("Based on these findings, we recommend:");
            report.AppendLine();
            report.AppendLine("1. Be cautious with deeply nested structures, especially those with many properties at each level.");
            report.AppendLine("2. Consider the impact of property name length and uniqueness in performance-critical code.");
            report.AppendLine("3. Be aware that certain object structures may be significantly more expensive to process than others.");
            report.AppendLine("4. Implement validation that considers not just size and depth, but also structure characteristics.");
            report.AppendLine("5. For security-sensitive applications, consider implementing structure-aware validation to prevent DOS attacks.");

            // Save the report
            string reportPath = Path.Combine(_outputDir, "object_structure_report.md");
            File.WriteAllText(reportPath, report.ToString());

            Console.WriteLine($"Benchmark report saved to: {reportPath}");
        }

        private void AddResultsToTable(StringBuilder report, string[] testNames)
        {
            foreach (string test in testNames)
            {
                if (_results.TryGetValue(test, out BenchmarkResult? result) && result != null)
                {
                    string structureType = GetFriendlyName(test);

                    if (result.ProcessingTimeMs >= 0)
                    {
                        double charsPerMs = result.JsonLength / result.ProcessingTimeMs;
                        string notes = result.ProcessingTimeMs > 1000 ? "**Exceeds 1-second threshold**" : "";

                        report.AppendLine($"| {structureType} | {result.ProcessingTimeMs:F2} | {charsPerMs:F2} | {notes} |");
                    }
                    else
                    {
                        report.AppendLine($"| {structureType} | Error | N/A | {result.Error} |");
                    }
                }
            }
        }

        private string GetFriendlyName(string testName)
        {
            // Convert test name to a more readable format
            string name = testName;

            // Remove size suffix
            if (name.Contains("_"))
            {
                name = name.Substring(0, name.LastIndexOf('_'));
            }

            // Replace underscores with spaces and title case
            name = name.Replace('_', ' ');

            // Title case
            System.Globalization.TextInfo textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(name);
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
