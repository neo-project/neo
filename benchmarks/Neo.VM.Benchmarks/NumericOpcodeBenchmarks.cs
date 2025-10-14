// Copyright (C) 2015-2025 The Neo Project.
//
// NumericOpcodeBenchmarks.cs file belongs to the neo project and is free
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
    /// Executes arithmetic/bitwise/boolean comparison opcodes against real VM scripts so that
    /// dynamic gas analysis has measured data for every numeric instruction.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class NumericOpcodeBenchmarks
    {
        private const int Iterations = 64;

        public sealed record OpcodeCase(string Name, byte[] Script)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _unaryCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _boolUnaryCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _binaryCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _shiftCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _boolBinaryCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _bitwiseCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _comparisonCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _ternaryCases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _withinCases = Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _unaryCases = BuildUnaryCases();
            _boolUnaryCases = BuildBoolUnaryCases();
            _binaryCases = BuildBinaryCases();
            _shiftCases = BuildShiftCases();
            _boolBinaryCases = BuildBoolBinaryCases();
            _bitwiseCases = BuildBitwiseCases();
            _comparisonCases = BuildComparisonCases();
            _withinCases = BuildWithinCases();
            _ternaryCases = BuildTernaryCases();
        }

        [Benchmark]
        [BenchmarkCategory("UnaryNumeric")]
        [ArgumentsSource(nameof(UnaryCases))]
        public void UnaryNumeric(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("UnaryBoolean")]
        [ArgumentsSource(nameof(BoolUnaryCases))]
        public void UnaryBoolean(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("BinaryNumeric")]
        [ArgumentsSource(nameof(BinaryCases))]
        public void BinaryNumeric(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("Shift")]
        [ArgumentsSource(nameof(ShiftCases))]
        public void Shift(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("BinaryBoolean")]
        [ArgumentsSource(nameof(BoolBinaryCases))]
        public void BinaryBoolean(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("Bitwise")]
        [ArgumentsSource(nameof(BitwiseCases))]
        public void Bitwise(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("Comparison")]
        [ArgumentsSource(nameof(ComparisonCases))]
        public void Comparisons(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.WITHIN))]
        [ArgumentsSource(nameof(WithinCases))]
        public void Within(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory("TernaryNumeric")]
        [ArgumentsSource(nameof(TernaryCases))]
        public void TernaryNumeric(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> UnaryCases() => _unaryCases;
        public IEnumerable<OpcodeCase> BoolUnaryCases() => _boolUnaryCases;
        public IEnumerable<OpcodeCase> BinaryCases() => _binaryCases;
        public IEnumerable<OpcodeCase> ShiftCases() => _shiftCases;
        public IEnumerable<OpcodeCase> BoolBinaryCases() => _boolBinaryCases;
        public IEnumerable<OpcodeCase> BitwiseCases() => _bitwiseCases;
        public IEnumerable<OpcodeCase> ComparisonCases() => _comparisonCases;
        public IEnumerable<OpcodeCase> WithinCases() => _withinCases;
        public IEnumerable<OpcodeCase> TernaryCases() => _ternaryCases;

        #region Case builders

        private static OpcodeCase[] BuildUnaryCases()
        {
            var opcodes = new[]
            {
                OpCode.NEGATE, OpCode.ABS, OpCode.SIGN,
                OpCode.INC, OpCode.DEC, OpCode.NOT
            };

            var cases = new List<OpcodeCase>();
            foreach (var opcode in opcodes)
            {
                cases.Add(new OpcodeCase(opcode.ToString(), BuildUnaryScript(opcode, i => i - 32)));
            }
            return cases.ToArray();
        }

        private static OpcodeCase[] BuildBoolUnaryCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.NZ.ToString(), BuildUnaryScript(OpCode.NZ, i => i % 2 == 0 ? 0 : 1))
            };
        }

        private static OpcodeCase[] BuildBinaryCases()
        {
            var cases = new List<OpcodeCase>();
            var binaryOps = new[]
            {
                OpCode.ADD, OpCode.SUB, OpCode.MUL, OpCode.DIV, OpCode.MOD,
                OpCode.MIN, OpCode.MAX
            };
            foreach (var opcode in binaryOps)
            {
                cases.Add(new OpcodeCase(opcode.ToString(), BuildBinaryScript(opcode, i => (i + 5, (i % 5) + 1))));
            }
            return cases.ToArray();
        }

        private static OpcodeCase[] BuildShiftCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.SHL.ToString(), BuildBinaryScript(OpCode.SHL, i => (1 << (i % 8), 1))),
                new OpcodeCase(OpCode.SHR.ToString(), BuildBinaryScript(OpCode.SHR, i => (256 >> (i % 4), 1)))
            };
        }

        private static OpcodeCase[] BuildBoolBinaryCases()
        {
            var cases = new List<OpcodeCase>();
            var boolOps = new[] { OpCode.BOOLAND, OpCode.BOOLOR };
            foreach (var opcode in boolOps)
            {
                cases.Add(new OpcodeCase(opcode.ToString(), BuildBinaryScript(opcode, i => ((i % 2) == 0, (i % 3) == 0))));
            }
            return cases.ToArray();
        }

        private static OpcodeCase[] BuildBitwiseCases()
        {
            var cases = new List<OpcodeCase>();
            var ops = new[] { OpCode.AND, OpCode.OR, OpCode.XOR };
            foreach (var opcode in ops)
            {
                cases.Add(new OpcodeCase(opcode.ToString(), BuildBinaryScript(opcode, i => (0x0F0F0F0F + i, 0x00FF00FF - i))));
            }
            cases.Add(new OpcodeCase(OpCode.INVERT.ToString(), BuildUnaryScript(OpCode.INVERT, i => i)));
            return cases.ToArray();
        }

        private static OpcodeCase[] BuildComparisonCases()
        {
            var ops = new[]
            {
                OpCode.NUMEQUAL, OpCode.NUMNOTEQUAL,
                OpCode.LT, OpCode.LE, OpCode.GT, OpCode.GE
            };
            var cases = new List<OpcodeCase>();
            foreach (var opcode in ops)
            {
                cases.Add(new OpcodeCase(opcode.ToString(), BuildBinaryScript(opcode, i => (i, i + (i % 2)))));
            }
            return cases.ToArray();
        }

        private static OpcodeCase[] BuildWithinCases()
        {
            return new[]
            {
                new OpcodeCase("WITHIN", BuildWithinScript(Iterations))
            };
        }

        private static OpcodeCase[] BuildTernaryCases()
        {
            return new[]
            {
                new OpcodeCase(OpCode.MODMUL.ToString(), BuildTernaryScript(OpCode.MODMUL, i =>
                {
                    var a = i + 3;
                    var b = (i % 5) + 2;
                    var mod = (i % 7) + 11;
                    return (a, b, mod);
                }))
            };
        }

        #endregion

        #region Script builders

        private static byte[] BuildUnaryScript(OpCode opcode, Func<int, object> valueFactory)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitLiteral(builder, valueFactory(i));
                builder.Emit(opcode);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildBinaryScript(OpCode opcode, Func<int, (object Left, object Right)> operandFactory)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                var (left, right) = operandFactory(i);
                EmitLiteral(builder, left);
                EmitLiteral(builder, right);
                builder.Emit(opcode);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildWithinScript(int iterations)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < iterations; i++)
            {
                var value = i % 10;
                var lower = 2;
                var upper = 8;
                EmitLiteral(builder, value);
                EmitLiteral(builder, lower);
                EmitLiteral(builder, upper);
                builder.Emit(OpCode.WITHIN);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildTernaryScript(OpCode opcode, Func<int, (object A, object B, object C)> operandFactory)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                var (a, b, c) = operandFactory(i);
                EmitLiteral(builder, a);
                EmitLiteral(builder, b);
                EmitLiteral(builder, c);
                builder.Emit(opcode);
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

        private static void EmitLiteral(ScriptBuilder builder, object value)
        {
            switch (value)
            {
                case int i:
                    builder.EmitPush(new BigInteger(i));
                    break;
                case long l:
                    builder.EmitPush(new BigInteger(l));
                    break;
                case BigInteger bi:
                    builder.EmitPush(bi);
                    break;
                case bool b:
                    builder.EmitPush(b);
                    break;
                default:
                    throw new ArgumentException($"Unsupported literal type {value.GetType()}", nameof(value));
            }
        }

        #endregion
    }
}
