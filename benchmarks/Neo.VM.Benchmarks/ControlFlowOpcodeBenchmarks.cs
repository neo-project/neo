// Copyright (C) 2015-2025 The Neo Project.
//
// ControlFlowOpcodeBenchmarks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Benchmarks the Neo VM opcodes whose correctness depends on control-flow targets
    /// (CALL*, TRY/ENDTRY/ENDFINALLY) and syscalls whose cost depends on the host jump table.
    /// Scripts are emitted via <see cref="InstructionBuilder"/> so that all offsets are valid.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class ControlFlowOpcodeBenchmarks
    {
        public sealed record OpcodeCase(string Name, byte[] Script, Func<ExecutionEngine>? EngineFactory = null)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _callCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _callLongCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _callPointerCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _tryCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _tryLongCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _syscallCases = System.Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _callCases = BuildCallCases(OpCode.CALL);
            _callLongCases = BuildCallCases(OpCode.CALL_L);
            _callPointerCases = BuildCallPointerCases();
            _tryCases = BuildTryCases(OpCode.TRY, OpCode.ENDTRY);
            _tryLongCases = BuildTryCases(OpCode.TRY_L, OpCode.ENDTRY_L);
            _syscallCases = BuildSyscallCases();
        }

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.CALL))]
        [ArgumentsSource(nameof(CallCases))]
        public void CALL(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.CALL_L))]
        [ArgumentsSource(nameof(CallLongCases))]
        public void CALL_L(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.CALLA))]
        [ArgumentsSource(nameof(CallPointerCases))]
        public void CALLA(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.TRY))]
        [ArgumentsSource(nameof(TryCases))]
        public void TRY_WITH_FINALLY(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.TRY_L))]
        [ArgumentsSource(nameof(TryLongCases))]
        public void TRY_L_WITH_FINALLY(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.SYSCALL))]
        [ArgumentsSource(nameof(SyscallCases))]
        public void SYSCALL(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> CallCases() => _callCases;
        public IEnumerable<OpcodeCase> CallLongCases() => _callLongCases;
        public IEnumerable<OpcodeCase> CallPointerCases() => _callPointerCases;
        public IEnumerable<OpcodeCase> TryCases() => _tryCases;
        public IEnumerable<OpcodeCase> TryLongCases() => _tryLongCases;
        public IEnumerable<OpcodeCase> SyscallCases() => _syscallCases;

        #region Case builders

        private static OpcodeCase[] BuildCallCases(OpCode callOpcode)
        {
            var iterations = new[] { 1, 8, 32 };
            return iterations
                .Select(count => new OpcodeCase(
                    $"{callOpcode}_{count}",
                    BuildCallScript(callOpcode, count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildCallPointerCases()
        {
            var iterations = new[] { 1, 8, 32 };
            return iterations
                .Select(count => new OpcodeCase(
                    $"CALLA_{count}",
                    BuildCallPointerScript(count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildTryCases(OpCode tryOpcode, OpCode endTryOpcode)
        {
            var counts = new[] { 1, 4, 16 };
            return counts
                .Select(count => new OpcodeCase(
                    $"{tryOpcode}_{count}",
                    BuildTryScript(tryOpcode, endTryOpcode, count)))
                .ToArray();
        }

        private static OpcodeCase[] BuildSyscallCases()
        {
            var payloadSizes = new[] { 0, 16, 256 };
            return payloadSizes
                .Select(size => new OpcodeCase(
                    $"SYSCALL_{size}",
                    BuildSyscallScript(size),
                    () => new SyscallBenchmarkEngine()))
                .ToArray();
        }

        #endregion

        #region Script builders

        private static byte[] BuildCallScript(OpCode callOpcode, int callCount)
        {
            var builder = new InstructionBuilder();
            var functionTarget = new JumpTarget();

            for (int i = 0; i < callCount; i++)
            {
                builder.Jump(callOpcode, functionTarget);
                builder.AddInstruction(OpCode.DROP);
            }

            var endInstruction = builder.AddInstruction(OpCode.RET);

            var functionStart = builder.Push(1);
            functionTarget._instruction = functionStart;
            builder.AddInstruction(OpCode.RET);

            return builder.ToArray();
        }

        private static byte[] BuildCallPointerScript(int callCount)
        {
            var builder = new InstructionBuilder();
            var functionTarget = new JumpTarget();

            for (int i = 0; i < callCount; i++)
            {
                builder.AddInstruction(new Instruction
                {
                    _opCode = OpCode.PUSHA,
                    _target = functionTarget
                });
                builder.AddInstruction(OpCode.CALLA);
                builder.AddInstruction(OpCode.DROP);
            }

            var endInstruction = builder.AddInstruction(OpCode.RET);

            var functionStart = builder.Push(1);
            functionTarget._instruction = functionStart;
            builder.AddInstruction(OpCode.RET);

            return builder.ToArray();
        }

        private static byte[] BuildTryScript(OpCode tryOpcode, OpCode endTryOpcode, int tryCount)
        {
            var builder = new InstructionBuilder();

            for (int i = 0; i < tryCount; i++)
            {
                var finallyTarget = new JumpTarget();
                var endTarget = new JumpTarget();

                builder.AddInstruction(new Instruction
                {
                    _opCode = tryOpcode,
                    _target = new JumpTarget(), // no catch block
                    _target2 = finallyTarget
                });

                builder.Push(i);
                builder.AddInstruction(OpCode.DROP);

                builder.AddInstruction(new Instruction
                {
                    _opCode = endTryOpcode,
                    _target = endTarget
                });

                var endInstruction = builder.AddInstruction(OpCode.NOP);
                endTarget._instruction = endInstruction;

                var finallyStart = builder.Push(i);
                finallyTarget._instruction = finallyStart;
                builder.AddInstruction(OpCode.DROP);
                builder.AddInstruction(OpCode.ENDFINALLY);
            }

            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildSyscallScript(int payloadSize)
        {
            using var builder = new ScriptBuilder();
            if (payloadSize > 0)
            {
                builder.EmitPush(GeneratePayload(payloadSize));
                builder.Emit(OpCode.DROP);
            }

            builder.Emit(OpCode.SYSCALL, BitConverter.GetBytes(0x12345678));
            builder.Emit(OpCode.DROP);
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        #endregion

        #region Helpers

        private static void ExecuteCase(OpcodeCase @case)
        {
            using var engine = @case.EngineFactory?.Invoke() ?? new ExecutionEngine();
            engine.LoadScript(@case.Script);
            var state = engine.Execute();
            if (state != VMState.HALT)
                throw new InvalidOperationException($"Benchmark case '{@case.Name}' ended with VM state {state}.");
        }

        private static byte[] GeneratePayload(int size)
        {
            var data = new byte[size];
            for (int i = 0; i < size; i++)
                data[i] = (byte)(i % 251);
            return data;
        }

        private sealed class SyscallBenchmarkEngine : ExecutionEngine
        {
            public SyscallBenchmarkEngine()
                : base(new SyscallJumpTable(), new ReferenceCounter(), ExecutionEngineLimits.Default)
            {
            }
        }

        private sealed class SyscallJumpTable : JumpTable
        {
            public override void Syscall(ExecutionEngine engine, global::Neo.VM.Instruction instruction)
            {
                // Simulate a cheap syscall that pushes a small result
                engine.Push(true);
            }
        }

        #endregion
    }
}
