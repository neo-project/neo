// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.InstructionCaching.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace Neo.VM.Benchmark
{
    [MemoryDiagnoser]
    public class InstructionCachingBenchmarks
    {
        private byte[] smallScript = null!;
        private byte[] mediumScript = null!;
        private byte[] largeScript = null!;
        private Script normalScript = null!;
        private CachedScript cachedScript = null!;
        private Script normalMediumScript = null!;
        private CachedScript cachedMediumScript = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Generate test scripts of various sizes
            smallScript = GenerateScript(100);
            mediumScript = GenerateScript(1000);
            largeScript = GenerateScript(10000);

            normalScript = new Script(smallScript);
            cachedScript = new CachedScript(smallScript);
            normalMediumScript = new Script(mediumScript);
            cachedMediumScript = new CachedScript(mediumScript);
        }

        private byte[] GenerateScript(int instructionCount)
        {
            var script = new List<byte>();
            var random = new Random(42); // Fixed seed for reproducibility

            for (int i = 0; i < instructionCount; i++)
            {
                // Mix of different instruction types
                var choice = random.Next(5);
                switch (choice)
                {
                    case 0: // PUSH1
                        script.Add((byte)VM.OpCode.PUSH1);
                        break;
                    case 1: // PUSHDATA1
                        script.Add((byte)VM.OpCode.PUSHDATA1);
                        var len = (byte)random.Next(1, 10);
                        script.Add(len);
                        for (int j = 0; j < len; j++)
                            script.Add((byte)random.Next(256));
                        break;
                    case 2: // ADD
                        script.Add((byte)VM.OpCode.ADD);
                        break;
                    case 3: // DUP
                        script.Add((byte)VM.OpCode.DUP);
                        break;
                    case 4: // NOP
                        script.Add((byte)VM.OpCode.NOP);
                        break;
                }
            }

            return script.ToArray();
        }

        [Benchmark(Baseline = true)]
        public void Normal_SequentialAccess()
        {
            for (int ip = 0; ip < normalScript.Length;)
            {
                var instruction = normalScript.GetInstruction(ip);
                ip += instruction.Size;
            }
        }

        [Benchmark]
        public void Cached_SequentialAccess()
        {
            for (int ip = 0; ip < cachedScript.Length;)
            {
                var instruction = cachedScript.GetInstruction(ip);
                ip += instruction.Size;
            }
        }

        [Benchmark]
        public void Normal_RandomAccess()
        {
            var random = new Random(42);
            var positions = new List<int> { 0 };

            // Build valid instruction positions
            for (int ip = 0; ip < normalScript.Length;)
            {
                var instruction = normalScript.GetInstruction(ip);
                ip += instruction.Size;
                if (ip < normalScript.Length)
                    positions.Add(ip);
            }

            // Random access pattern
            for (int i = 0; i < 100; i++)
            {
                var pos = positions[random.Next(positions.Count)];
                _ = normalScript.GetInstruction(pos);
            }
        }

        [Benchmark]
        public void Cached_RandomAccess()
        {
            var random = new Random(42);
            var positions = new List<int> { 0 };

            // Build valid instruction positions
            for (int ip = 0; ip < cachedScript.Length;)
            {
                var instruction = cachedScript.GetInstruction(ip);
                ip += instruction.Size;
                if (ip < cachedScript.Length)
                    positions.Add(ip);
            }

            // Random access pattern
            for (int i = 0; i < 100; i++)
            {
                var pos = positions[random.Next(positions.Count)];
                _ = cachedScript.GetInstruction(pos);
            }
        }

        [Benchmark]
        public void Normal_RepeatedAccess()
        {
            // Simulate VM execution pattern: repeated access to same instructions (loops)
            for (int loop = 0; loop < 10; loop++)
            {
                for (int ip = 0; ip < Math.Min(50, normalMediumScript.Length);)
                {
                    var instruction = normalMediumScript.GetInstruction(ip);
                    ip += instruction.Size;
                }
            }
        }

        [Benchmark]
        public void Cached_RepeatedAccess()
        {
            // Simulate VM execution pattern: repeated access to same instructions (loops)
            for (int loop = 0; loop < 10; loop++)
            {
                for (int ip = 0; ip < Math.Min(50, cachedMediumScript.Length);)
                {
                    var instruction = cachedMediumScript.GetInstruction(ip);
                    ip += instruction.Size;
                }
            }
        }
    }

    /// <summary>
    /// Simple demonstration of instruction caching performance improvements
    /// </summary>
    public class SimpleInstructionCachingBenchmark
    {
        public static void RunBenchmark()
        {
            Console.WriteLine("=== Neo VM Instruction Caching Performance Benchmark ===\n");

            // Generate a test script
            var scriptBytes = new List<byte>();
            for (int i = 0; i < 500; i++)
            {
                if (i % 5 == 0)
                {
                    scriptBytes.Add((byte)VM.OpCode.PUSHDATA1);
                    scriptBytes.Add(4);
                    scriptBytes.AddRange(BitConverter.GetBytes(i));
                }
                else if (i % 3 == 0)
                {
                    scriptBytes.Add((byte)VM.OpCode.ADD);
                }
                else
                {
                    scriptBytes.Add((byte)VM.OpCode.PUSH1);
                }
            }

            var script = scriptBytes.ToArray();

            // Warm up
            for (int i = 0; i < 10; i++)
            {
                var s1 = new Script(script);
                var s2 = new CachedScript(script);
                _ = s1.GetInstruction(0);
                _ = s2.GetInstruction(0);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Benchmark 1: Sequential Access (First Pass)
            Console.WriteLine("1. Sequential Access Performance (First Pass):");

            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            var normalScript = new Script(script);
            for (int ip = 0; ip < normalScript.Length;)
            {
                var instruction = normalScript.GetInstruction(ip);
                ip += instruction.Size;
            }
            sw1.Stop();

            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            var cachedScript = new CachedScript(script);
            for (int ip = 0; ip < cachedScript.Length;)
            {
                var instruction = cachedScript.GetInstruction(ip);
                ip += instruction.Size;
            }
            sw2.Stop();

            var normalTime1 = sw1.Elapsed.TotalMicroseconds;
            var cachedTime1 = sw2.Elapsed.TotalMicroseconds;

            Console.WriteLine($"   Normal Script:     {normalTime1:F2} μs");
            Console.WriteLine($"   Cached Script:     {cachedTime1:F2} μs");
            Console.WriteLine($"   Note: Initial pass includes pre-decoding overhead\n");

            // Benchmark 2: Repeated Access (Simulating loops)
            Console.WriteLine("2. Repeated Access Performance (10 iterations over first 100 bytes):");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var positions = new List<int>();
            for (int ip = 0; ip < Math.Min(100, script.Length);)
            {
                var instruction = normalScript.GetInstruction(ip);
                positions.Add(ip);
                ip += instruction.Size;
            }

            var sw3 = System.Diagnostics.Stopwatch.StartNew();
            for (int iter = 0; iter < 10; iter++)
            {
                foreach (var pos in positions)
                {
                    _ = normalScript.GetInstruction(pos);
                }
            }
            sw3.Stop();

            var sw4 = System.Diagnostics.Stopwatch.StartNew();
            for (int iter = 0; iter < 10; iter++)
            {
                foreach (var pos in positions)
                {
                    _ = cachedScript.GetInstruction(pos);
                }
            }
            sw4.Stop();

            var normalTime2 = sw3.Elapsed.TotalMicroseconds;
            var cachedTime2 = sw4.Elapsed.TotalMicroseconds;
            var speedup = normalTime2 / cachedTime2;

            Console.WriteLine($"   Normal Script:     {normalTime2:F2} μs");
            Console.WriteLine($"   Cached Script:     {cachedTime2:F2} μs");
            Console.WriteLine($"   Speedup:           {speedup:F2}x faster");
            Console.WriteLine($"   Improvement:       {((speedup - 1) * 100):F1}% performance gain\n");

            // Cache Statistics
            var (cached, total, hitRatio) = cachedScript.GetCacheStats();
            Console.WriteLine("3. Cache Statistics:");
            Console.WriteLine($"   Cached Instructions: {cached}/{total}");
            Console.WriteLine($"   Cache Hit Ratio:     {hitRatio:F1}%\n");

            // Summary
            Console.WriteLine("=== Summary ===");
            Console.WriteLine("Instruction caching provides:");
            Console.WriteLine($"• {speedup:F1}x faster repeated instruction access");
            Console.WriteLine("• Eliminates redundant instruction decoding");
            Console.WriteLine("• Especially beneficial for loops and hot paths");
            Console.WriteLine("• Small memory overhead for significant performance gain");
            Console.WriteLine($"• {hitRatio:F0}% of instructions pre-decoded and cached");
        }
    }
}
