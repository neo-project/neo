// Copyright (C) 2015-2025 The Neo Project.
//
// ExceptionOpcodeBenchmarks.cs file belongs to the neo project and is free
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
    /// Benchmarks exception-related opcodes that either throw faults or leverage special control flow.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class ExceptionOpcodeBenchmarks
    {
        private const int Iterations = 32;

        public sealed record OpcodeCase(string Name, byte[] Script, bool ExpectFault = false)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _throwCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _abortCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _calltCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _tryCatchCases = Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _throwCases = BuildThrowCases();
            _abortCases = BuildAbortCases();
            _calltCases = BuildCallTCases();
            _tryCatchCases = BuildTryCatchCases();
        }

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.THROW))]
        [ArgumentsSource(nameof(ThrowCases))]
        public void THROW(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.ABORT))]
        [ArgumentsSource(nameof(AbortCases))]
        public void ABORT(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.CALLT))]
        [ArgumentsSource(nameof(CallTCases))]
        public void CALLT(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("TRY_CATCH")]
        [ArgumentsSource(nameof(TryCatchCases))]
        public void TRY_CATCH(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> ThrowCases() => _throwCases;
        public IEnumerable<OpcodeCase> AbortCases() => _abortCases;
        public IEnumerable<OpcodeCase> CallTCases() => _calltCases;
        public IEnumerable<OpcodeCase> TryCatchCases() => _tryCatchCases;

        #region Case builders

        private static OpcodeCase[] BuildThrowCases()
        {
            return new[]
            {
                new OpcodeCase("THROW", BuildThrowScript(), ExpectFault: true)
            };
        }

        private static OpcodeCase[] BuildAbortCases()
        {
            return new[]
            {
                new OpcodeCase("ABORT", BuildAbortScript(), ExpectFault: true)
            };
        }

        private static OpcodeCase[] BuildCallTCases()
        {
            return new[]
            {
                new OpcodeCase("CALLT", BuildCallTScript(), ExpectFault: true)
            };
        }

        private static OpcodeCase[] BuildTryCatchCases()
        {
            return new[]
            {
                new OpcodeCase("TRY_CATCH", BuildTryCatchScript(), ExpectFault: false)
            };
        }

        #endregion

        #region Script builders

        private static byte[] BuildThrowScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                builder.EmitPush($"error_{i}");
                builder.Emit(OpCode.THROW);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildAbortScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                builder.Emit(OpCode.ABORT);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildCallTScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                builder.Emit(OpCode.CALLT, new byte[] { 0x01, 0x00 });
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildTryCatchScript()
        {
            var builder = new InstructionBuilder();

            for (int i = 0; i < Iterations; i++)
            {
                var catchTarget = new JumpTarget();
                var finallyTarget = new JumpTarget();
                var endTarget = new JumpTarget();

                builder.AddInstruction(new Instruction
                {
                    _opCode = OpCode.TRY,
                    _target = catchTarget,
                    _target2 = finallyTarget
                });

                builder.Push(i);
                builder.AddInstruction(OpCode.THROW);

                catchTarget._instruction = builder.Push("caught");
                builder.AddInstruction(OpCode.DROP);
                builder.AddInstruction(new Instruction
                {
                    _opCode = OpCode.ENDTRY,
                    _target = endTarget
                });

                finallyTarget._instruction = builder.Push(i);
                builder.AddInstruction(OpCode.DROP);
                builder.AddInstruction(OpCode.ENDFINALLY);

                endTarget._instruction = builder.AddInstruction(OpCode.NOP);
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
            try
            {
                var state = engine.Execute();
                if (@case.ExpectFault)
                {
                    if (state != VMState.FAULT)
                        throw new InvalidOperationException($"Expected FAULT for '{@case.Name}' but got {state}.");
                }
                else
                {
                    if (state != VMState.HALT)
                        throw new InvalidOperationException($"Expected HALT for '{@case.Name}' but got {state}.");
                }
            }
            catch when (@case.ExpectFault)
            {
                // Expected fault path; ignore for benchmark measurement
            }
        }

        #endregion
    }
}
