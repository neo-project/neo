// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using CommandLine;
using Neo.Json.Fuzzer.Generators;
using Neo.Json.Fuzzer.Runners;
using Neo.Json.Fuzzer.Tests;
using Neo.Json.Fuzzer.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Json.Fuzzer
{
    /// <summary>
    /// Main entry point for the Neo.Json.Fuzzer
    /// </summary>
    public class Program
    {
        private static readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// Command line options for the fuzzer
        /// </summary>
        public class Options
        {
            [Option('o', "output", Required = false, Default = "./output", HelpText = "Output directory for fuzzing results")]
            public string OutputDir { get; set; } = "./output";

            [Option('c', "corpus", Required = false, HelpText = "Directory containing initial corpus of JSON files")]
            public string? CorpusDir { get; set; }

            [Option('r', "runs", Required = false, Default = 0, HelpText = "Number of fuzzing runs (0 = infinite)")]
            public int Runs { get; set; }

            [Option('t', "timeout", Required = false, Default = 5000, HelpText = "Timeout in milliseconds for each fuzzing run")]
            public int TimeoutMs { get; set; }

            [Option('s', "seed", Required = false, Default = 0, HelpText = "Random seed (0 = use time-based seed)")]
            public int Seed { get; set; }

            [Option('d', "detect-dos", Required = false, Default = false, HelpText = "Enable detection of potential DOS vectors")]
            public bool DetectDOS { get; set; }

            [Option("dos-threshold", Required = false, Default = 0.8, HelpText = "Threshold for DOS detection (0.0-1.0)")]
            public double DOSThreshold { get; set; }

            [Option('m', "track-memory", Required = false, Default = false, HelpText = "Track memory usage during fuzzing")]
            public bool TrackMemory { get; set; }

            [Option('v', "verbose", Required = false, Default = false, HelpText = "Enable verbose output")]
            public bool Verbose { get; set; }

            [Option('p', "mutation-probability", Required = false, Default = 0.7, HelpText = "Probability of mutating existing inputs vs generating new ones")]
            public double MutationProbability { get; set; }

            [Option("min-mutations", Required = false, Default = 1, HelpText = "Minimum number of mutations per input")]
            public int MinMutations { get; set; }

            [Option("max-mutations", Required = false, Default = 5, HelpText = "Maximum number of mutations per input")]
            public int MaxMutations { get; set; }

            [Option("max-depth", Required = false, Default = 10, HelpText = "Maximum depth for generated JSON")]
            public int MaxDepth { get; set; }

            [Option("max-children", Required = false, Default = 10, HelpText = "Maximum number of children per node in generated JSON")]
            public int MaxChildren { get; set; }

            [Option("max-string-length", Required = false, Default = 100, HelpText = "Maximum length of generated strings")]
            public int MaxStringLength { get; set; }

            [Option("threads", Required = false, Default = 1, HelpText = "Number of fuzzing threads")]
            public int Threads { get; set; }

            [Option("targeted-dos-tests", Required = false, Default = false, HelpText = "Run targeted DOS vector tests")]
            public bool TargetedDOSTests { get; set; }

            [Option("mutation-engine-tests", Required = false, Default = false, HelpText = "Run MutationEngine refactoring tests")]
            public bool MutationEngineTests { get; set; }

            [Option("custom-dos-tests", Required = false, Default = false, HelpText = "Run custom DOS tests to find vectors taking over 1 second")]
            public bool CustomDOSTests { get; set; }

            [Option("array-nesting-benchmark", Required = false, Default = false, HelpText = "Run detailed benchmark tests on array size and nesting depth combinations")]
            public bool ArrayNestingBenchmark { get; set; }

            [Option("object-structure-benchmark", Required = false, Default = false, HelpText = "Run detailed benchmark tests on different object structures and their performance impact")]
            public bool ObjectStructureBenchmark { get; set; }

            // New specialized testing options
            [Option("jpath-tests", Required = false, Default = false, HelpText = "Run specialized JPath query testing")]
            public bool JPathTests { get; set; }

            [Option("unicode-tests", Required = false, Default = false, HelpText = "Run specialized Unicode handling tests")]
            public bool UnicodeTests { get; set; }

            [Option("numeric-precision-tests", Required = false, Default = false, HelpText = "Run specialized numeric precision tests")]
            public bool NumericPrecisionTests { get; set; }

            [Option("streaming-tests", Required = false, Default = false, HelpText = "Run specialized streaming JSON tests")]
            public bool StreamingTests { get; set; }

            [Option("concurrent-access-tests", Required = false, Default = false, HelpText = "Run specialized concurrent access tests")]
            public bool ConcurrentAccessTests { get; set; }

            [Option("specialized-test-type", Required = false, HelpText = "Specific type of specialized test to run (e.g., 'jpath_filter', 'unicode_bmp', etc.)")]
            public string? SpecializedTestType { get; set; }

            [Option("specialized-test-count", Required = false, Default = 100, HelpText = "Number of specialized test cases to generate")]
            public int SpecializedTestCount { get; set; }
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("Neo.Json.Fuzzer");
            Console.WriteLine("Copyright (C) 2015-2025 The Neo Project");
            Console.WriteLine();

            // Parse command line options
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunFuzzer)
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Invalid command line options.");
                });
        }

        /// <summary>
        /// Runs the fuzzer with the specified options
        /// </summary>
        private static void RunFuzzer(Options options)
        {
            try
            {
                // Initialize random seed
                int seed = options.Seed != 0 ? options.Seed : Environment.TickCount;
                Console.WriteLine($"Using random seed: {seed}");
                Random random = new(seed);

                // Create output directory
                Directory.CreateDirectory(options.OutputDir);

                // Run targeted DOS vector tests if requested
                if (options.TargetedDOSTests)
                {
                    Console.WriteLine("Running targeted DOS vector tests...");
                    var dosVectorTests = new DOSVectorTests(
                        options.OutputDir,
                        options.Verbose,
                        options.DOSThreshold,
                        options.TrackMemory);

                    dosVectorTests.RunAllTests();

                    // If we're only running DOS tests, exit
                    if (options.Runs == 0 && !options.CustomDOSTests && !options.ArrayNestingBenchmark && !options.ObjectStructureBenchmark &&
                        !options.JPathTests && !options.UnicodeTests && !options.NumericPrecisionTests && !options.StreamingTests && !options.ConcurrentAccessTests)
                    {
                        Console.WriteLine("Targeted DOS vector tests completed successfully");
                        return;
                    }

                    Console.WriteLine("Continuing with next tests...");
                }

                // Run custom DOS tests if requested
                if (options.CustomDOSTests)
                {
                    Console.WriteLine("Running custom DOS tests to find vectors taking over 1 second...");
                    var customDOSTest = new CustomDOSTest(options.OutputDir, seed);
                    customDOSTest.RunTests();

                    // If we're only running DOS tests, exit
                    if (options.Runs == 0 && !options.ArrayNestingBenchmark && !options.ObjectStructureBenchmark &&
                        !options.JPathTests && !options.UnicodeTests && !options.NumericPrecisionTests && !options.StreamingTests && !options.ConcurrentAccessTests)
                    {
                        Console.WriteLine("Custom DOS tests completed successfully");
                        return;
                    }

                    Console.WriteLine("Continuing with next tests...");
                }

                // Run array nesting benchmark if requested
                if (options.ArrayNestingBenchmark)
                {
                    Console.WriteLine("Running array nesting benchmark tests...");
                    var arrayNestingBenchmark = new ArrayNestingBenchmark(options.OutputDir, random);
                    arrayNestingBenchmark.RunBenchmarks();

                    // If we're only running benchmark tests, exit
                    if (options.Runs == 0 && !options.ObjectStructureBenchmark &&
                        !options.JPathTests && !options.UnicodeTests && !options.NumericPrecisionTests && !options.StreamingTests && !options.ConcurrentAccessTests)
                    {
                        Console.WriteLine("Array nesting benchmark tests completed successfully");
                        return;
                    }

                    Console.WriteLine("Continuing with next tests...");
                }

                // Run object structure benchmark if requested
                if (options.ObjectStructureBenchmark)
                {
                    Console.WriteLine("Running object structure benchmark tests...");
                    var objectStructureBenchmark = new ObjectStructureBenchmark(options.OutputDir, random);
                    objectStructureBenchmark.RunBenchmarks();

                    // If we're only running benchmark tests, exit
                    if (options.Runs == 0 && !options.JPathTests && !options.UnicodeTests && !options.NumericPrecisionTests && !options.StreamingTests && !options.ConcurrentAccessTests)
                    {
                        Console.WriteLine("Object structure benchmark tests completed successfully");
                        return;
                    }

                    Console.WriteLine("Continuing with next tests...");
                }

                // Run MutationEngine refactoring tests if requested
                if (options.MutationEngineTests)
                {
                    Console.WriteLine("Running MutationEngine refactoring tests...");
                    TestRunner.RunTests();
                    return;
                }

                // Initialize components
                var corpusManager = new CorpusManager(options.OutputDir, options.CorpusDir);
                var coverageTracker = new CoverageTracker(options.OutputDir, options.Verbose);
                var statistics = new FuzzingStatistics(options.OutputDir, options.Verbose);
                var jsonGenerator = new JsonGenerator(
                    random,
                    options.MaxDepth,
                    options.MaxChildren,
                    options.MaxStringLength);
                var mutationEngine = new MutationEngine(
                    random,
                    options.MinMutations,
                    options.MaxMutations);

                // Run specialized tests if requested
                if (RunSpecializedTests(options, mutationEngine, corpusManager, coverageTracker, statistics, random))
                {
                    // If we're only running specialized tests, exit
                    if (options.Runs == 0)
                    {
                        Console.WriteLine("Specialized tests completed successfully");
                        return;
                    }

                    Console.WriteLine("Continuing with regular fuzzing...");
                }

                // Load corpus
                Console.WriteLine("Loading corpus...");
                corpusManager.LoadCorpus();
                Console.WriteLine($"Loaded {corpusManager.CorpusSize} items into corpus");

                // Register Ctrl+C handler
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Stopping fuzzer...");
                    _cts.Cancel();
                    e.Cancel = true;
                };

                // Start fuzzing
                Console.WriteLine("Starting fuzzing...");
                Console.WriteLine($"Output directory: {Path.GetFullPath(options.OutputDir)}");
                Console.WriteLine($"Runs: {(options.Runs == 0 ? "infinite" : options.Runs.ToString())}");
                Console.WriteLine($"Timeout: {options.TimeoutMs}ms");
                Console.WriteLine($"Threads: {options.Threads}");
                Console.WriteLine();

                // Create tasks for each thread
                var tasks = new Task[options.Threads];
                for (int i = 0; i < options.Threads; i++)
                {
                    int threadId = i;
                    tasks[i] = Task.Run(() => FuzzingLoop(
                        threadId,
                        options,
                        random,
                        corpusManager,
                        coverageTracker,
                        statistics,
                        jsonGenerator,
                        mutationEngine));
                }

                // Wait for all tasks to complete
                Task.WaitAll(tasks);

                // Print final statistics
                statistics.PrintProgressReport();
                coverageTracker.PrintSummary();

                // Save reports
                Console.WriteLine("Saving reports...");
                statistics.SaveStatistics();
                coverageTracker.SaveCoverageReport();

                Console.WriteLine("Fuzzing completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Runs specialized tests based on command line options
        /// </summary>
        /// <returns>True if any specialized tests were run</returns>
        private static bool RunSpecializedTests(
            Options options,
            MutationEngine mutationEngine,
            CorpusManager corpusManager,
            CoverageTracker coverageTracker,
            FuzzingStatistics statistics,
            Random random)
        {
            bool ranTests = false;

            // Create specialized test output directories
            string specializedTestsDir = Path.Combine(options.OutputDir, "specialized_tests");
            Directory.CreateDirectory(specializedTestsDir);

            // Create JSON runner for testing
            var jsonRunner = new JsonRunner(
                options.TimeoutMs,
                options.DetectDOS,
                options.DOSThreshold,
                options.TrackMemory);

            // Run JPath tests if requested
            if (options.JPathTests)
            {
                ranTests = true;
                string jpathTestsDir = Path.Combine(specializedTestsDir, "jpath_tests");
                Directory.CreateDirectory(jpathTestsDir);

                Console.WriteLine("Running specialized JPath tests...");

                // Determine test types to run
                string[] testTypes = options.SpecializedTestType != null && options.SpecializedTestType.StartsWith("jpath_")
                    ? new[] { options.SpecializedTestType }
                    : new[] { "jpath", "jpath_simple", "jpath_wildcard", "jpath_filter", "jpath_union", "jpath_recursive", "jpath_slice" };

                foreach (string testType in testTypes)
                {
                    string testTypeDir = Path.Combine(jpathTestsDir, testType);
                    Directory.CreateDirectory(testTypeDir);

                    Console.WriteLine($"  Generating {options.SpecializedTestCount} {testType} test cases...");

                    for (int i = 0; i < options.SpecializedTestCount; i++)
                    {
                        // Generate specialized test JSON
                        string json = mutationEngine.GenerateSpecializedTestJson(testType);

                        // Execute test
                        var result = jsonRunner.Execute(json);

                        // Record statistics and coverage
                        statistics.RecordResult(result);
                        bool foundNewCoverage = coverageTracker.RecordCoverage(result.Coverage);

                        // Save test case
                        string filename = Path.Combine(testTypeDir, $"test_{i:D4}.json");
                        File.WriteAllText(filename, json);

                        // Save to corpus if it found new coverage
                        if (foundNewCoverage)
                        {
                            corpusManager.SaveInteresting(json);
                        }

                        // Save crashes and DOS vectors
                        if (result.Crashed)
                        {
                            corpusManager.SaveCrash(json, result.ExceptionType);
                        }

                        if (result.DOSAnalysis?.IsPotentialDOSVector == true)
                        {
                            corpusManager.SaveDOSVector(json, result.DOSAnalysis);
                        }
                    }
                }

                Console.WriteLine("JPath tests completed successfully");
            }

            // Run Unicode tests if requested
            if (options.UnicodeTests)
            {
                ranTests = true;
                string unicodeTestsDir = Path.Combine(specializedTestsDir, "unicode_tests");
                Directory.CreateDirectory(unicodeTestsDir);

                Console.WriteLine("Running specialized Unicode tests...");

                // Determine test types to run
                string[] testTypes = options.SpecializedTestType != null && options.SpecializedTestType.StartsWith("unicode_")
                    ? new[] { options.SpecializedTestType }
                    : new[] { "unicode", "unicode_bmp", "unicode_supplementary" };

                foreach (string testType in testTypes)
                {
                    string testTypeDir = Path.Combine(unicodeTestsDir, testType);
                    Directory.CreateDirectory(testTypeDir);

                    Console.WriteLine($"  Generating {options.SpecializedTestCount} {testType} test cases...");

                    for (int i = 0; i < options.SpecializedTestCount; i++)
                    {
                        // Generate specialized test JSON
                        string unicodeString = mutationEngine.GenerateSpecializedTestJson(testType);
                        string json = $"{{\"value\":\"{unicodeString.Replace("\"", "\\\"")}\",\"type\":\"{testType}\"}}";

                        // Execute test
                        var result = jsonRunner.Execute(json);

                        // Record statistics and coverage
                        statistics.RecordResult(result);
                        bool foundNewCoverage = coverageTracker.RecordCoverage(result.Coverage);

                        // Save test case
                        string filename = Path.Combine(testTypeDir, $"test_{i:D4}.json");
                        File.WriteAllText(filename, json);

                        // Save to corpus if it found new coverage
                        if (foundNewCoverage)
                        {
                            corpusManager.SaveInteresting(json);
                        }

                        // Save crashes and DOS vectors
                        if (result.Crashed)
                        {
                            corpusManager.SaveCrash(json, result.ExceptionType);
                        }

                        if (result.DOSAnalysis?.IsPotentialDOSVector == true)
                        {
                            corpusManager.SaveDOSVector(json, result.DOSAnalysis);
                        }
                    }
                }

                Console.WriteLine("Unicode tests completed successfully");
            }

            // Run numeric precision tests if requested
            if (options.NumericPrecisionTests)
            {
                ranTests = true;
                string numericTestsDir = Path.Combine(specializedTestsDir, "numeric_tests");
                Directory.CreateDirectory(numericTestsDir);

                Console.WriteLine("Running specialized numeric precision tests...");

                // Determine test types to run
                string[] testTypes = options.SpecializedTestType != null && options.SpecializedTestType.StartsWith("numeric_")
                    ? new[] { options.SpecializedTestType }
                    : new[] { "numeric", "numeric_integer", "numeric_float", "numeric_boundary", "numeric_scientific", "numeric_precision" };

                foreach (string testType in testTypes)
                {
                    string testTypeDir = Path.Combine(numericTestsDir, testType);
                    Directory.CreateDirectory(testTypeDir);

                    Console.WriteLine($"  Generating {options.SpecializedTestCount} {testType} test cases...");

                    for (int i = 0; i < options.SpecializedTestCount; i++)
                    {
                        // Generate specialized test JSON
                        string numericValue = mutationEngine.GenerateSpecializedTestJson(testType);
                        string json = $"{{\"value\":{numericValue},\"type\":\"{testType}\"}}";

                        // Execute test
                        var result = jsonRunner.Execute(json);

                        // Record statistics and coverage
                        statistics.RecordResult(result);
                        bool foundNewCoverage = coverageTracker.RecordCoverage(result.Coverage);

                        // Save test case
                        string filename = Path.Combine(testTypeDir, $"test_{i:D4}.json");
                        File.WriteAllText(filename, json);

                        // Save to corpus if it found new coverage
                        if (foundNewCoverage)
                        {
                            corpusManager.SaveInteresting(json);
                        }

                        // Save crashes and DOS vectors
                        if (result.Crashed)
                        {
                            corpusManager.SaveCrash(json, result.ExceptionType);
                        }

                        if (result.DOSAnalysis?.IsPotentialDOSVector == true)
                        {
                            corpusManager.SaveDOSVector(json, result.DOSAnalysis);
                        }
                    }
                }

                Console.WriteLine("Numeric precision tests completed successfully");
            }

            // Run streaming tests if requested
            if (options.StreamingTests)
            {
                ranTests = true;
                string streamingTestsDir = Path.Combine(specializedTestsDir, "streaming_tests");
                Directory.CreateDirectory(streamingTestsDir);

                Console.WriteLine("Running specialized streaming tests...");

                // Determine test types to run
                string[] testTypes = options.SpecializedTestType != null && options.SpecializedTestType.StartsWith("streaming_")
                    ? new[] { options.SpecializedTestType }
                    : new[] { "streaming", "streaming_large_array", "streaming_large_object", "streaming_deep_nesting", "streaming_chunked" };

                foreach (string testType in testTypes)
                {
                    string testTypeDir = Path.Combine(streamingTestsDir, testType);
                    Directory.CreateDirectory(testTypeDir);

                    Console.WriteLine($"  Generating {options.SpecializedTestCount} {testType} test cases...");

                    for (int i = 0; i < options.SpecializedTestCount; i++)
                    {
                        // Generate specialized test JSON
                        string json = mutationEngine.GenerateSpecializedTestJson(testType);

                        // Execute test
                        var result = jsonRunner.Execute(json);

                        // Record statistics and coverage
                        statistics.RecordResult(result);
                        bool foundNewCoverage = coverageTracker.RecordCoverage(result.Coverage);

                        // Save test case
                        string filename = Path.Combine(testTypeDir, $"test_{i:D4}.json");
                        File.WriteAllText(filename, json);

                        // Save to corpus if it found new coverage
                        if (foundNewCoverage)
                        {
                            corpusManager.SaveInteresting(json);
                        }

                        // Save crashes and DOS vectors
                        if (result.Crashed)
                        {
                            corpusManager.SaveCrash(json, result.ExceptionType);
                        }

                        if (result.DOSAnalysis?.IsPotentialDOSVector == true)
                        {
                            corpusManager.SaveDOSVector(json, result.DOSAnalysis);
                        }
                    }
                }

                Console.WriteLine("Streaming tests completed successfully");
            }

            // Run concurrent access tests if requested
            if (options.ConcurrentAccessTests)
            {
                ranTests = true;
                string concurrentTestsDir = Path.Combine(specializedTestsDir, "concurrent_tests");
                Directory.CreateDirectory(concurrentTestsDir);

                Console.WriteLine("Running specialized concurrent access tests...");

                // Determine test types to run
                string[] testTypes = options.SpecializedTestType != null && options.SpecializedTestType.StartsWith("concurrent_")
                    ? new[] { options.SpecializedTestType }
                    : new[] { "concurrent", "concurrent_shared_objects", "concurrent_parallel_operations", "concurrent_race_conditions", "concurrent_thread_safety" };

                foreach (string testType in testTypes)
                {
                    string testTypeDir = Path.Combine(concurrentTestsDir, testType);
                    Directory.CreateDirectory(testTypeDir);

                    Console.WriteLine($"  Generating {options.SpecializedTestCount} {testType} test cases...");

                    for (int i = 0; i < options.SpecializedTestCount; i++)
                    {
                        // Generate specialized test JSON
                        string json = mutationEngine.GenerateSpecializedTestJson(testType);

                        // Execute test
                        var result = jsonRunner.Execute(json);

                        // Record statistics and coverage
                        statistics.RecordResult(result);
                        bool foundNewCoverage = coverageTracker.RecordCoverage(result.Coverage);

                        // Save test case
                        string filename = Path.Combine(testTypeDir, $"test_{i:D4}.json");
                        File.WriteAllText(filename, json);

                        // Save to corpus if it found new coverage
                        if (foundNewCoverage)
                        {
                            corpusManager.SaveInteresting(json);
                        }

                        // Save crashes and DOS vectors
                        if (result.Crashed)
                        {
                            corpusManager.SaveCrash(json, result.ExceptionType);
                        }

                        if (result.DOSAnalysis?.IsPotentialDOSVector == true)
                        {
                            corpusManager.SaveDOSVector(json, result.DOSAnalysis);
                        }
                    }
                }

                Console.WriteLine("Concurrent access tests completed successfully");
            }

            return ranTests;
        }

        /// <summary>
        /// Main fuzzing loop
        /// </summary>
        private static void FuzzingLoop(
            int threadId,
            Options options,
            Random random,
            CorpusManager corpusManager,
            CoverageTracker coverageTracker,
            FuzzingStatistics statistics,
            JsonGenerator jsonGenerator,
            MutationEngine mutationEngine)
        {
            // Create thread-specific components
            var jsonRunner = new JsonRunner(
                options.TimeoutMs,
                options.DetectDOS,
                options.DOSThreshold,
                options.TrackMemory);

            int runs = 0;
            while (!_cts.IsCancellationRequested && (options.Runs == 0 || runs < options.Runs))
            {
                try
                {
                    // Generate or mutate JSON
                    string json;
                    if (corpusManager.CorpusSize == 0 || random.NextDouble() > options.MutationProbability)
                    {
                        // Generate new JSON
                        json = jsonGenerator.GenerateRandomJson();
                    }
                    else
                    {
                        // Mutate existing JSON
                        string baseJson = corpusManager.GetRandomJson();
                        json = mutationEngine.MutateJson(baseJson);
                    }

                    // Execute JSON parsing
                    var result = jsonRunner.Execute(json);

                    // Record statistics
                    statistics.RecordResult(result);

                    // Record coverage
                    bool foundNewCoverage = coverageTracker.RecordCoverage(result.Coverage);

                    // Save interesting cases
                    if (foundNewCoverage)
                    {
                        corpusManager.SaveInteresting(json);
                    }

                    // Save crashes
                    if (result.Crashed)
                    {
                        corpusManager.SaveCrash(json, result.ExceptionType);
                    }

                    // Save DOS vectors
                    if (result.DOSAnalysis?.IsPotentialDOSVector == true)
                    {
                        corpusManager.SaveDOSVector(json, result.DOSAnalysis);
                    }

                    // Update run count
                    runs++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Thread {threadId} error: {ex.Message}");
                }
            }
        }
    }
}
