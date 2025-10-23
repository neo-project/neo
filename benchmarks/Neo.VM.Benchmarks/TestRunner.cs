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
using Neo.VM;
using System;
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
        public void SimplePushAdd() => Execute(builder =>
        {
            builder.EmitPush(1);
            builder.EmitPush(2);
            builder.Emit(OpCode.ADD);
            builder.Emit(OpCode.DROP);
        });

        [Benchmark]
        public void SimpleMathOperations() => Execute(builder =>
        {
            builder.EmitPush(5);
            builder.EmitPush(3);
            builder.Emit(OpCode.MUL);
            builder.EmitPush(10);
            builder.Emit(OpCode.ADD);
            builder.Emit(OpCode.DROP);
        });

        [Benchmark]
        public void StackOperations() => Execute(builder =>
        {
            builder.EmitPush(1);
            builder.EmitPush(2);
            builder.EmitPush(3);
            builder.Emit(OpCode.DUP);
            builder.Emit(OpCode.SWAP);
            builder.Emit(OpCode.DROP);
            builder.Emit(OpCode.DROP);
            builder.Emit(OpCode.DROP);
        });

        [Benchmark]
        public void ControlFlow() => Execute(builder =>
        {
            builder.EmitPush(0);
            builder.Emit(OpCode.JMPIF, new[] { (byte)0x02 });
            builder.EmitPush(1);
            builder.Emit(OpCode.DROP);
            builder.EmitPush(2);
            builder.Emit(OpCode.DROP);
        });

        [Benchmark]
        public void ArrayOperations() => Execute(builder =>
        {
            builder.Emit(OpCode.NEWARRAY0);
            builder.EmitPush(1);
            builder.EmitPush(1);
            builder.Emit(OpCode.PACK);
            builder.Emit(OpCode.SIZE);
            builder.Emit(OpCode.DROP);
        });

        [GlobalCleanup]
        public void Cleanup()
        {
            Console.WriteLine("=== Neo VM Benchmark Execution Complete ===");
        }

        private static void Execute(Action<ScriptBuilder> emitter)
        {
            using var builder = new ScriptBuilder();
            emitter(builder);
            builder.Emit(OpCode.RET);

            using var engine = new ExecutionEngine();
            engine.LoadScript(builder.ToArray());
            Debug.Assert(engine.Execute() == VMState.HALT);
        }
    }
}
