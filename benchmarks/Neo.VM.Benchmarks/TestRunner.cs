// Copyright (C) 2015-2025 The Neo Project.
//
// TestRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Neo.VM;
using System.Diagnostics;

namespace Neo.VM.Benchmark
{
    [MemoryDiagnoser]
    [SimpleJob]
    public class TestRunner
    {
        [GlobalSetup]
        public void Setup()
        {
            Console.WriteLine("=== Neo VM Benchmark Execution Environment Setup ===");
            Console.WriteLine(".NET Version: " + Environment.Version);
            Console.WriteLine("Platform: " + Environment.OSVersion);
            Console.WriteLine("Processor Count: " + Environment.ProcessorCount);
            Console.WriteLine();
        }

        [Benchmark]
        public void SimplePushAdd()
        {
            using var engine = new ExecutionEngine();

            // Simple script: PUSH1, PUSH2, ADD, DROP
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PUSH2,
                (byte)Neo.VM.OpCode.ADD,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }

        [Benchmark]
        public void SimpleMathOperations()
        {
            using var engine = new ExecutionEngine();

            // Multiple math operations
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH5,
                (byte)Neo.VM.OpCode.PUSH3,
                (byte)Neo.VM.OpCode.MUL,
                (byte)Neo.VM.OpCode.PUSH10,
                (byte)Neo.VM.OpCode.ADD,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }

        [Benchmark]
        public void StackOperations()
        {
            using var engine = new ExecutionEngine();

            // Stack manipulation operations
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PUSH2,
                (byte)Neo.VM.OpCode.PUSH3,
                (byte)Neo.VM.OpCode.DUP,
                (byte)Neo.VM.OpCode.SWAP,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }

        [Benchmark]
        public void ControlFlow()
        {
            using var engine = new ExecutionEngine();

            // Simple control flow
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH0,
                (byte)Neo.VM.OpCode.JMPIF, 0x02, // Jump 2 bytes forward
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.PUSH2,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }

        [Benchmark]
        public void ArrayOperations()
        {
            using var engine = new ExecutionEngine();

            // Array creation and manipulation
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.NEWARRAY0,
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PACK,
                (byte)Neo.VM.OpCode.SIZE,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Console.WriteLine("=== Neo VM Benchmark Execution Complete ===");
        }
    }
}
