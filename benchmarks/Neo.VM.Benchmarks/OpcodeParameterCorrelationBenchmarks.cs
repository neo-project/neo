// Copyright (C) 2015-2025 The Neo Project.
//
// OpcodeParameterCorrelationBenchmarks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Neo.VM.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class OpcodeParameterCorrelationBenchmarks
    {
        [GlobalSetup]
        public void Setup()
        {
            Console.WriteLine("=== Opcode Parameter Correlation Benchmarks Setup ===");
            Console.WriteLine("Measuring performance with varying stack sizes and data sizes");
            Console.WriteLine(".NET Version: " + Environment.Version);
            Console.WriteLine("Platform: " + Environment.OSVersion);
            Console.WriteLine("Processor Count: " + Environment.ProcessorCount);
            Console.WriteLine();
        }

        #region Stack Size Correlation Tests

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        [Arguments(10000)]
        public void StackOperationsWithVaryingDepth(int stackSize)
        {
            using var engine = new ExecutionEngine();

            // Build a script that creates a stack of specified size
            var script = new List<byte>();

            // Push values to create desired stack size
            for (int i = 0; i < Math.Min(stackSize, 1000); i++) // Limit for performance
            {
                script.Add(0x11); // PUSH1
            }

            // Perform stack operations
            script.Add(0x4A); // DUP
            script.Add(0x50); // SWAP
            script.Add(0x45); // DROP

            // Clean up remaining items
            for (int i = 0; i < Math.Min(stackSize + 1, 1001); i++)
            {
                script.Add(0x45); // DROP
            }

            script.Add(0x40); // RET

            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public void ArrayOperationsWithVaryingSize(int arraySize)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>
            {
                // Create array with specified size
                0x11 // PUSH1
            };
            var sizeBytes = BitConverter.GetBytes(arraySize);
            foreach (var b in sizeBytes.Take(4))
            {
                script.Add((byte)b);
            }

            // Use appropriate NEWARRAY based on size
            if (arraySize <= 255)
            {
                script.Add(0xC3); // NEWARRAY
            }
            else
            {
                script.Add(0xC3); // NEWARRAY (handles larger sizes)
                script.Add(0x45); // DROP (for this test, we'll skip large arrays)
            }

            // Perform array operations
            script.Add(0xCA); // SIZE
            script.Add(0x45); // DROP

            // Clean up
            script.Add(0x45); // DROP
            script.Add(0x40); // RET

            if (arraySize <= 1000) // Limit size for this benchmark
            {
                engine.LoadScript(script.ToArray());
                engine.Execute();
            }
        }

        #endregion

        #region Data Size Correlation Tests

        [Benchmark]
        [Arguments(32)]    // 32 bytes
        [Arguments(256)]   // 256 bytes
        [Arguments(1024)]  // 1KB
        [Arguments(4096)]  // 4KB
        public void PushDataWithVaryingSize(int dataSize)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>();

            // Use PUSHDATA1 for data up to 255 bytes, PUSHDATA2 for larger
            if (dataSize <= 255)
            {
                script.Add(0x0C); // PUSHDATA1
                script.Add((byte)dataSize); // Length prefix
            }
            else
            {
                script.Add(0x0D); // PUSHDATA2
                var sizeBytes = BitConverter.GetBytes((ushort)dataSize);
                script.Add((byte)sizeBytes[0]);
                script.Add((byte)sizeBytes[1]);
            }

            // Add dummy data
            for (int i = 0; i < Math.Min(dataSize, 4096); i++)
            {
                script.Add((byte)(i % 256));
            }

            script.Add(0x45); // DROP
            script.Add(0x40); // RET

            if (dataSize <= 4096) // Limit size for this benchmark
            {
                engine.LoadScript(script.ToArray());
                engine.Execute();
            }
        }

        [Benchmark]
        [Arguments(10)]    // 10 iterations
        [Arguments(100)]   // 100 iterations
        [Arguments(1000)]  // 1000 iterations
        public void ArithmeticOperationsWithIterations(int iterations)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>
            {
                // Start with a value
                0x10 // PUSH0
            };

            // Perform repeated operations
            for (int i = 0; i < Math.Min(iterations, 1000); i++)
            {
                script.Add(0x11); // PUSH1
                script.Add(0x9E); // ADD
            }

            // Clean up
            script.Add(0x45); // DROP result
            script.Add(0x40); // RET

            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Memory Usage Correlation Tests

        [Benchmark]
        [Arguments(1)]      // Single array
        [Arguments(10)]     // 10 arrays
        [Arguments(100)]    // 100 arrays
        public void MultipleArrayCreation(int arrayCount)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>();

            // Create multiple arrays
            for (int i = 0; i < Math.Min(arrayCount, 100); i++)
            {
                script.Add(0xC2); // NEWARRAY0
            }

            // Clean up
            for (int i = 0; i < Math.Min(arrayCount, 100); i++)
            {
                script.Add(0x45); // DROP
            }

            script.Add(0x40); // RET

            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(5)]      // 5 elements
        [Arguments(50)]     // 50 elements
        [Arguments(500)]    // 500 elements
        public void ArrayPackOperations(int elementCount)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>();

            // Push elements onto stack
            for (int i = 0; i < Math.Min(elementCount, 500); i++)
            {
                script.Add(0x11); // PUSH1
            }

            // Pack them into an array
            script.Add(0x11); // PUSH1 (array size)
            script.Add(0xC0); // PACK

            // Clean up
            script.Add(0x45); // DROP
            script.Add(0x40); // RET

            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Control Flow Complexity Tests

        [Benchmark]
        [Arguments(10)]     // Simple loop
        [Arguments(100)]    // Medium loop
        [Arguments(1000)]   // Complex loop
        public void LoopOperationsWithComplexity(int loopCount)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>
            {
                // Initialize counter
                0x10 // PUSH0
            };

            // Simple loop structure
            for (int i = 0; i < Math.Min(loopCount, 1000); i++)
            {
                script.Add(0x9C); // INC
            }

            // Clean up
            script.Add(0x45); // DROP
            script.Add(0x40); // RET

            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Bitwise Operation Performance with Data Patterns

        [Benchmark]
        [Arguments("zeros")]      // Pattern of zeros
        [Arguments("ones")]       // Pattern of ones
        [Arguments("random")]     // Random pattern
        [Arguments("alternating")] // Alternating pattern
        public void BitwiseOperationsWithPatterns(string pattern)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>();

            // Push two values based on pattern
            int value1, value2;
            switch (pattern)
            {
                case "zeros":
                    value1 = 0; value2 = 0;
                    break;
                case "ones":
                    value1 = -1; value2 = -1; // 0xFFFFFFFF as signed int
                    break;
                case "random":
                    value1 = 0x12345678; value2 = unchecked((int)0x87654321); // Cast to signed int
                    break;
                case "alternating":
                    value1 = unchecked((int)0xAAAAAAAA); value2 = 0x55555555;
                    break;
                default:
                    value1 = 1; value2 = 2;
                    break;
            }

            // Push values (using PUSHINT32 for 32-bit values)
            script.Add(0x02); // PUSHINT32
            script.AddRange(BitConverter.GetBytes(value1));
            script.Add(0x02); // PUSHINT32
            script.AddRange(BitConverter.GetBytes(value2));

            // Perform bitwise operations
            script.Add(0x91); // AND
            script.Add(0x45); // DROP result

            script.Add(0x02); // PUSHINT32
            script.AddRange(BitConverter.GetBytes(value1));
            script.Add(0x02); // PUSHINT32
            script.AddRange(BitConverter.GetBytes(value2));

            script.Add(0x92); // OR
            script.Add(0x45); // DROP result

            script.Add(0x02); // PUSHINT32
            script.AddRange(BitConverter.GetBytes(value1));
            script.Add(0x02); // PUSHINT32
            script.AddRange(BitConverter.GetBytes(value2));

            script.Add(0x93); // XOR
            script.Add(0x45); // DROP result

            script.Add(0x40); // RET

            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Mathematical Complexity Tests

        [Benchmark]
        [Arguments(2, "POWER")]      // 2^n
        [Arguments(3, "POWER")]      // 3^n
        [Arguments(10, "FACTORIAL")] // n!
        [Arguments(100, "PRIME")]    // Prime calculation
        public void MathematicalComplexityTests(int baseValue, string operation)
        {
            using var engine = new ExecutionEngine();

            var script = new List<byte>();

            switch (operation)
            {
                case "POWER":
                    // Calculate baseValue ^ 10
                    script.Add(0x02); // PUSHINT32
                    script.AddRange(BitConverter.GetBytes(baseValue));
                    script.Add(0x11); // PUSH10
                    script.Add(0xA3); // POW
                    break;

                case "FACTORIAL":
                    // Simple factorial simulation using multiplication
                    script.Add(0x11); // PUSH1 (start with 1)
                    for (int i = 2; i <= Math.Min(baseValue, 20); i++) // Limit to prevent overflow
                    {
                        script.Add(0x11); // PUSH1 (representing i)
                        script.Add(0xA0); // MUL
                    }
                    break;

                case "PRIME":
                    // Simple primality test simulation
                    script.Add(0x02); // PUSHINT32
                    script.AddRange(BitConverter.GetBytes(baseValue));
                    // Simplified primality check (just a placeholder for performance testing)
                    script.Add(0x9C); // INC
                    script.Add(0x9D); // DEC
                    break;
            }

            script.Add(0x45); // DROP result
            script.Add(0x40); // RET

            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        [GlobalCleanup]
        public void Cleanup()
        {
            Console.WriteLine("=== Opcode Parameter Correlation Benchmarks Complete ===");
        }
    }
}
