// Copyright (C) 2015-2025 The Neo Project.
//
// JsonRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json.Fuzzer.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Json.Fuzzer.Runners
{
    /// <summary>
    /// Executes JSON parsing operations and tracks execution results
    /// </summary>
    public class JsonRunner
    {
        private readonly int _timeoutMs;
        private readonly bool _detectDOS;
        private readonly double _dosThreshold;
        private readonly bool _trackMemory;
        private readonly DOSDetector? _dosDetector;

        /// <summary>
        /// Creates a new JSON runner
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds for parsing operations</param>
        /// <param name="detectDOS">Whether to detect potential DOS vectors</param>
        /// <param name="dosThreshold">Threshold for flagging potential DOS vectors (0.0-1.0)</param>
        /// <param name="trackMemory">Whether to track memory usage</param>
        public JsonRunner(int timeoutMs = 5000, bool detectDOS = false, double dosThreshold = 0.8, bool trackMemory = false)
        {
            _timeoutMs = timeoutMs;
            _detectDOS = detectDOS;
            _dosThreshold = dosThreshold;
            _trackMemory = trackMemory;

            if (_detectDOS)
            {
                _dosDetector = new DOSDetector(_dosThreshold, _trackMemory);
            }
        }

        /// <summary>
        /// Executes a JSON parsing operation and returns the execution result
        /// </summary>
        /// <param name="json">The JSON string to parse</param>
        /// <returns>An execution result with details about the parsing operation</returns>
        public JsonExecutionResult Execute(string json)
        {
            var result = new JsonExecutionResult();

            if (_detectDOS)
            {
                _dosDetector?.Reset();
            }

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(_timeoutMs);

            var stopwatch = Stopwatch.StartNew();
            long startMemory = GC.GetTotalMemory(false);

            try
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        // Test parsing with different max_nest values
                        TestParsingWithMaxNest(json, result);

                        // Test JPath operations if parsing succeeded
                        if (result.ParsedToken != null)
                        {
                            TestJPathOperations(result.ParsedToken, result);
                        }

                        // Test type conversions if parsing succeeded
                        if (result.ParsedToken != null)
                        {
                            TestTypeConversions(result.ParsedToken, result);
                        }

                        // Test serialization if parsing succeeded
                        if (result.ParsedToken != null)
                        {
                            TestSerialization(result.ParsedToken, result);
                        }

                        result.Success = true;
                    }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                        result.ExceptionType = ex.GetType().Name;
                        result.ExceptionMessage = ex.Message;
                        result.Crashed = true;
                        result.Success = false;
                    }
                }, cts.Token);

                if (!task.Wait(_timeoutMs))
                {
                    result.TimedOut = true;
                    result.Success = false;
                }
            }
            catch (OperationCanceledException)
            {
                result.TimedOut = true;
                result.Success = false;
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                result.ExceptionType = ex.GetType().Name;
                result.ExceptionMessage = ex.Message;
                result.Crashed = true;
                result.Success = false;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

                // Calculate memory usage
                long endMemory = GC.GetTotalMemory(false);
                result.MemoryUsageBytes = endMemory - startMemory;

                // Perform DOS analysis if enabled
                if (_detectDOS && _dosDetector != null)
                {
                    var metrics = new Dictionary<string, double>
                    {
                        ["ExecutionTimeMs"] = result.ExecutionTimeMs,
                        ["MemoryUsageBytes"] = result.MemoryUsageBytes,
                        ["InputLength"] = json.Length,
                        ["NestingDepth"] = CalculateNestingDepth(json)
                    };

                    result.DOSAnalysis = _dosDetector.Analyze(metrics);
                }

                // Track coverage
                result.Coverage = CollectCoveragePoints(result);
            }

            return result;
        }

        /// <summary>
        /// Tests parsing the JSON with different max_nest values
        /// </summary>
        private void TestParsingWithMaxNest(string json, JsonExecutionResult result)
        {
            // Try with default max_nest (64)
            try
            {
                result.ParsedToken = JToken.Parse(json);
                result.DefaultNestingSucceeded = true;
            }
            catch (Exception ex)
            {
                result.DefaultNestingException = ex.GetType().Name;
            }

            // Try with lower max_nest (10)
            try
            {
                JToken.Parse(json, 10);
                result.LowNestingSucceeded = true;
            }
            catch (Exception ex)
            {
                result.LowNestingException = ex.GetType().Name;
            }

            // Try with higher max_nest (128)
            try
            {
                JToken.Parse(json, 128);
                result.HighNestingSucceeded = true;
            }
            catch (Exception ex)
            {
                result.HighNestingException = ex.GetType().Name;
            }
        }

        /// <summary>
        /// Tests JPath operations on the parsed token
        /// </summary>
        private void TestJPathOperations(JToken token, JsonExecutionResult result)
        {
            // Test simple JPath expressions
            try
            {
                token.JsonPath("$");
                result.JPathRootSucceeded = true;
            }
            catch (Exception ex)
            {
                result.JPathRootException = ex.GetType().Name;
            }

            // Only test more complex JPath if the token is a container
            if (token is JContainer)
            {
                // Test property access if it's an object
                if (token is JObject)
                {
                    try
                    {
                        token.JsonPath("$.*");
                        result.JPathPropertiesSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        result.JPathPropertiesException = ex.GetType().Name;
                    }
                }

                // Test array access if it's an array
                if (token is JArray)
                {
                    try
                    {
                        token.JsonPath("$[*]");
                        result.JPathArraySucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        result.JPathArrayException = ex.GetType().Name;
                    }
                }
            }
        }

        /// <summary>
        /// Tests type conversions on the parsed token
        /// </summary>
        private void TestTypeConversions(JToken token, JsonExecutionResult result)
        {
            // Test AsBoolean
            try
            {
                token.AsBoolean();
                result.AsBooleanSucceeded = true;
            }
            catch (Exception ex)
            {
                result.AsBooleanException = ex.GetType().Name;
            }

            // Test AsNumber
            try
            {
                token.AsNumber();
                result.AsNumberSucceeded = true;
            }
            catch (Exception ex)
            {
                result.AsNumberException = ex.GetType().Name;
            }

            // Test AsString
            try
            {
                token.AsString();
                result.AsStringSucceeded = true;
            }
            catch (Exception ex)
            {
                result.AsStringException = ex.GetType().Name;
            }

            // Test GetBoolean
            try
            {
                token.GetBoolean();
                result.GetBooleanSucceeded = true;
            }
            catch (Exception ex)
            {
                result.GetBooleanException = ex.GetType().Name;
            }

            // Test GetNumber
            try
            {
                token.GetNumber();
                result.GetNumberSucceeded = true;
            }
            catch (Exception ex)
            {
                result.GetNumberException = ex.GetType().Name;
            }

            // Test GetString
            try
            {
                token.GetString();
                result.GetStringSucceeded = true;
            }
            catch (Exception ex)
            {
                result.GetStringException = ex.GetType().Name;
            }
        }

        /// <summary>
        /// Tests serialization of the parsed token
        /// </summary>
        private void TestSerialization(JToken token, JsonExecutionResult result)
        {
            // Test ToString()
            try
            {
                token.ToString();
                result.ToStringSucceeded = true;
            }
            catch (Exception ex)
            {
                result.ToStringException = ex.GetType().Name;
            }

            // Test ToString(true) - indented
            try
            {
                token.ToString(true);
                result.ToStringIndentedSucceeded = true;
            }
            catch (Exception ex)
            {
                result.ToStringIndentedException = ex.GetType().Name;
            }

            // Test ToByteArray
            try
            {
                token.ToByteArray(false);
                result.ToByteArraySucceeded = true;
            }
            catch (Exception ex)
            {
                result.ToByteArrayException = ex.GetType().Name;
            }
        }

        /// <summary>
        /// Calculates the nesting depth of a JSON string
        /// </summary>
        private int CalculateNestingDepth(string json)
        {
            int maxDepth = 0;
            int currentDepth = 0;

            foreach (char c in json)
            {
                if (c == '{' || c == '[')
                {
                    currentDepth++;
                    maxDepth = Math.Max(maxDepth, currentDepth);
                }
                else if (c == '}' || c == ']')
                {
                    currentDepth = Math.Max(0, currentDepth - 1);
                }
            }

            return maxDepth;
        }

        /// <summary>
        /// Collects coverage points based on the execution result
        /// </summary>
        private List<string> CollectCoveragePoints(JsonExecutionResult result)
        {
            var coverage = new List<string>();

            // Add coverage points for parsing
            if (result.DefaultNestingSucceeded)
            {
                coverage.Add("Parse:DefaultNesting");
            }
            if (result.LowNestingSucceeded)
            {
                coverage.Add("Parse:LowNesting");
            }
            if (result.HighNestingSucceeded)
            {
                coverage.Add("Parse:HighNesting");
            }

            // Add coverage points for JPath operations
            if (result.JPathRootSucceeded)
            {
                coverage.Add("JPath:Root");
            }
            if (result.JPathPropertiesSucceeded)
            {
                coverage.Add("JPath:Properties");
            }
            if (result.JPathArraySucceeded)
            {
                coverage.Add("JPath:Array");
            }

            // Add coverage points for type conversions
            if (result.AsBooleanSucceeded)
            {
                coverage.Add("Convert:AsBoolean");
            }
            if (result.AsNumberSucceeded)
            {
                coverage.Add("Convert:AsNumber");
            }
            if (result.AsStringSucceeded)
            {
                coverage.Add("Convert:AsString");
            }
            if (result.GetBooleanSucceeded)
            {
                coverage.Add("Convert:GetBoolean");
            }
            if (result.GetNumberSucceeded)
            {
                coverage.Add("Convert:GetNumber");
            }
            if (result.GetStringSucceeded)
            {
                coverage.Add("Convert:GetString");
            }

            // Add coverage points for serialization
            if (result.ToStringSucceeded)
            {
                coverage.Add("Serialize:ToString");
            }
            if (result.ToStringIndentedSucceeded)
            {
                coverage.Add("Serialize:ToStringIndented");
            }
            if (result.ToByteArraySucceeded)
            {
                coverage.Add("Serialize:ToByteArray");
            }

            // Add coverage points for token types
            if (result.ParsedToken != null)
            {
                coverage.Add($"TokenType:{result.ParsedToken.GetType().Name}");
            }

            // Add coverage points for exceptions
            if (result.Crashed)
            {
                coverage.Add($"Exception:{result.ExceptionType}");
            }

            return coverage;
        }
    }

    /// <summary>
    /// Result of executing a JSON parsing operation
    /// </summary>
    public class JsonExecutionResult
    {
        // Basic execution information
        public bool Success { get; set; }
        public bool Crashed { get; set; }
        public bool TimedOut { get; set; }
        public double ExecutionTimeMs { get; set; }
        public long MemoryUsageBytes { get; set; }
        public Exception? Exception { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public JToken? ParsedToken { get; set; }
        public List<string> Coverage { get; set; } = new List<string>();
        public DOSAnalysisResult? DOSAnalysis { get; set; }

        // Parsing with different max_nest values
        public bool DefaultNestingSucceeded { get; set; }
        public string? DefaultNestingException { get; set; }
        public bool LowNestingSucceeded { get; set; }
        public string? LowNestingException { get; set; }
        public bool HighNestingSucceeded { get; set; }
        public string? HighNestingException { get; set; }

        // JPath operations
        public bool JPathRootSucceeded { get; set; }
        public string? JPathRootException { get; set; }
        public bool JPathPropertiesSucceeded { get; set; }
        public string? JPathPropertiesException { get; set; }
        public bool JPathArraySucceeded { get; set; }
        public string? JPathArrayException { get; set; }

        // Type conversions
        public bool AsBooleanSucceeded { get; set; }
        public string? AsBooleanException { get; set; }
        public bool AsNumberSucceeded { get; set; }
        public string? AsNumberException { get; set; }
        public bool AsStringSucceeded { get; set; }
        public string? AsStringException { get; set; }
        public bool GetBooleanSucceeded { get; set; }
        public string? GetBooleanException { get; set; }
        public bool GetNumberSucceeded { get; set; }
        public string? GetNumberException { get; set; }
        public bool GetStringSucceeded { get; set; }
        public string? GetStringException { get; set; }

        // Serialization
        public bool ToStringSucceeded { get; set; }
        public string? ToStringException { get; set; }
        public bool ToStringIndentedSucceeded { get; set; }
        public string? ToStringIndentedException { get; set; }
        public bool ToByteArraySucceeded { get; set; }
        public string? ToByteArrayException { get; set; }
    }
}
