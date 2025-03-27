using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Neo.Json;

namespace Neo.Json.Fuzzer.Tests
{
    /// <summary>
    /// Specialized tests for potential DOS vectors identified during fuzzing
    /// </summary>
    public class DOSVectorTests
    {
        private readonly string _outputDirectory;
        private readonly bool _verbose;
        private readonly double _dosThreshold;
        private readonly bool _trackMemory;

        public DOSVectorTests(string outputDirectory, bool verbose = false, double dosThreshold = 0.7, bool trackMemory = true)
        {
            _outputDirectory = outputDirectory;
            _verbose = verbose;
            _dosThreshold = dosThreshold;
            _trackMemory = trackMemory;
        }

        /// <summary>
        /// Run all DOS vector tests
        /// </summary>
        public void RunAllTests()
        {
            Console.WriteLine("Running DOS Vector Tests...");
            
            // Create the DOS vectors directory if it doesn't exist
            string dosVectorsDir = Path.Combine(_outputDirectory, "targeted-dos-vectors");
            Directory.CreateDirectory(dosVectorsDir);
            
            // Run tests for minimal inputs
            TestMinimalInputs(dosVectorsDir);
            
            // Run tests for deeply nested structures
            TestDeeplyNestedStructures(dosVectorsDir);
            
            // Run tests for alternating types
            TestAlternatingTypes(dosVectorsDir);
            
            // Run tests for repeated patterns
            TestRepeatedPatterns(dosVectorsDir);
            
            // Run tests for exact nesting limits
            TestExactNestingLimits(dosVectorsDir);
            
            Console.WriteLine("DOS Vector Tests completed.");
        }

        /// <summary>
        /// Test minimal inputs that triggered high processing time
        /// </summary>
        private void TestMinimalInputs(string outputDir)
        {
            string[] minimalInputs = new string[]
            {
                "0",                    // Single digit
                "{}",                   // Empty object
                "[]",                   // Empty array
                "[0]",                  // Array with single digit
                "[3]",                  // Array with single digit (identified as DOS vector)
                "[-1]",                 // Negative number
                "[1.7976931348623157E+308]", // Max double value
                "[2,1,3]",              // Small array with multiple values
                "[\"\"]",               // Array with empty string
                "[\"\u0000\"]",         // Array with null character
                "[\"\u0001\"]"          // Array with control character
            };
            
            foreach (var input in minimalInputs)
            {
                TestInput(input, "minimal", outputDir);
            }
        }

        /// <summary>
        /// Test deeply nested structures that triggered high processing time
        /// </summary>
        private void TestDeeplyNestedStructures(string outputDir)
        {
            // Test with different nesting levels
            for (int i = 5; i <= 70; i += 5)
            {
                string nestedJson = CreateNestedStructure(i);
                TestInput(nestedJson, $"nested_{i}", outputDir);
            }
        }

        /// <summary>
        /// Test structures with alternating types that triggered high processing time
        /// </summary>
        private void TestAlternatingTypes(string outputDir)
        {
            string[] alternatingInputs = new string[]
            {
                "{\"string\":\"test\",\"number\":123,\"boolean\":true,\"null\":null}",
                "[\"string\",123,true,null,{},[],-1]",
                "{\"array\":[1,\"a\",true,null,{\"nested\":true}]}",
                "[{\"a\":1},{\"b\":\"2\"},{\"c\":true},{\"d\":null}]",
                CreateAlternatingTypesJson(3),
                CreateAlternatingTypesJson(5),
                CreateAlternatingTypesJson(10)
            };
            
            foreach (var input in alternatingInputs)
            {
                TestInput(input, "alternating", outputDir);
            }
        }

        /// <summary>
        /// Test structures with repeated patterns that triggered high processing time
        /// </summary>
        private void TestRepeatedPatterns(string outputDir)
        {
            string[] repeatedInputs = new string[]
            {
                "{\"a\":{\"a\":{\"a\":{\"a\":1}}}}",
                "[[[[[1]]]]]",
                "{\"a\":[{\"b\":[{\"c\":[1]}]}]}",
                CreateRepeatedPatternJson(5, 3),
                CreateRepeatedPatternJson(10, 2),
                CreateRepeatedPatternJson(15, 2)
            };
            
            foreach (var input in repeatedInputs)
            {
                TestInput(input, "repeated", outputDir);
            }
        }

        /// <summary>
        /// Test structures that are exactly at the nesting limits
        /// </summary>
        private void TestExactNestingLimits(string outputDir)
        {
            int[] nestingLimits = new int[] { 10, 64, 128 };
            
            foreach (var limit in nestingLimits)
            {
                string exactLimitJson = CreateExactLimitJson(limit);
                TestInput(exactLimitJson, $"exact_limit_{limit}", outputDir);
                
                // Also test one level above the limit
                string aboveLimitJson = CreateExactLimitJson(limit + 1);
                TestInput(aboveLimitJson, $"above_limit_{limit}", outputDir);
            }
        }

        /// <summary>
        /// Test a single input and record results if it's a DOS vector
        /// </summary>
        private void TestInput(string input, string category, string outputDir)
        {
            try
            {
                // Measure execution time
                Stopwatch sw = new Stopwatch();
                long memoryBefore = 0;
                long memoryAfter = 0;
                
                if (_trackMemory)
                {
                    GC.Collect();
                    memoryBefore = GC.GetTotalMemory(true);
                }
                
                sw.Start();
                JToken? result = JToken.Parse(input);
                sw.Stop();
                
                if (_trackMemory)
                {
                    memoryAfter = GC.GetTotalMemory(false);
                }
                
                // Calculate metrics
                double executionTimeMs = sw.Elapsed.TotalMilliseconds;
                int inputLength = input.Length;
                int nestingDepth = result != null ? CalculateNestingDepth(result) : 0;
                long memoryUsageBytes = memoryAfter - memoryBefore;
                
                // Calculate DOS score
                double timePerCharRatio = inputLength > 0 ? executionTimeMs / inputLength : 0;
                double timePerCharThreshold = 0.01; // 0.01ms per character
                
                double memoryPerCharRatio = inputLength > 0 ? memoryUsageBytes / inputLength : 0;
                double memoryPerCharThreshold = 100; // 100 bytes per character
                
                double timeScore = timePerCharRatio / timePerCharThreshold;
                double memoryScore = _trackMemory ? memoryPerCharRatio / memoryPerCharThreshold : 0;
                
                double dosScore = Math.Max(timeScore, memoryScore);
                
                // If DOS score exceeds threshold, save as a DOS vector
                if (dosScore >= _dosThreshold)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string hash = Math.Abs(input.GetHashCode()).ToString("X8");
                    string filename = $"targeted_dos_{timestamp}_{hash}_{category}_{dosScore:F2}";
                    
                    // Save the input
                    File.WriteAllText(Path.Combine(outputDir, $"{filename}.json"), input);
                    
                    // Save the analysis
                    StringBuilder analysis = new StringBuilder();
                    analysis.AppendLine($"DOS Score: {dosScore:F2}");
                    
                    if (timeScore >= _dosThreshold)
                    {
                        analysis.AppendLine($"Detection Reason: High time per character ratio: {timePerCharRatio:F4}ms (threshold: {timePerCharThreshold:F4}ms)");
                    }
                    else if (memoryScore >= _dosThreshold)
                    {
                        analysis.AppendLine($"Detection Reason: High memory per character ratio: {memoryPerCharRatio:F1} bytes (threshold: {memoryPerCharThreshold:F1} bytes)");
                    }
                    
                    analysis.AppendLine();
                    analysis.AppendLine("Metrics:");
                    analysis.AppendLine($"  ExecutionTimeMs: {executionTimeMs:F4}");
                    analysis.AppendLine($"  InputLength: {inputLength}");
                    
                    if (_trackMemory)
                    {
                        analysis.AppendLine($"  MemoryUsageBytes: {memoryUsageBytes}");
                    }
                    
                    analysis.AppendLine($"  NestingDepth: {nestingDepth}");
                    
                    File.WriteAllText(Path.Combine(outputDir, $"{filename}.analysis.txt"), analysis.ToString());
                    
                    if (_verbose)
                    {
                        Console.WriteLine($"DOS Vector detected: {filename}");
                        Console.WriteLine($"  Score: {dosScore:F2}");
                        Console.WriteLine($"  Time: {executionTimeMs:F4}ms for {inputLength} chars");
                        if (_trackMemory)
                        {
                            Console.WriteLine($"  Memory: {memoryUsageBytes} bytes");
                        }
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                if (_verbose)
                {
                    Console.WriteLine($"Error testing input: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Calculate the nesting depth of a JToken
        /// </summary>
        private int CalculateNestingDepth(JToken? token)
        {
            if (token == null)
            {
                return 0;
            }
            
            if (token is JObject obj)
            {
                int maxDepth = 0;
                foreach (var property in obj.Properties)
                {
                    int depth = CalculateNestingDepth(property.Value);
                    maxDepth = Math.Max(maxDepth, depth);
                }
                return maxDepth + 1;
            }
            else if (token is JArray array)
            {
                int maxDepth = 0;
                foreach (var item in array)
                {
                    int depth = CalculateNestingDepth(item);
                    maxDepth = Math.Max(maxDepth, depth);
                }
                return maxDepth + 1;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Create a deeply nested structure with the specified nesting level
        /// </summary>
        private string CreateNestedStructure(int nestingLevel)
        {
            StringBuilder sb = new StringBuilder();
            
            // Start with an object
            sb.Append("{\"nested\":");
            
            // Create nesting
            for (int i = 0; i < nestingLevel; i++)
            {
                if (i % 2 == 0)
                {
                    sb.Append("{\"level\":");
                }
                else
                {
                    sb.Append("[");
                }
            }
            
            // Add a value at the deepest level
            sb.Append("0");
            
            // Close the nesting
            for (int i = nestingLevel - 1; i >= 0; i--)
            {
                if (i % 2 == 0)
                {
                    sb.Append("}");
                }
                else
                {
                    sb.Append("]");
                }
            }
            
            // Close the root object
            sb.Append("}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Create a structure with alternating types
        /// </summary>
        private string CreateAlternatingTypesJson(int depth)
        {
            StringBuilder sb = new StringBuilder();
            
            // Start with an object
            sb.Append("{");
            
            for (int i = 0; i < depth; i++)
            {
                string key = $"level{i}";
                
                if (i > 0)
                {
                    sb.Append(",");
                }
                
                switch (i % 4)
                {
                    case 0:
                        sb.Append($"\"{key}\":{i}");
                        break;
                    case 1:
                        sb.Append($"\"{key}\":\"value{i}\"");
                        break;
                    case 2:
                        sb.Append($"\"{key}\":{(i % 2 == 0 ? "true" : "false")}");
                        break;
                    case 3:
                        sb.Append($"\"{key}\":null");
                        break;
                }
            }
            
            // Add a nested structure
            sb.Append(",\"nested\":[");
            
            for (int i = 0; i < depth; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                
                switch (i % 4)
                {
                    case 0:
                        sb.Append(i);
                        break;
                    case 1:
                        sb.Append($"\"value{i}\"");
                        break;
                    case 2:
                        sb.Append(i % 2 == 0 ? "true" : "false");
                        break;
                    case 3:
                        sb.Append("null");
                        break;
                }
            }
            
            sb.Append("]}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Create a structure with repeated patterns
        /// </summary>
        private string CreateRepeatedPatternJson(int depth, int repetitions)
        {
            StringBuilder sb = new StringBuilder();
            
            // Create a pattern
            string pattern = CreatePattern(depth);
            
            // Start with an object
            sb.Append("{");
            
            // Add the pattern multiple times
            for (int i = 0; i < repetitions; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                
                sb.Append($"\"pattern{i}\":{pattern}");
            }
            
            // Close the object
            sb.Append("}");
            
            return sb.ToString();
        }

        /// <summary>
        /// Create a pattern for repetition
        /// </summary>
        private string CreatePattern(int depth)
        {
            if (depth <= 0)
            {
                return "0";
            }
            
            if (depth % 3 == 0)
            {
                return $"{{\"id\":{depth},\"value\":\"test\",\"nested\":{CreatePattern(depth - 1)}}}";
            }
            else if (depth % 3 == 1)
            {
                string element = CreatePattern(depth - 1);
                return $"[{element},{element},{element}]";
            }
            else
            {
                return $"{{\"0\":{CreatePattern(depth - 1)},\"1\":{CreatePattern(depth - 1)}}}";
            }
        }

        /// <summary>
        /// Create a structure that tests exactly at a nesting limit
        /// </summary>
        private string CreateExactLimitJson(int limit)
        {
            StringBuilder sb = new StringBuilder();
            
            // Start with an object
            sb.Append($"{{\"maxDepth\":{limit},\"structure\":");
            
            // Create the nested structure
            JToken? current = null;
            
            for (int i = 0; i < limit; i++)
            {
                if (i == 0)
                {
                    // Start with an object at the root
                    current = new JObject();
                }
                
                string levelName = $"level{limit - i}";
                
                if (current is JObject obj)
                {
                    // Create a new array and add it to the object
                    JArray newArray = new JArray();
                    obj[levelName] = newArray;
                    current = newArray;
                }
                else if (current is JArray arr)
                {
                    // Create a new object and add it to the array
                    JObject newObj = new JObject();
                    arr.Add(newObj);
                    current = newObj;
                }
            }
            
            // Add a value at the deepest level
            if (current is JObject finalObj)
            {
                finalObj["value"] = new JNumber(0);
            }
            else if (current is JArray finalArr)
            {
                finalArr.Add(new JNumber(0));
            }
            
            // Append the structure to the string
            if (current != null)
            {
                sb.Append(current.ToString());
            }
            
            // Close the root object
            sb.Append("}");
            
            return sb.ToString();
        }
    }
}
