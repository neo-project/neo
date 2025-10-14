// Copyright (C) 2015-2025 The Neo Project.
//
// ComprehensiveOpcodeBenchmarks.cs file belongs to the neo project and is free
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

namespace Neo.VM.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class ComprehensiveOpcodeBenchmarks
    {
        private const int Iterations = 1000;

        [GlobalSetup]
        public void Setup()
        {
            Console.WriteLine("=== Comprehensive Neo VM Opcode Benchmarks Setup ===");
            Console.WriteLine($"Iterations per benchmark: {Iterations}");
            Console.WriteLine(".NET Version: " + Environment.Version);
            Console.WriteLine("Platform: " + Environment.OSVersion);
            Console.WriteLine("Processor Count: " + Environment.ProcessorCount);
            Console.WriteLine();
        }

        #region Push Constants (0x00-0x20)

        [Benchmark]
        [Arguments(0x08, "PUSHT")]
        [Arguments(0x09, "PUSHF")]
        [Arguments(0x0B, "PUSHNULL")]
        [Arguments(0x0F, "PUSHM1")]
        [Arguments(0x10, "PUSH0")]
        [Arguments(0x11, "PUSH1")]
        [Arguments(0x12, "PUSH2")]
        [Arguments(0x13, "PUSH3")]
        [Arguments(0x14, "PUSH4")]
        [Arguments(0x15, "PUSH5")]
        public void PushConstantOpcodes(byte opcode, string name)
        {
            // Create a script with multiple operations for proper benchmarking
            var script = new List<byte>();
            for (int i = 0; i < Iterations; i++)
            {
                script.Add(opcode); // Add the opcode
                script.Add(0x45); // DROP to clean stack
            }
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Flow Control (0x21-0x41)

        [Benchmark]
        [Arguments(0x21, "NOP")]
        [Arguments(0x40, "RET")]
        public void FlowControlOpcodes(byte opcode, string name)
        {
            // Create a script with multiple operations for proper benchmarking
            var script = new List<byte>();
            for (int i = 0; i < Iterations; i++)
            {
                if (opcode == 0x40) // RET needs stack manipulation
                {
                    script.Add(0x10); // PUSH0 to have something to return
                }
                script.Add(opcode); // Add the opcode
            }

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        [Benchmark]
        [Arguments(0x22, "JMP")]
        [Arguments(0x24, "JMPIF")]
        [Arguments(0x26, "JMPIFNOT")]
        public void JumpOpcodes(byte opcode, string name)
        {
            // Create a script with jump operations
            var script = new List<byte>();
            for (int i = 0; i < Math.Min(100, Iterations); i++) // Limit jump operations
            {
                script.Add(0x10); // PUSH0 (false for conditional jumps)
                script.Add(opcode); // Jump opcode
                script.Add(0x02); // Jump offset
                script.Add(0x10); // PUSH0 (target)
                script.Add(0x45); // DROP
            }
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Stack Operations (0x43-0x55)

        [Benchmark]
        [Arguments(0x43, "DEPTH")]
        [Arguments(0x45, "DROP")]
        [Arguments(0x46, "NIP")]
        [Arguments(0x49, "CLEAR")]
        [Arguments(0x4A, "DUP")]
        [Arguments(0x4B, "OVER")]
        [Arguments(0x4E, "TUCK")]
        [Arguments(0x50, "SWAP")]
        [Arguments(0x51, "ROT")]
        [Arguments(0x53, "REVERSE3")]
        [Arguments(0x54, "REVERSE4")]
        public void StackOpcodes(byte opcode, string name)
        {
            // Create a script with stack operations
            var script = new List<byte>();
            for (int i = 0; i < Iterations; i++)
            {
                // Setup stack for operations that need values
                script.Add(0x11); // PUSH1
                script.Add(0x12); // PUSH2
                script.Add(0x13); // PUSH3

                script.Add(opcode); // Test opcode

                // Clean up
                script.Add(0x45); // DROP
                script.Add(0x45); // DROP
                script.Add(0x45); // DROP
            }
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Bitwise Logic (0x90-0x98)

        [Benchmark]
        [Arguments(0x90, "INVERT")]
        [Arguments(0x91, "AND")]
        [Arguments(0x92, "OR")]
        [Arguments(0x93, "XOR")]
        [Arguments(0x97, "EQUAL")]
        [Arguments(0x98, "NOTEQUAL")]
        public void BitwiseOpcodes(byte opcode, string name)
        {
            // Create a script with bitwise operations
            var script = new List<byte>();
            for (int i = 0; i < Iterations; i++)
            {
                if (opcode >= 0x91) // Binary operations need two values
                {
                    script.Add(0x11); // PUSH1
                    script.Add(0x12); // PUSH2
                }
                else // Unary operations need one value
                {
                    script.Add(0x11); // PUSH1
                }

                script.Add(opcode); // Test opcode
                script.Add(0x45); // DROP result
            }
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Arithmetic (0x99-0xBB)

        [Benchmark]
        [Arguments(0x99, "SIGN")]
        [Arguments(0x9A, "ABS")]
        [Arguments(0x9B, "NEGATE")]
        [Arguments(0x9C, "INC")]
        [Arguments(0x9D, "DEC")]
        [Arguments(0x9E, "ADD")]
        [Arguments(0x9F, "SUB")]
        [Arguments(0xA0, "MUL")]
        [Arguments(0xA1, "DIV")]
        [Arguments(0xA2, "MOD")]
        [Arguments(0xA8, "SHL")]
        [Arguments(0xA9, "SHR")]
        [Arguments(0xAA, "NOT")]
        [Arguments(0xAB, "BOOLAND")]
        [Arguments(0xAC, "BOOLOR")]
        [Arguments(0xB1, "NZ")]
        [Arguments(0xB3, "NUMEQUAL")]
        [Arguments(0xB4, "NUMNOTEQUAL")]
        [Arguments(0xB5, "LT")]
        [Arguments(0xB6, "LE")]
        [Arguments(0xB7, "GT")]
        [Arguments(0xB8, "GE")]
        [Arguments(0xB9, "MIN")]
        [Arguments(0xBA, "MAX")]
        [Arguments(0xBB, "WITHIN")]
        public void ArithmeticOpcodes(byte opcode, string name)
        {
            // Create a script with arithmetic operations
            var script = new List<byte>();
            for (int i = 0; i < Iterations; i++)
            {
                if (opcode >= 0x9E && opcode <= 0xA9) // Binary operations
                {
                    script.Add(0x11); // PUSH1
                    script.Add(0x12); // PUSH2
                }
                else if (opcode == 0xBB) // WITHIN needs 3 values
                {
                    script.Add(0x11); // PUSH1
                    script.Add(0x12); // PUSH2
                    script.Add(0x13); // PUSH3
                }
                else // Unary operations
                {
                    script.Add(0x11); // PUSH1
                }

                script.Add(opcode); // Test opcode
                script.Add(0x45); // DROP result
            }
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Compound Types (0xBE-0xD4)

        [Benchmark]
        [Arguments(0xC2, "NEWARRAY0")]
        [Arguments(0xCA, "SIZE")]
        [Arguments(0xCC, "KEYS")]
        [Arguments(0xCD, "VALUES")]
        [Arguments(0xCE, "PICKITEM")]
        [Arguments(0xD1, "REVERSEITEMS")]
        [Arguments(0xD3, "CLEARITEMS")]
        [Arguments(0xD4, "POPITEM")]
        public void CompoundTypeOpcodes(byte opcode, string name)
        {
            // Create a script with compound type operations
            var script = new List<byte>();
            for (int i = 0; i < Math.Min(100, Iterations); i++) // Limit compound operations
            {
                if (opcode == 0xC2) // NEWARRAY0 doesn't need setup
                {
                    script.Add(opcode); // Test opcode
                }
                else if (opcode == 0xCA) // SIZE needs an array
                {
                    script.Add(0xC2); // NEWARRAY0
                    script.Add(opcode); // Test opcode
                }
                else // Other operations need array setup
                {
                    script.Add(0xC2); // NEWARRAY0
                    script.Add(opcode); // Test opcode
                }

                script.Add(0x45); // DROP result
            }
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        #region Type Operations (0xD8-0xDB)

        [Benchmark]
        [Arguments(0xD8, "ISNULL")]
        [Arguments(0xDB, "CONVERT")]
        public void TypeOpcodes(byte opcode, string name)
        {
            // Create a script with type operations
            var script = new List<byte>();
            for (int i = 0; i < Iterations; i++)
            {
                script.Add(0x11); // PUSH1
                script.Add(opcode); // Test opcode
                script.Add(0x45); // DROP result
            }
            script.Add(0x40); // RET

            using var engine = new ExecutionEngine();
            engine.LoadScript(script.ToArray());
            engine.Execute();
        }

        #endregion

        [GlobalCleanup]
        public void Cleanup()
        {
            Console.WriteLine("=== Comprehensive Opcode Benchmarks Complete ===");
        }
    }
}
