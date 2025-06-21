// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.JumpTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Types;
using System;
using System.Runtime.CompilerServices;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Benchmark comparing reflection-based vs pre-compiled jump table initialization
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob]
    public class Benchmarks_JumpTable
    {
        private const int OperationsPerInvoke = 1000;

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

        /// <summary>
        /// Test script for opcode dispatch benchmarks
        /// </summary>
        private readonly byte[] _testScript = new ScriptBuilder()
            .EmitPush(1)              // PUSH1
            .EmitPush(2)              // PUSH1 (with operand)
            .Emit(VM.OpCode.ADD)      // ADD
            .EmitPush(3)              // PUSH1
            .Emit(VM.OpCode.MUL)      // MUL
            .Emit(VM.OpCode.DUP)      // DUP
            .Emit(VM.OpCode.DROP)     // DROP
            .ToArray();

        [Benchmark(Description = "JumpTable Construction - Reflection Based (Old)", OperationsPerInvoke = OperationsPerInvoke)]
        public void CreateJumpTable_Reflection()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var jumpTable = new ReflectionJumpTable();
                GC.KeepAlive(jumpTable);
            }
        }

        [Benchmark(Description = "JumpTable Construction - Pre-compiled (New)", OperationsPerInvoke = OperationsPerInvoke)]
        public void CreateJumpTable_PreCompiled()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                var jumpTable = new JumpTable();
                GC.KeepAlive(jumpTable);
            }
        }

        [Benchmark(Description = "VM Initialization - Reflection Based", OperationsPerInvoke = OperationsPerInvoke / 10)]
        public void CreateExecutionEngine_Reflection()
        {
            for (int i = 0; i < OperationsPerInvoke / 10; i++)
            {
                var jumpTable = new ReflectionJumpTable();
                var engine = new ExecutionEngine(jumpTable);
                GC.KeepAlive(engine);
            }
        }

        [Benchmark(Description = "VM Initialization - Pre-compiled", OperationsPerInvoke = OperationsPerInvoke / 10)]
        public void CreateExecutionEngine_PreCompiled()
        {
            for (int i = 0; i < OperationsPerInvoke / 10; i++)
            {
                var engine = new ExecutionEngine();
                GC.KeepAlive(engine);
            }
        }

        [Benchmark(Description = "Opcode Dispatch - Reflection Based", OperationsPerInvoke = OperationsPerInvoke / 10)]
        public void OpcodeDispatch_Reflection()
        {
            var jumpTable = new ReflectionJumpTable();

            for (int i = 0; i < OperationsPerInvoke / 10; i++)
            {
                var engine = new ExecutionEngine(jumpTable);
                engine.LoadScript(_testScript);
                engine.Execute();
                GC.KeepAlive(engine);
            }
        }

        [Benchmark(Description = "Opcode Dispatch - Pre-compiled", OperationsPerInvoke = OperationsPerInvoke / 10)]
        public void OpcodeDispatch_PreCompiled()
        {
            for (int i = 0; i < OperationsPerInvoke / 10; i++)
            {
                var engine = new ExecutionEngine();
                engine.LoadScript(_testScript);
                engine.Execute();
                GC.KeepAlive(engine);
            }
        }

        [Benchmark(Description = "Single Opcode Lookup - Reflection Based", OperationsPerInvoke = OperationsPerInvoke * 10)]
        public void SingleOpcodeLookup_Reflection()
        {
            var jumpTable = new ReflectionJumpTable();
            var opcodes = new[] { VM.OpCode.PUSH1, VM.OpCode.ADD, VM.OpCode.MUL, VM.OpCode.DUP, VM.OpCode.DROP };

            for (int i = 0; i < OperationsPerInvoke * 2; i++)
            {
                var opcode = opcodes[i % opcodes.Length];
                var action = jumpTable[opcode];
                GC.KeepAlive(action);
            }
        }

        [Benchmark(Description = "Single Opcode Lookup - Pre-compiled", OperationsPerInvoke = OperationsPerInvoke * 10)]
        public void SingleOpcodeLookup_PreCompiled()
        {
            var jumpTable = new JumpTable();
            var opcodes = new[] { VM.OpCode.PUSH1, VM.OpCode.ADD, VM.OpCode.MUL, VM.OpCode.DUP, VM.OpCode.DROP };

            for (int i = 0; i < OperationsPerInvoke * 2; i++)
            {
                var opcode = opcodes[i % opcodes.Length];
                var action = jumpTable[opcode];
                GC.KeepAlive(action);
            }
        }

        /// <summary>
        /// Benchmark startup time for multiple VM instances (simulates high-throughput scenarios)
        /// </summary>
        [Benchmark(Description = "Multi-VM Initialization - Reflection Based", OperationsPerInvoke = 50)]
        public void MultiVMInitialization_Reflection()
        {
            var engines = new ExecutionEngine[50];

            for (int i = 0; i < 50; i++)
            {
                var jumpTable = new ReflectionJumpTable();
                engines[i] = new ExecutionEngine(jumpTable);
            }

            GC.KeepAlive(engines);
        }

        [Benchmark(Description = "Multi-VM Initialization - Pre-compiled", OperationsPerInvoke = 50)]
        public void MultiVMInitialization_PreCompiled()
        {
            var engines = new ExecutionEngine[50];

            for (int i = 0; i < 50; i++)
            {
                engines[i] = new ExecutionEngine();
            }

            GC.KeepAlive(engines);
        }
    }
}
