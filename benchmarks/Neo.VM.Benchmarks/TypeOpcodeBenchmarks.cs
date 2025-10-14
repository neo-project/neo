// Copyright (C) 2015-2025 The Neo Project.
//
// TypeOpcodeBenchmarks.cs file belongs to the neo project and is free
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

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Executes type/abort/assert opcodes under controlled scripts so we can measure their runtime cost.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class TypeOpcodeBenchmarks
    {
        private const int Iterations = 64;

        public sealed record OpcodeCase(string Name, byte[] Script, bool ExpectFault = false)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _isNullCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _isTypeCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _assertCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _assertMsgCases = System.Array.Empty<OpcodeCase>();
        private OpcodeCase[] _abortMsgCases = System.Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _isNullCases = BuildIsNullCases();
            _isTypeCases = BuildIsTypeCases();
            _assertCases = BuildAssertCases();
            _assertMsgCases = BuildAssertMsgCases();
            _abortMsgCases = BuildAbortMsgCases();
        }

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.ISNULL))]
        [ArgumentsSource(nameof(IsNullCases))]
        public void ISNULL(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.ISTYPE))]
        [ArgumentsSource(nameof(IsTypeCases))]
        public void ISTYPE(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.ASSERT))]
        [ArgumentsSource(nameof(AssertCases))]
        public void ASSERT(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.ASSERTMSG))]
        [ArgumentsSource(nameof(AssertMsgCases))]
        public void ASSERTMSG(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.ABORTMSG))]
        [ArgumentsSource(nameof(AbortMsgCases))]
        public void ABORTMSG(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> IsNullCases() => _isNullCases;
        public IEnumerable<OpcodeCase> IsTypeCases() => _isTypeCases;
        public IEnumerable<OpcodeCase> AssertCases() => _assertCases;
        public IEnumerable<OpcodeCase> AssertMsgCases() => _assertMsgCases;
        public IEnumerable<OpcodeCase> AbortMsgCases() => _abortMsgCases;

        #region Case builders

        private static OpcodeCase[] BuildIsNullCases()
        {
            return new[]
            {
                new OpcodeCase("ISNULL_TRUE", BuildIsNullScript(pushNull: true)),
                new OpcodeCase("ISNULL_FALSE", BuildIsNullScript(pushNull: false))
            };
        }

        private static OpcodeCase[] BuildIsTypeCases()
        {
            return new[]
            {
                new OpcodeCase("ISTYPE_Integer", BuildIsTypeScript(StackItemType.Integer)),
                new OpcodeCase("ISTYPE_ByteString", BuildIsTypeScript(StackItemType.ByteString)),
                new OpcodeCase("ISTYPE_Boolean", BuildIsTypeScript(StackItemType.Boolean))
            };
        }

        private static OpcodeCase[] BuildAssertCases()
        {
            return new[]
            {
                new OpcodeCase("ASSERT_TRUE", BuildAssertScript(alwaysTrue: true), ExpectFault: false),
                new OpcodeCase("ASSERT_FALSE", BuildAssertScript(alwaysTrue: false), ExpectFault: true)
            };
        }

        private static OpcodeCase[] BuildAssertMsgCases()
        {
            return new[]
            {
                new OpcodeCase("ASSERTMSG_TRUE", BuildAssertMsgScript(alwaysTrue: true), ExpectFault: false),
                new OpcodeCase("ASSERTMSG_FALSE", BuildAssertMsgScript(alwaysTrue: false), ExpectFault: true)
            };
        }

        private static OpcodeCase[] BuildAbortMsgCases()
        {
            return new[]
            {
                new OpcodeCase("ABORTMSG", BuildAbortMsgScript(), ExpectFault: true)
            };
        }

        #endregion

        #region Script builders

        private static byte[] BuildIsNullScript(bool pushNull)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                if (pushNull)
                    builder.Emit(OpCode.PUSHNULL);
                else
                    builder.EmitPush(i);
                builder.Emit(OpCode.ISNULL);
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildIsTypeScript(StackItemType targetType)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                EmitTypedValue(builder, targetType, i);
                builder.Emit(OpCode.ISTYPE, new[] { (byte)targetType });
                builder.Emit(OpCode.DROP);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildAssertScript(bool alwaysTrue)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                builder.EmitPush(alwaysTrue ? 1 : 0);
                builder.Emit(OpCode.ASSERT);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildAssertMsgScript(bool alwaysTrue)
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                builder.EmitPush(alwaysTrue ? 1 : 0);
                builder.EmitPush("fail");
                builder.Emit(OpCode.ASSERTMSG);
            }
            builder.Emit(OpCode.RET);
            return builder.ToArray();
        }

        private static byte[] BuildAbortMsgScript()
        {
            using var builder = new ScriptBuilder();
            for (int i = 0; i < Iterations; i++)
            {
                builder.EmitPush("abort");
                builder.Emit(OpCode.ABORTMSG);
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
                // Expected fault path; ignore exception to allow benchmark measurement
            }
        }

        private static void EmitTypedValue(ScriptBuilder builder, StackItemType type, int seed)
        {
            switch (type)
            {
                case StackItemType.Integer:
                    builder.EmitPush(seed);
                    break;
                case StackItemType.Boolean:
                    builder.EmitPush(seed % 2 == 0);
                    break;
                case StackItemType.ByteString:
                    builder.EmitPush(new byte[] { (byte)(seed % 256) });
                    break;
                default:
                    builder.EmitPush(seed);
                    break;
            }
        }

        #endregion
    }
}
