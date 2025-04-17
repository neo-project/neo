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

using Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core;
using Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Generators;
using System;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests
{
    /// <summary>
    /// Main program entry point for corpus generation and fuzzing
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "generate-corpus":
                        GenerateCorpus(args);
                        return;
                    case "run-corpus":
                        RunCorpus(args);
                        return;
                    case "analyze":
                        AnalyzeTestCase(args);
                        return;
                    case "fuzz":
                        FuzzSingleInput(args);
                        return;
                }
            }

            // Display help if no valid command is provided
            Console.WriteLine("DBFT Plugin Fuzzing Test");
            Console.WriteLine("========================");
            Console.WriteLine("Usage:");
            Console.WriteLine("  generate-corpus [output_dir] [count] - Generate corpus files");
            Console.WriteLine("  run-corpus [input_dir] - Run the fuzzer on corpus files");
            Console.WriteLine("  analyze <file_path> - Analyze a specific test case");
            Console.WriteLine("  fuzz <file_path> - Run fuzzer on a single input file (used by AFL)");
            Console.WriteLine("  For continuous fuzzing, use the run_fuzzer.sh script");
        }

        /// <summary>
        /// Generate corpus files for fuzzing
        /// </summary>
        private static void GenerateCorpus(string[] args)
        {
            string outputDir = args.Length > 1 ? args[1] : "corpus";
            int count = args.Length > 2 && int.TryParse(args[2], out int c) ? c : 10;

            Console.WriteLine($"Generating corpus files in {outputDir}...");
            CorpusGenerator.Generate(outputDir, count);
            Console.WriteLine($"Generated {count * 8} corpus files in {outputDir}");
            Console.WriteLine("Corpus generation completed.");
        }

        /// <summary>
        /// Run the fuzzer on all corpus files
        /// </summary>
        private static void RunCorpus(string[] args)
        {
            string inputDir = args.Length > 1 ? args[1] : "corpus";
            if (!Directory.Exists(inputDir))
            {
                Console.WriteLine($"Error: Input directory '{inputDir}' does not exist.");
                return;
            }

            string[] files = Directory.GetFiles(inputDir);
            if (files.Length == 0)
            {
                Console.WriteLine($"Error: No files found in '{inputDir}'.");
                return;
            }

            Console.WriteLine($"Running fuzzer on {files.Length} corpus files from {inputDir}...");
            int successCount = 0;
            int errorCount = 0;

            foreach (string file in files)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(file);
                    using var ms = new MemoryStream(data);
                    FuzzConsensus.Fuzz(ms);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(file)}: {ex.Message}");
                    errorCount++;
                }
            }

            Console.WriteLine($"Completed: {successCount} successful, {errorCount} errors");
        }

        /// <summary>
        /// Analyze a specific test case
        /// </summary>
        private static void AnalyzeTestCase(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Error: Please specify a file to analyze.");
                return;
            }

            string filePath = args[1];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File '{filePath}' does not exist.");
                return;
            }

            Console.WriteLine($"Analyzing test case: {filePath}");
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(data);
                FuzzConsensus.Fuzz(ms);
                Console.WriteLine("Analysis completed successfully. No issues detected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Analysis detected an issue: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Run fuzzer on a single input file (used by AFL)
        /// </summary>
        private static void FuzzSingleInput(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Error: Please specify a file to fuzz.");
                return;
            }

            string filePath = args[1];
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File '{filePath}' does not exist.");
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                using var ms = new MemoryStream(data);
                FuzzConsensus.Fuzz(ms);
            }
            catch (Exception)
            {
                // Don't output anything for AFL mode - just exit with error code
                Environment.Exit(1);
            }
        }
    }
}
