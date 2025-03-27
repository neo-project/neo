// Copyright (C) 2015-2025 The Neo Project.
//
// CustomDOSTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Neo.Json.Fuzzer.Tests
{
    /// <summary>
    /// Custom tests to find DOS vectors that take over 1 second to process
    /// </summary>
    public class CustomDOSTest
    {
        private readonly string _outputDir;
        private readonly Random _random;

        public CustomDOSTest(string outputDir, int? seed = null)
        {
            _outputDir = Path.Combine(outputDir, "custom-dos-vectors");
            Directory.CreateDirectory(_outputDir);
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void RunTests()
        {
            Console.WriteLine("Running Custom DOS Tests to find vectors taking over 1 second...");

            // Test various potential DOS vectors
            TestDeeplyNestedObjects(60); // Stay under the 64 limit
            TestDeeplyNestedArrays(60); // Stay under the 64 limit
            TestLargeStrings(5_000_000); // Increased size
            TestLargeNumbers(100000); // Increased count
            TestComplexRegexPatterns(1000); // Increased complexity
            TestRecursiveStructuresFixed(30); // Increased depth
            TestLargePropertyNames(1000, 5000); // Increased size and count
            TestRepeatedPropertyNames(100000); // Increased count
            TestDeepPathQueries(60); // Increased depth
            TestLargeNumberPrecision(5000); // Increased precision
            TestManySmallObjects(200000); // Doubled size
            TestBackAndForthConversions(5000); // Increased iterations
            TestNestedArraysWithLargeStrings(50, 10000); // New test
            TestExtremelyLargeJson(10_000_000); // New test

            // Additional targeted tests
            TestComplexObjectWithManyTypes(50000); // New targeted test
            TestAlternatingArrayTypes(100000); // New targeted test
            TestDeepObjectWithPathAccess(60); // New targeted test
            TestStringWithSpecialCharactersFixed(1000000); // Fixed test
            TestObjectWithEscapedPropertyNames(10000); // New targeted test
            TestLargeUnicodeStringsFixed(1000000); // Fixed test
            TestNestedObjectsWithIdenticalStructure(40); // New targeted test
            TestJsonWithComments(1000000); // New targeted test - may not be supported

            // More extreme tests
            TestCombinedComplexStructure(100000); // New extreme test
            TestPropertyLookupPerformance(200000); // New extreme test
            TestLargeArrayWithNestedObjects(100000, 10); // New extreme test
            TestDeepPathWithManyProperties(50, 1000); // New extreme test

            // Final extreme test - this came closest to our threshold
            TestLargeArrayWithNestedObjects(150000, 15); // Final extreme test with increased parameters

            Console.WriteLine("Custom DOS Tests completed.");
        }

        private void TestDeeplyNestedObjects(int depth)
        {
            Console.WriteLine($"Testing deeply nested objects with depth {depth}...");
            StringBuilder sb = new StringBuilder();

            // Create opening braces
            for (int i = 0; i < depth; i++)
            {
                sb.Append("{\"level").Append(i).Append("\":");
            }

            // Add a value at the deepest level
            sb.Append("0");

            // Close all braces
            for (int i = 0; i < depth; i++)
            {
                sb.Append("}");
            }

            string json = sb.ToString();
            MeasureParsingTime(json, $"nested_objects_{depth}");
        }

        private void TestDeeplyNestedArrays(int depth)
        {
            Console.WriteLine($"Testing deeply nested arrays with depth {depth}...");
            StringBuilder sb = new StringBuilder();

            // Create opening brackets
            for (int i = 0; i < depth; i++)
            {
                sb.Append("[");
            }

            // Add a value at the deepest level
            sb.Append("0");

            // Close all brackets
            for (int i = 0; i < depth; i++)
            {
                sb.Append("]");
            }

            string json = sb.ToString();
            MeasureParsingTime(json, $"nested_arrays_{depth}");
        }

        private void TestLargeStrings(int length)
        {
            Console.WriteLine($"Testing large string with length {length}...");
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"largeString\":\"");

            // Generate a large string
            for (int i = 0; i < length; i++)
            {
                sb.Append((char)(_random.Next(26) + 'a'));
            }

            sb.Append("\"}");

            string json = sb.ToString();
            MeasureParsingTime(json, $"large_string_{length}");
        }

        private void TestLargeNumbers(int count)
        {
            Console.WriteLine($"Testing large number of numeric values {count}...");
            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            // Generate many numbers
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');

                // Mix of integers, decimals, and scientific notation
                switch (_random.Next(3))
                {
                    case 0:
                        sb.Append(_random.Next(int.MaxValue));
                        break;
                    case 1:
                        sb.Append(_random.NextDouble() * 1000000);
                        break;
                    case 2:
                        sb.Append(_random.NextDouble()).Append("e").Append(_random.Next(100));
                        break;
                }
            }

            sb.Append(']');

            string json = sb.ToString();
            MeasureParsingTime(json, $"large_numbers_{count}");
        }

        private void TestComplexRegexPatterns(int depth)
        {
            Console.WriteLine($"Testing complex regex-like patterns with depth {depth}...");
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"pattern\":\"");

            // Create a complex pattern that might trigger backtracking
            for (int i = 0; i < depth; i++)
            {
                sb.Append("(a+)+");
            }

            sb.Append("\"}");

            string json = sb.ToString();
            MeasureParsingTime(json, $"complex_regex_{depth}");
        }

        private void TestRecursiveStructuresFixed(int depth)
        {
            Console.WriteLine($"Testing recursive structures with controlled depth {depth}...");

            // Create a more controlled recursive structure
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            // Create a chain of nested objects with arrays
            string currentPath = "root";
            for (int i = 0; i < depth; i++)
            {
                sb.Append($"\"{currentPath}\":");

                if (i % 2 == 0)
                {
                    // Object
                    sb.Append("{");
                    currentPath = $"level{i}";
                }
                else
                {
                    // Array
                    sb.Append("[{");
                    currentPath = $"item{i}";
                }
            }

            // Add a value at the deepest level
            sb.Append("\"value\":0");

            // Close all braces
            for (int i = 0; i < depth; i++)
            {
                if (i % 2 == 0)
                {
                    sb.Append("}");
                }
                else
                {
                    sb.Append("}]");
                }
            }

            string json = sb.ToString();
            MeasureParsingTime(json, $"recursive_fixed_{depth}");
        }

        private void TestLargePropertyNames(int count, int length)
        {
            Console.WriteLine($"Testing {count} properties with names of length {length}...");
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            // Generate many properties with long names
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');

                sb.Append('"');
                for (int j = 0; j < length; j++)
                {
                    sb.Append((char)(_random.Next(26) + 'a'));
                }
                sb.Append("\":0");
            }

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"large_property_names_{count}_{length}");
        }

        private void TestRepeatedPropertyNames(int count)
        {
            Console.WriteLine($"Testing object with {count} repeated property names...");
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            // Generate many properties with the same name
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');
                // Use a different property name for each property to avoid the duplicate property error
                sb.Append($"\"prop{i}\":").Append(i);
            }

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"many_properties_{count}");
        }

        private void TestDeepPathQueries(int depth)
        {
            Console.WriteLine($"Testing deep path queries with depth {depth}...");

            // Create a deeply nested object for path queries
            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            string currentPath = "root";
            for (int i = 0; i < depth; i++)
            {
                sb.Append($"\"{currentPath}\":");
                sb.Append('{');
                currentPath = $"level{i}";
            }

            // Add a value at the deepest level
            sb.Append($"\"{currentPath}\":0");

            // Close all the nested objects
            for (int i = 0; i <= depth; i++)
            {
                sb.Append("}");
            }

            string json = sb.ToString();

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Parse the JSON
                JToken? token = JToken.Parse(json);

                // Add null check before using token
                if (token == null)
                {
                    sw.Stop();
                    Console.WriteLine("Failed to parse JSON (null token)");
                    return;
                }

                // Now try to access the deepest path
                string path = "$";
                for (int i = 0; i < depth; i++)
                {
                    path += $".level{i}";
                }

                // Measure the time to query the path
                var result = token.JsonPath(path);

                sw.Stop();
                double elapsedMs = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine($"  Path query time: {elapsedMs:F2}ms");

                // If parsing took more than 1000ms (1 second), save it as a DOS vector
                if (elapsedMs > 1000)
                {
                    string fileName = $"custom_dos_{DateTime.Now:yyyyMMdd_HHmmss}_path_query_{depth}_{elapsedMs:F2}.json";
                    string filePath = Path.Combine(_outputDir, fileName);
                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"DOS Vector detected: {fileName}");
                    Console.WriteLine($"  Score: {elapsedMs:F2}");
                    Console.WriteLine($"  Time: {elapsedMs:F2}ms for {json.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing path query: {ex.Message}");
            }
        }

        private void TestLargeNumberPrecision(int digits)
        {
            Console.WriteLine($"Testing number with {digits} digits...");
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"largeNumber\":");

            // Generate a number with many digits
            sb.Append("1");
            for (int i = 0; i < digits; i++)
            {
                sb.Append(_random.Next(10));
            }

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"large_number_precision_{digits}");
        }

        private void TestManySmallObjects(int count)
        {
            Console.WriteLine($"Testing array with {count} small objects...");
            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            // Generate many small objects
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(i).Append(",\"value\":\"test\"}");
            }

            sb.Append(']');

            string json = sb.ToString();
            MeasureParsingTime(json, $"many_small_objects_{count}");
        }

        private void TestBackAndForthConversions(int iterations)
        {
            Console.WriteLine($"Testing back and forth conversions with {iterations} iterations...");

            // Start with a simple object
            string json = "{\"value\":0}";

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Parse and stringify repeatedly
                for (int i = 0; i < iterations; i++)
                {
                    JToken token = JToken.Parse(json);
                    json = token.ToString();
                }

                sw.Stop();
                double elapsedMs = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine($"  Conversion time: {elapsedMs:F2}ms");

                // If processing took more than 1000ms (1 second), save it as a DOS vector
                if (elapsedMs > 1000)
                {
                    string fileName = $"custom_dos_{DateTime.Now:yyyyMMdd_HHmmss}_conversions_{iterations}_{elapsedMs:F2}.json";
                    string filePath = Path.Combine(_outputDir, fileName);
                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"DOS Vector detected: {fileName}");
                    Console.WriteLine($"  Score: {elapsedMs:F2}");
                    Console.WriteLine($"  Time: {elapsedMs:F2}ms for {json.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing conversions: {ex.Message}");
            }
        }

        private void TestNestedArraysWithLargeStrings(int depth, int stringLength)
        {
            Console.WriteLine($"Testing nested arrays with large strings (depth: {depth}, string length: {stringLength})...");

            // Generate a large string
            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < stringLength; i++)
            {
                strBuilder.Append((char)(_random.Next(26) + 'a'));
            }
            string largeString = strBuilder.ToString();

            // Create nested arrays with the large string
            StringBuilder sb = new StringBuilder();

            // Opening brackets
            for (int i = 0; i < depth; i++)
            {
                sb.Append('[');
            }

            // Add the large string
            sb.Append('"').Append(largeString).Append('"');

            // Closing brackets
            for (int i = 0; i < depth; i++)
            {
                sb.Append(']');
            }

            string json = sb.ToString();
            MeasureParsingTime(json, $"nested_arrays_large_string_{depth}_{stringLength}");
        }

        private void TestExtremelyLargeJson(int size)
        {
            Console.WriteLine($"Testing extremely large JSON ({size} bytes)...");

            // Create a large JSON object with many properties
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            int propertiesCount = size / 100; // Approximate number of properties
            for (int i = 0; i < propertiesCount; i++)
            {
                if (i > 0) sb.Append(',');

                // Property name
                sb.Append($"\"prop{i}\":");

                // Property value (alternating between types)
                switch (i % 5)
                {
                    case 0: // Number
                        sb.Append(_random.Next(10000));
                        break;
                    case 1: // String
                        sb.Append('"');
                        for (int j = 0; j < 20; j++)
                        {
                            sb.Append((char)(_random.Next(26) + 'a'));
                        }
                        sb.Append('"');
                        break;
                    case 2: // Boolean
                        sb.Append(_random.Next(2) == 0 ? "true" : "false");
                        break;
                    case 3: // Null
                        sb.Append("null");
                        break;
                    case 4: // Small object
                        sb.Append("{\"id\":").Append(i).Append("}");
                        break;
                }

                // Check if we've reached the target size
                if (sb.Length >= size - 10)
                {
                    break;
                }
            }

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"extremely_large_json_{size}");
        }

        private void TestComplexObjectWithManyTypes(int count)
        {
            Console.WriteLine($"Testing complex object with {count} properties of different types...");

            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');

                string propName = $"prop{i}";
                sb.Append($"\"{propName}\":");

                // Cycle through different types
                switch (i % 7)
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
                    case 4: // Small object
                        sb.Append($"{{\"id\":{i}}}");
                        break;
                    case 5: // Small array
                        sb.Append($"[{i}, {i + 1}, {i + 2}]");
                        break;
                    case 6: // Nested object
                        sb.Append($"{{\"nested\":{{\"id\":{i}}}}}");
                        break;
                }
            }

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"complex_object_many_types_{count}");
        }

        private void TestAlternatingArrayTypes(int count)
        {
            Console.WriteLine($"Testing array with {count} alternating type elements...");

            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');

                // Alternate between different types
                switch (i % 5)
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
                    case 4: // Small object
                        sb.Append($"{{\"id\":{i}}}");
                        break;
                }
            }

            sb.Append(']');

            string json = sb.ToString();
            MeasureParsingTime(json, $"alternating_array_types_{count}");
        }

        private void TestDeepObjectWithPathAccess(int depth)
        {
            Console.WriteLine($"Testing deep object with path access (depth: {depth})...");

            // Create a deeply nested object
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            string currentPath = "root";
            for (int i = 0; i < depth; i++)
            {
                sb.Append($"\"{currentPath}\":");
                sb.Append('{');
                currentPath = $"level{i}";
            }

            // Add a value at the deepest level
            sb.Append($"\"value\":\"found it!\"");

            // Close all the nested objects
            for (int i = 0; i < depth; i++)
            {
                sb.Append('}');
            }

            sb.Append('}');

            string json = sb.ToString();

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Parse the JSON
                JToken? token = JToken.Parse(json);

                // Add null check before using token
                if (token == null)
                {
                    sw.Stop();
                    Console.WriteLine("Failed to parse JSON (null token)");
                    return;
                }

                // Now try to access the deepest path
                string path = "$";
                for (int i = 0; i < depth; i++)
                {
                    path += $".level{i}";
                }

                // Measure the time to query the path
                var result = token.JsonPath(path);

                sw.Stop();
                double elapsedMs = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine($"  Path access time: {elapsedMs:F2}ms");

                // If processing took more than 1000ms (1 second), save it as a DOS vector
                if (elapsedMs > 1000)
                {
                    string fileName = $"custom_dos_{DateTime.Now:yyyyMMdd_HHmmss}_path_access_{depth}_{elapsedMs:F2}.json";
                    string filePath = Path.Combine(_outputDir, fileName);
                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"DOS Vector detected: {fileName}");
                    Console.WriteLine($"  Score: {elapsedMs:F2}");
                    Console.WriteLine($"  Time: {elapsedMs:F2}ms for {json.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing path access: {ex.Message}");
            }
        }

        private void TestStringWithSpecialCharactersFixed(int length)
        {
            Console.WriteLine($"Testing string with special characters (length: {length})...");

            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"value\":\"");

            // Generate a string with special characters that are properly escaped
            for (int i = 0; i < length; i++)
            {
                // Add various special characters that need escaping
                switch (i % 10)
                {
                    case 0: sb.Append("\\\\"); break; // Escaped backslash
                    case 1: sb.Append("\\\""); break; // Escaped double quote
                    case 2: sb.Append("\\b"); break; // Escaped backspace
                    case 3: sb.Append("\\f"); break; // Escaped form feed
                    case 4: sb.Append("\\n"); break; // Escaped new line
                    case 5: sb.Append("\\r"); break; // Escaped carriage return
                    case 6: sb.Append("\\t"); break; // Escaped tab
                    case 7: sb.Append("\\/"); break; // Escaped forward slash
                    case 8: sb.Append("\\u0020"); break; // Unicode escape
                    case 9: sb.Append((char)(_random.Next(26) + 'a')); break; // Regular character
                }
            }

            sb.Append("\"");
            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"string_special_chars_fixed_{length}");
        }

        private void TestLargeUnicodeStringsFixed(int length)
        {
            Console.WriteLine($"Testing large Unicode string (length: {length})...");

            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"value\":\"");

            // Generate a string with valid Unicode characters
            for (int i = 0; i < length; i++)
            {
                // Add various Unicode characters (avoiding surrogate pairs)
                int charCode = _random.Next(0x20, 0xD700); // Valid Unicode range below surrogate pairs
                sb.Append((char)charCode);
            }

            sb.Append("\"");
            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"large_unicode_string_fixed_{length}");
        }

        private void TestCombinedComplexStructure(int size)
        {
            Console.WriteLine($"Testing combined complex structure with size {size}...");

            // Create a complex structure that combines multiple potential DOS vectors
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            // Add a deeply nested object (but stay under the limit)
            sb.Append("\"nested\":{");
            for (int i = 0; i < 60; i++)
            {
                sb.Append($"\"level{i}\":");
                sb.Append('{');
            }
            sb.Append("\"value\":0");
            for (int i = 0; i < 60; i++)
            {
                sb.Append('}');
            }

            // Add a large array of objects
            sb.Append(",\"largeArray\":[");
            int arraySize = size / 10; // Limit array size to avoid excessive memory usage
            for (int i = 0; i < arraySize; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(i).Append(",\"value\":\"test\"}");
            }
            sb.Append(']');

            // Add properties with long names
            sb.Append(",\"propertiesWithLongNames\":{");
            for (int i = 0; i < 100; i++)
            {
                if (i > 0) sb.Append(',');

                // Long property name
                sb.Append('"');
                for (int j = 0; j < 100; j++)
                {
                    sb.Append('p');
                }
                sb.Append(i).Append("\":");

                // Simple value
                sb.Append(i);
            }
            sb.Append('}');

            // Add a property with a large string value
            sb.Append(",\"largeString\":\"");
            for (int i = 0; i < size / 10; i++)
            {
                sb.Append((char)(_random.Next(26) + 'a'));
            }
            sb.Append("\"");

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"combined_complex_structure_{size}");
        }

        private void TestPropertyLookupPerformance(int count)
        {
            Console.WriteLine($"Testing property lookup performance with {count} properties...");

            // Create an object with many properties
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            // Add many properties with unique names
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append($"\"prop{i}\":{i}");
            }

            sb.Append('}');

            string json = sb.ToString();

            try
            {
                // Parse the JSON
                JObject? obj = JToken.Parse(json) as JObject;
                if (obj == null)
                {
                    Console.WriteLine("  Failed to parse JSON as JObject");
                    return;
                }

                // Measure time to look up properties
                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Look up a random sample of properties
                int sampleSize = Math.Min(10000, count);
                for (int i = 0; i < sampleSize; i++)
                {
                    int propIndex = _random.Next(count);
                    string propName = $"prop{propIndex}";
                    JToken? value = obj[propName];
                }

                sw.Stop();
                double elapsedMs = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine($"  Property lookup time: {elapsedMs:F2}ms for {sampleSize} lookups");

                // If processing took more than 1000ms (1 second), save it as a DOS vector
                if (elapsedMs > 1000)
                {
                    string fileName = $"custom_dos_{DateTime.Now:yyyyMMdd_HHmmss}_property_lookup_{count}_{elapsedMs:F2}.json";
                    string filePath = Path.Combine(_outputDir, fileName);
                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"DOS Vector detected: {fileName}");
                    Console.WriteLine($"  Score: {elapsedMs:F2}");
                    Console.WriteLine($"  Time: {elapsedMs:F2}ms for {json.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing property lookup: {ex.Message}");
            }
        }

        private void TestLargeArrayWithNestedObjects(int count, int depth)
        {
            Console.WriteLine($"Testing large array with {count} nested objects of depth {depth}...");

            StringBuilder sb = new StringBuilder();
            sb.Append('[');

            // Generate a large array of nested objects
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');

                // Create a nested object
                StringBuilder objBuilder = new StringBuilder();
                for (int d = 0; d < depth; d++)
                {
                    objBuilder.Append('{');
                    objBuilder.Append($"\"level{d}\":");
                }

                // Add a value at the deepest level
                objBuilder.Append($"\"{i}\"");

                // Close all objects
                for (int d = 0; d < depth; d++)
                {
                    objBuilder.Append('}');
                }

                sb.Append(objBuilder.ToString());
            }

            sb.Append(']');

            string json = sb.ToString();
            MeasureParsingTime(json, $"large_array_nested_objects_{count}_{depth}");
        }

        private void TestDeepPathWithManyProperties(int depth, int propertiesPerLevel)
        {
            Console.WriteLine($"Testing deep path with {propertiesPerLevel} properties per level (depth: {depth})...");

            // Create a deeply nested object with many properties at each level
            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            // Function to generate a level with many properties
            Action<StringBuilder, int, int>? generateLevel = null;
            generateLevel = (builder, currentDepth, propsPerLevel) =>
            {
                if (currentDepth <= 0) return;

                // Add many properties at this level
                for (int i = 0; i < propsPerLevel; i++)
                {
                    if (i > 0) builder.Append(',');

                    if (i == propsPerLevel - 1 && currentDepth > 1)
                    {
                        // Last property contains the next level
                        builder.Append($"\"nextLevel\":");
                        builder.Append('{');
                        generateLevel?.Invoke(builder, currentDepth - 1, propsPerLevel);
                        builder.Append('}');
                    }
                    else
                    {
                        // Regular property
                        builder.Append($"\"prop{i}\":{i}");
                    }
                }
            };

            // Generate the structure
            generateLevel?.Invoke(sb, depth, propertiesPerLevel);

            sb.Append('}');

            string json = sb.ToString();

            try
            {
                // Parse the JSON
                JToken? token = JToken.Parse(json);

                // Add null check before using token
                if (token == null)
                {
                    Console.WriteLine("Failed to parse JSON (null token)");
                    return;
                }

                // Measure time to navigate to the deepest level
                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Navigate to the deepest level
                JToken? current = token;
                for (int i = 0; i < depth - 1; i++)
                {
                    if (current is JObject obj && obj.ContainsProperty("nextLevel"))
                    {
                        current = obj["nextLevel"];
                    }
                    else
                    {
                        break;
                    }
                }

                sw.Stop();
                double elapsedMs = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine($"  Deep path navigation time: {elapsedMs:F2}ms");

                // If processing took more than 1000ms (1 second), save it as a DOS vector
                if (elapsedMs > 1000)
                {
                    string fileName = $"custom_dos_{DateTime.Now:yyyyMMdd_HHmmss}_deep_path_many_props_{depth}_{propertiesPerLevel}_{elapsedMs:F2}.json";
                    string filePath = Path.Combine(_outputDir, fileName);
                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"DOS Vector detected: {fileName}");
                    Console.WriteLine($"  Score: {elapsedMs:F2}");
                    Console.WriteLine($"  Time: {elapsedMs:F2}ms for {json.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing deep path with many properties: {ex.Message}");
            }
        }

        private void TestObjectWithEscapedPropertyNames(int count)
        {
            Console.WriteLine($"Testing object with {count} escaped property names...");

            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(',');

                // Create property names with escaped characters
                sb.Append("\"prop");
                for (int j = 0; j < 5; j++)
                {
                    switch (j % 5)
                    {
                        case 0: sb.Append("\\\""); break; // Escaped quote
                        case 1: sb.Append("\\\\"); break; // Escaped backslash
                        case 2: sb.Append("\\n"); break; // Escaped new line
                        case 3: sb.Append("\\t"); break; // Escaped tab
                        case 4: sb.Append("\\r"); break; // Escaped carriage return
                    }
                }
                sb.Append(i);
                sb.Append("\":");

                // Simple value
                sb.Append(i);
            }

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"escaped_property_names_{count}");
        }

        private void TestNestedObjectsWithIdenticalStructure(int depth)
        {
            Console.WriteLine($"Testing nested objects with identical structure (depth: {depth})...");

            StringBuilder sb = new StringBuilder();

            // Function to generate a nested object structure
            Action<StringBuilder, int> generateNestedObject = null;
            generateNestedObject = (builder, currentDepth) =>
            {
                builder.Append('{');
                builder.Append("\"id\":");
                builder.Append(currentDepth);

                if (currentDepth > 0)
                {
                    builder.Append(",\"child\":");
                    generateNestedObject?.Invoke(builder, currentDepth - 1);
                }

                builder.Append('}');
            };

            // Generate the nested structure
            generateNestedObject?.Invoke(sb, depth);

            string json = sb.ToString();
            MeasureParsingTime(json, $"nested_identical_structure_{depth}");
        }

        private void TestJsonWithComments(int length)
        {
            Console.WriteLine($"Testing JSON with comments (length: {length})...");

            StringBuilder sb = new StringBuilder();
            sb.Append('{');

            // Add a property with a large value
            sb.Append("\"value\":");

            // Add a large number
            sb.Append(length);

            // Add a comment-like string (not actual JSON comments, as they're not standard)
            sb.Append(",\"comment\":\"");
            for (int i = 0; i < length; i++)
            {
                if (i % 100 == 0)
                {
                    sb.Append("/* This is a comment-like string */");
                }
                else if (i % 50 == 0)
                {
                    sb.Append("// This is a comment-like string");
                }
                else
                {
                    sb.Append('.');
                }
            }
            sb.Append("\"");

            sb.Append('}');

            string json = sb.ToString();
            MeasureParsingTime(json, $"json_with_comments_{length}");
        }

        private void MeasureParsingTime(string json, string testName)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                // Parse the JSON
                JToken? token = JToken.Parse(json);

                // Add null check before using token
                if (token == null)
                {
                    sw.Stop();
                    Console.WriteLine("Failed to parse JSON (null token)");
                    return;
                }

                sw.Stop();
                double elapsedMs = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine($"  Parsing time: {elapsedMs:F2}ms");

                // If parsing took more than 1000ms (1 second), save it as a DOS vector
                if (elapsedMs > 1000)
                {
                    string fileName = $"custom_dos_{DateTime.Now:yyyyMMdd_HHmmss}_{testName}_{elapsedMs:F2}.json";
                    string filePath = Path.Combine(_outputDir, fileName);
                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"DOS Vector detected: {fileName}");
                    Console.WriteLine($"  Score: {elapsedMs:F2}");
                    Console.WriteLine($"  Time: {elapsedMs:F2}ms for {json.Length} chars");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing input: {ex.Message}");
            }
        }
    }
}
