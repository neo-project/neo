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
using Neo.VM.Fuzzer.Generators;
using Neo.VM.Fuzzer.Runners;
using Neo.VM.Fuzzer.Utils;
using System;
using System.IO;
using System.Threading;

namespace Neo.VM.Fuzzer
{
    /// <summary>
    /// Main entry point for the Neo VM Fuzzer
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Command line options for the fuzzer
        /// </summary>
        public class Options
        {
            [Option('i', "iterations", Default = 1000, HelpText = "Number of fuzzing iterations to run")]
            public int Iterations { get; set; }

            [Option('s', "seed", HelpText = "Random seed for reproducible fuzzing")]
            public int? Seed { get; set; }

            [Option('o', "output", Default = "fuzzer-output", HelpText = "Output directory for crash reports and interesting scripts")]
            public string OutputDir { get; set; } = "fuzzer-output";

            [Option('t', "timeout", Default = 5000, HelpText = "Timeout in milliseconds for each VM execution")]
            public int TimeoutMs { get; set; }

            [Option('m', "mutation-rate", Default = 0.1, HelpText = "Rate of mutation for script evolution (0.0-1.0)")]
            public double MutationRate { get; set; }

            [Option('c', "corpus", HelpText = "Directory with initial corpus of scripts")]
            public string? CorpusDir { get; set; }

            [Option('v', "verbose", Default = false, HelpText = "Enable verbose output")]
            public bool Verbose { get; set; }

            [Option('r', "report-interval", Default = 100, HelpText = "Interval for progress reporting")]
            public int ReportInterval { get; set; }

            [Option("detect-dos", Default = false, HelpText = "Enable detection of potential DOS vectors")]
            public bool DetectDOS { get; set; }

            [Option("dos-threshold", Default = 0.8, HelpText = "Threshold for flagging potential DOS vectors (0.0-1.0)")]
            public double DOSThreshold { get; set; }

            [Option("track-memory", Default = false, HelpText = "Enable detailed memory tracking for DOS detection")]
            public bool TrackMemory { get; set; }

            [Option("track-opcodes", Default = true, HelpText = "Track execution time per opcode for DOS detection")]
            public bool TrackOpcodes { get; set; }
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunFuzzer)
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Failed to parse command line arguments:");
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  {error}");
                    }
                });
        }

        /// <summary>
        /// Runs the fuzzer with the specified options
        /// </summary>
        private static void RunFuzzer(Options options)
        {
            Console.WriteLine("=== Neo VM Fuzzer ===");

            // Initialize random with seed if provided
            Random random = options.Seed.HasValue
                ? new Random(options.Seed.Value)
                : new Random();

            if (options.Seed.HasValue)
            {
                Console.WriteLine($"Using seed: {options.Seed.Value}");
            }
            else
            {
                Console.WriteLine("Using random seed");
            }

            // Create output directory
            Directory.CreateDirectory(options.OutputDir);
            Console.WriteLine($"Output directory: {options.OutputDir}");

            // Initialize components
            var scriptGenerator = new ScriptGenerator(random);
            var mutationEngine = new MutationEngine(random, options.MutationRate);
            var vmRunner = new VMRunner(
                options.TimeoutMs,
                detectDOS: options.DetectDOS,
                dosThreshold: options.DOSThreshold,
                trackMemory: options.TrackMemory,
                trackOpcodes: options.TrackOpcodes);
            var corpusManager = new CorpusManager(options.OutputDir, options.CorpusDir);
            var fuzzingResults = new FuzzingResults(options.OutputDir);
            var coverageTracker = new CoverageTracker();

            // Load initial corpus if available
            corpusManager.LoadCorpus();
            Console.WriteLine($"Initial corpus size: {corpusManager.CorpusSize}");

            Console.WriteLine($"Starting fuzzing with {options.Iterations} iterations...");
            Console.WriteLine();

            int crashCount = 0;
            int timeoutCount = 0;
            int newCoverageCount = 0;

            // Main fuzzing loop
            for (int i = 0; i < options.Iterations; i++)
            {
                // Report progress periodically
                if (i % options.ReportInterval == 0 && i > 0)
                {
                    ReportProgress(i, options.Iterations, crashCount, timeoutCount, newCoverageCount, coverageTracker.CoverageCount);
                }

                // Generate or mutate a script
                byte[] scriptBytes;

                if (corpusManager.CorpusSize > 0 && random.NextDouble() < 0.7) // 70% chance to use corpus when available
                {
                    // Use and mutate a script from the corpus
                    scriptBytes = corpusManager.GetRandomScript();
                    scriptBytes = mutationEngine.MutateScript(scriptBytes);
                }
                else
                {
                    // Generate a new random script
                    scriptBytes = scriptGenerator.GenerateRandomScript();
                }

                // Execute the script
                var executionResult = vmRunner.Execute(scriptBytes);

                // Check if it's a potential DOS vector
                if (options.DetectDOS && executionResult.DOSAnalysis?.IsPotentialDOSVector == true)
                {
                    corpusManager.SaveDOSVector(scriptBytes, executionResult.DOSAnalysis);
                    Console.WriteLine($"Found potential DOS vector! Score: {executionResult.DOSAnalysis.DOSScore:F2}, Reason: {executionResult.DOSAnalysis.DetectionReason}");
                }
                else if (options.DetectDOS && executionResult.DOSAnalysis != null)
                {
                    // Add detailed logging about why it wasn't detected as a DOS vector
                    Console.WriteLine($"Script analyzed but not flagged as DOS vector. Score: {executionResult.DOSAnalysis.DOSScore:F2}, Threshold: {options.DOSThreshold:F2}");
                    Console.WriteLine($"Metrics: Instructions={executionResult.DOSAnalysis.Metrics["TotalInstructions"]}, MaxStackDepth={executionResult.DOSAnalysis.Metrics["MaxStackDepth"]}, ExecutionTime={executionResult.DOSAnalysis.Metrics["TotalExecutionTimeMs"]}ms");

                    if (executionResult.Crashed)
                    {
                        Console.WriteLine($"Script crashed with {executionResult.ExceptionType} but DOS analysis was still performed");
                    }
                }
                else if (options.DetectDOS)
                {
                    Console.WriteLine("DOS analysis was not performed or returned null");
                }

                // Check if it crashed
                if (executionResult.Crashed)
                {
                    corpusManager.SaveCrash(scriptBytes, executionResult.ExceptionType);
                    Console.WriteLine($"Found crash! Exception: {executionResult.ExceptionType}");
                }

                // Check if it found new coverage
                bool foundNewCoverage = false;
                foreach (var point in executionResult.Coverage)
                {
                    if (coverageTracker.AddCoveragePoint(point))
                    {
                        foundNewCoverage = true;
                    }
                }

                if (foundNewCoverage)
                {
                    corpusManager.SaveInteresting(scriptBytes);
                    Console.WriteLine("Found new coverage!");
                }

                // Record result for statistics
                fuzzingResults.RecordResult(
                    executionResult.ExecutionTimeMs,
                    executionResult.Crashed,
                    executionResult.TimedOut,
                    foundNewCoverage,
                    executionResult.ExceptionType,
                    executionResult.Coverage?.Select(c => ParseOpCode(c))?.Where(op => op != OpCode.NOP),
                    executionResult.DOSAnalysis
                );

                // Occasionally reset the VM to avoid memory issues
                if (i % 1000 == 999)
                {
                    GC.Collect();
                    Thread.Sleep(100);
                }
            }

            // Report final results
            ReportProgress(options.Iterations, options.Iterations, crashCount, timeoutCount, newCoverageCount, coverageTracker.CoverageCount);

            // Save results
            string resultsFile = $"fuzzing_results_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            fuzzingResults.SaveResults(resultsFile);

            // Save histogram
            string histogramFile = $"execution_time_histogram_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            fuzzingResults.SaveExecutionTimeHistogram(histogramFile);

            Console.WriteLine($"\nResults saved to {Path.Combine(options.OutputDir, resultsFile)}");
            Console.WriteLine($"Execution time histogram saved to {Path.Combine(options.OutputDir, histogramFile)}");
            Console.WriteLine("\nFuzzing completed!");
        }

        /// <summary>
        /// Reports progress during fuzzing
        /// </summary>
        private static void ReportProgress(int current, int total, int crashes, int timeouts, int newCoverage, int totalCoverage)
        {
            double progress = (double)current / total * 100;
            Console.WriteLine($"Progress: {current}/{total} ({progress:F2}%)");
            Console.WriteLine($"Crashes: {crashes} | Timeouts: {timeouts} | New Coverage: {newCoverage}");
            Console.WriteLine($"Total Coverage: {totalCoverage} points");
            Console.WriteLine();
        }

        /// <summary>
        /// Parses an opcode from a coverage string
        /// </summary>
        private static OpCode ParseOpCode(string coveragePoint)
        {
            if (string.IsNullOrEmpty(coveragePoint) || !coveragePoint.StartsWith("OpCode:"))
            {
                return OpCode.NOP;
            }

            string opCodeStr = coveragePoint.Substring(7); // Remove "OpCode:" prefix

            if (Enum.TryParse<OpCode>(opCodeStr, out var opCode))
            {
                return opCode;
            }

            return OpCode.NOP;
        }
    }
}
