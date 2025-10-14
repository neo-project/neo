// Copyright (C) 2015-2025 The Neo Project.
//
// JumpOpcodeBenchmarks.cs file belongs to the neo project and is free
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
using System.Collections.Generic;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Benchmarks jump opcodes (including long variants) by emitting scripts that exercise the branch logic.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class JumpOpcodeBenchmarks
    {
        private const int Iterations = 64;

        public sealed record OpcodeCase(string Name, byte[] Script)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _unconditionalCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _booleanCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _comparisonCases = Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _unconditionalCases = BuildUnconditionalCases();
            _booleanCases = BuildBooleanCases();
            _comparisonCases = BuildComparisonCases();
        }

        [Benchmark]
        [BenchmarkCategory("JumpUnconditional")]
        [ArgumentsSource(nameof(UnconditionalCases))]
        public void Unconditional(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("JumpBoolean")]
        [ArgumentsSource(nameof(BooleanCases))]
        public void Boolean(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("JumpComparison")]
        [ArgumentsSource(nameof(ComparisonCases))]
        public void Comparison(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> UnconditionalCases() => _unconditionalCases;
        public IEnumerable<OpcodeCase> BooleanCases() => _booleanCases;
        public IEnumerable<OpcodeCase> ComparisonCases() => _comparisonCases;

        #region Case builders

        private static OpcodeCase[] BuildUnconditionalCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.JMP_L.ToString(), BuildUnconditionalScript(OpCode.JMP_L))
            };
        }

        private static OpcodeCase[] BuildBooleanCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.JMPIF_L.ToString(), BuildBooleanJumpScript(OpCode.JMPIF_L)),
                new OpcodeCase(OpCode.JMPIFNOT_L.ToString(), BuildBooleanJumpScript(OpCode.JMPIFNOT_L))
            };
        }

        private static OpcodeCase[] BuildComparisonCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.JMPEQ.ToString(), BuildComparisonJumpScript(OpCode.JMPEQ, i => (i, i))),
                new OpcodeCase(OpCode.JMPEQ_L.ToString(), BuildComparisonJumpScript(OpCode.JMPEQ_L, i => (i, i))),
                new OpcodeCase(OpCode.JMPNE.ToString(), BuildComparisonJumpScript(OpCode.JMPNE, i => (i, i + 1))),
                new OpcodeCase(OpCode.JMPNE_L.ToString(), BuildComparisonJumpScript(OpCode.JMPNE_L, i => (i, i + 1))),
                new OpcodeCase(OpCode.JMPGT.ToString(), BuildComparisonJumpScript(OpCode.JMPGT, i => (i + 1, i))),
                new OpcodeCase(OpCode.JMPGT_L.ToString(), BuildComparisonJumpScript(OpCode.JMPGT_L, i => (i + 1, i))),
                new OpcodeCase(OpCode.JMPGE.ToString(), BuildComparisonJumpScript(OpCode.JMPGE, i => (i, i))),
                new OpcodeCase(OpCode.JMPGE_L.ToString(), BuildComparisonJumpScript(OpCode.JMPGE_L, i => (i, i))),
                new OpcodeCase(OpCode.JMPLT.ToString(), BuildComparisonJumpScript(OpCode.JMPLT, i => (i, i + 1))),
                new OpcodeCase(OpCode.JMPLT_L.ToString(), BuildComparisonJumpScript(OpCode.JMPLT_L, i => (i, i + 1))),
                new OpcodeCase(OpCode.JMPLE.ToString(), BuildComparisonJumpScript(OpCode.JMPLE, i => (i, i))),
                new OpcodeCase(OpCode.JMPLE_L.ToString(), BuildComparisonJumpScript(OpCode.JMPLE_L, i => (i, i)))
            };
        }

        #endregion

        #region Script builders

        private static byte[] BuildUnconditionalScript(OpCode opcode)
        {
            var builder = new InstructionBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                var target = new JumpTarget();
                builder.Jump(opcode, target);
                builder.Push(i);
                builder.AddInstruction(OpCode.DROP);
                target._instruction = builder.AddInstruction(OpCode.NOP);
            }
            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildBooleanJumpScript(OpCode opcode)
        {
            var builder = new InstructionBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                var target = new JumpTarget();
                builder.Push(i % 2 == 0);
                builder.Jump(opcode, target);
                builder.Push(0);
                builder.AddInstruction(OpCode.DROP);
                target._instruction = builder.AddInstruction(OpCode.NOP);
            }
            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildComparisonJumpScript(OpCode opcode, Func<int, (int Left, int Right)> operands)
        {
            var builder = new InstructionBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                var target = new JumpTarget();
                var (left, right) = operands(i);

                builder.Push(left);
                builder.Push(right);
                builder.Jump(opcode, target);
                builder.Push(0);
                builder.AddInstruction(OpCode.DROP);
                target._instruction = builder.AddInstruction(OpCode.NOP);
            }
            builder.AddInstruction(OpCode.RET);
            return builder.ToArray();
        }

        #endregion

        #region Helpers

        private static void ExecuteCase(OpcodeCase @case)
        {
            using var engine = new ExecutionEngine();
            engine.LoadScript(@case.Script);
            var state = engine.Execute();
            if (state != VMState.HALT)
                throw new InvalidOperationException($"Benchmark case '{@case.Name}' ended with VM state {state}.");
        }

        #endregion
    }
}
