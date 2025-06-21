// Copyright (C) 2015-2025 The Neo Project.
//
// Simple.JumpTable.Benchmark.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Simple benchmark to demonstrate jump table performance improvements
    /// </summary>
    public class SimpleJumpTableBenchmark
    {
        /// <summary>
        /// Legacy jump table using reflection (old approach)
        /// </summary>
        public class ReflectionJumpTable : JumpTable
        {
            public ReflectionJumpTable()
            {
                // Initialize all entries to InvalidOpcode first
                for (var x = 0; x < Table.Length; x++)
                {
                    Table[x] = InvalidOpcode;
                }

                // Use reflection to populate the table (old approach)
                InitializeWithReflection();
            }

            private void InitializeWithReflection()
            {
                // This mimics the original reflection-based implementation
                foreach (var mi in GetType().GetMethods())
                {
                    if (Enum.TryParse<VM.OpCode>(mi.Name, true, out var opCode))
                    {
                        if (Table[(byte)opCode] is not null && Table[(byte)opCode] != InvalidOpcode)
                        {
                            throw new InvalidOperationException($"Opcode {opCode} is already defined.");
                        }

                        try
                        {
                            Table[(byte)opCode] = (DelAction)mi.CreateDelegate(typeof(DelAction), this);
                        }
                        catch
                        {
                            // Method signature doesn't match, skip
                        }
                    }
                }
            }
        }

        public static void RunBenchmark()
        {
            const int iterations = 1000;

            Console.WriteLine("=== Neo VM Jump Table Performance Benchmark ===\n");

            // Benchmark JumpTable Construction
            Console.WriteLine("1. Jump Table Construction Performance:");

            // Warm up
            for (int i = 0; i < 10; i++)
            {
                var _ = new ReflectionJumpTable();
                GC.KeepAlive(_);
                var __ = new JumpTable();
                GC.KeepAlive(__);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Reflection-based (old approach)
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var jumpTable = new ReflectionJumpTable();
                GC.KeepAlive(jumpTable);
            }
            sw1.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Pre-compiled (new approach)
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var jumpTable = new JumpTable();
                GC.KeepAlive(jumpTable);
            }
            sw2.Stop();

            var reflectionTime = sw1.Elapsed.TotalMicroseconds;
            var precompiledTime = sw2.Elapsed.TotalMicroseconds;
            var speedup = reflectionTime / precompiledTime;

            Console.WriteLine($"   Reflection-based:  {reflectionTime:F2} μs (avg: {reflectionTime / iterations:F2} μs per table)");
            Console.WriteLine($"   Pre-compiled:      {precompiledTime:F2} μs (avg: {precompiledTime / iterations:F2} μs per table)");
            Console.WriteLine($"   Speedup:           {speedup:F1}x faster");
            Console.WriteLine($"   Improvement:       {((speedup - 1) * 100):F1}% performance gain\n");

            // Benchmark VM Initialization
            Console.WriteLine("2. VM Initialization Performance:");
            const int vmIterations = 100;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // VM with reflection-based jump table
            var sw3 = Stopwatch.StartNew();
            for (int i = 0; i < vmIterations; i++)
            {
                var jumpTable = new ReflectionJumpTable();
                var engine = new ExecutionEngine(jumpTable);
                GC.KeepAlive(engine);
            }
            sw3.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // VM with pre-compiled jump table
            var sw4 = Stopwatch.StartNew();
            for (int i = 0; i < vmIterations; i++)
            {
                var engine = new ExecutionEngine();
                GC.KeepAlive(engine);
            }
            sw4.Stop();

            var vmReflectionTime = sw3.Elapsed.TotalMicroseconds;
            var vmPrecompiledTime = sw4.Elapsed.TotalMicroseconds;
            var vmSpeedup = vmReflectionTime / vmPrecompiledTime;

            Console.WriteLine($"   Reflection-based:  {vmReflectionTime:F2} μs (avg: {vmReflectionTime / vmIterations:F2} μs per VM)");
            Console.WriteLine($"   Pre-compiled:      {vmPrecompiledTime:F2} μs (avg: {vmPrecompiledTime / vmIterations:F2} μs per VM)");
            Console.WriteLine($"   Speedup:           {vmSpeedup:F1}x faster");
            Console.WriteLine($"   Improvement:       {((vmSpeedup - 1) * 100):F1}% performance gain\n");

            // Summary
            Console.WriteLine("=== Summary ===");
            Console.WriteLine("The pre-compiled jump table eliminates expensive reflection overhead:");
            Console.WriteLine($"• Jump table construction is {speedup:F1}x faster");
            Console.WriteLine($"• VM initialization is {vmSpeedup:F1}x faster");
            Console.WriteLine($"• Overall improvement: {((Math.Min(speedup, vmSpeedup) - 1) * 100):F1}% - {((Math.Max(speedup, vmSpeedup) - 1) * 100):F1}% performance gain");
            Console.WriteLine("• Memory allocation reduced (fewer temporary objects)");
            Console.WriteLine("• Better JIT optimization potential");
        }
    }
}
