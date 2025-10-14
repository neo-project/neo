// Copyright (C) 2015-2025 The Neo Project.
//
// StackOpcodeBenchmarks.cs file belongs to the neo project and is free
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
using System.Numerics;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Benchmarks stack manipulation instructions by emitting explicit scripts that prepare
    /// and clean up the evaluation stack for each iteration.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class StackOpcodeBenchmarks
    {
        private const int Iterations = 64;

        public sealed record OpcodeCase(string Name, byte[] Script)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _simpleCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _duplicationCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _indexedCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _reversalCases = Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _simpleCases = BuildSimpleCases();
            _duplicationCases = BuildDuplicationCases();
            _indexedCases = BuildIndexedCases();
            _reversalCases = BuildReversalCases();
        }

        [Benchmark]
        [BenchmarkCategory("StackSimple")]
        [ArgumentsSource(nameof(SimpleCases))]
        public void SimpleOps(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("StackDuplication")]
        [ArgumentsSource(nameof(DuplicationCases))]
        public void DuplicationOps(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("StackIndexed")]
        [ArgumentsSource(nameof(IndexedCases))]
        public void IndexedOps(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("StackReversal")]
        [ArgumentsSource(nameof(ReversalCases))]
        public void ReversalOps(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> SimpleCases() => _simpleCases;
        public IEnumerable<OpcodeCase> DuplicationCases() => _duplicationCases;
        public IEnumerable<OpcodeCase> IndexedCases() => _indexedCases;
        public IEnumerable<OpcodeCase> ReversalCases() => _reversalCases;

        #region Case builders

        private static OpcodeCase[] BuildSimpleCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.DEPTH.ToString(), BuildDepthScript()),
                new OpcodeCase(OpCode.DROP.ToString(), BuildDropScript()),
                new OpcodeCase(OpCode.NIP.ToString(), BuildNipScript()),
                new OpcodeCase(OpCode.CLEAR.ToString(), BuildClearScript())
            };
        }

        private static OpcodeCase[] BuildDuplicationCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.DUP.ToString(), BuildDupScript()),
                new OpcodeCase(OpCode.OVER.ToString(), BuildOverScript()),
                new OpcodeCase(OpCode.TUCK.ToString(), BuildTuckScript()),
                new OpcodeCase(OpCode.SWAP.ToString(), BuildSwapScript()),
                new OpcodeCase(OpCode.ROT.ToString(), BuildRotScript())
            };
        }

        private static OpcodeCase[] BuildIndexedCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.XDROP.ToString(), BuildXDropScript()),
                new OpcodeCase(OpCode.PICK.ToString(), BuildPickScript()),
                new OpcodeCase(OpCode.ROLL.ToString(), BuildRollScript())
            };
        }

        private static OpcodeCase[] BuildReversalCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.REVERSE3.ToString(), BuildReverse3Script()),
                new OpcodeCase(OpCode.REVERSE4.ToString(), BuildReverse4Script()),
                new OpcodeCase(OpCode.REVERSEN.ToString(), BuildReverseNScript())
            };
        }

        #endregion

        #region Script builders

        private static byte[] BuildDepthScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                builder.Emit(OpCode.DEPTH);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildDropScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildNipScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                builder.Emit(OpCode.NIP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildClearScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                builder.Emit(OpCode.CLEAR);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildDupScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                builder.Emit(OpCode.DUP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildOverScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                builder.Emit(OpCode.OVER);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildTuckScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                builder.Emit(OpCode.TUCK);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildSwapScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                builder.Emit(OpCode.SWAP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildRotScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                builder.Emit(OpCode.ROT);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildXDropScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                EmitInt(builder, 1);
                builder.Emit(OpCode.XDROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildPickScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                EmitInt(builder, 2);
                builder.Emit(OpCode.PICK);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildRollScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                EmitInt(builder, 2);
                builder.Emit(OpCode.ROLL);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildReverse3Script()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                builder.Emit(OpCode.REVERSE3);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildReverse4Script()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                EmitInt(builder, i + 3);
                builder.Emit(OpCode.REVERSE4);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildReverseNScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitInt(builder, i);
                EmitInt(builder, i + 1);
                EmitInt(builder, i + 2);
                EmitInt(builder, i + 3);
                EmitInt(builder, 4);
                builder.Emit(OpCode.REVERSEN);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
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

        private static void EmitInt(ScriptBuilder builder, int value)
        {
            builder.EmitPush(new BigInteger(value));
        }

        #endregion
    }
}
