// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkProgram.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Neo.VM;
using System.IO;
using System.Linq;

namespace Neo.VM.Benchmark
{
    public class BenchmarkProgram
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Neo VM Benchmark Execution Environment ===");
            Console.WriteLine("Environment Info:");
            Console.WriteLine($".NET Version: {Environment.Version}");
            Console.WriteLine($"Platform: {Environment.OSVersion}");
            Console.WriteLine($"Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine();

            // Verify VM functionality first
            Console.WriteLine("=== VM Functionality Verification ===");
            ManualTest.RunTest();
            Console.WriteLine();

            // Run benchmarks
            Console.WriteLine("=== Running BenchmarkDotNet Benchmarks ===");
            var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var artifactsPath = Path.Combine(projectDirectory, "BenchmarkDotNet.Artifacts");
            var config = DefaultConfig.Instance.WithArtifactsPath(artifactsPath);
            var switcher = BenchmarkSwitcher.FromAssembly(typeof(BenchmarkProgram).Assembly);
            var summaries = args is { Length: > 0 }
                ? switcher.Run(args, config).ToArray()
                : switcher.RunAll(config).ToArray();

            Console.WriteLine("\n=== Benchmark Execution Complete ===");
            if (summaries.Length > 0)
            {
                // All summaries write to the same directory; report the first one.
                Console.WriteLine($"Results saved to: {summaries[0].ResultsDirectoryPath}");
            }
        }
    }
}
