// Copyright (C) 2015-2025 The Neo Project.
//
// DataPushOpcodeBenchmarks.cs file belongs to the neo project and is free
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
    /// Benchmarks the data push opcodes by emitting buffers of varying sizes that exercise PUSHDATA1/2/4.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, iterationCount: 8)]
    public class DataPushOpcodeBenchmarks
    {
        private const int Iterations = 64;

        public sealed record OpcodeCase(string Name, byte[] Script)
        {
            public override string ToString() => Name;
        }

        private OpcodeCase[] _pushData1Cases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _pushData2Cases = Array.Empty<OpcodeCase>();
        private OpcodeCase[] _pushData4Cases = Array.Empty<OpcodeCase>();

        [GlobalSetup]
        public void Setup()
        {
            _pushData1Cases = BuildPushDataCases("PUSHDATA1", new[] { 1, 64, 255 });
            _pushData2Cases = BuildPushDataCases("PUSHDATA2", new[] { 256, 2048, 65535 });
            _pushData4Cases = BuildPushDataCases("PUSHDATA4", new[] { 65536, 100_000, 200_000 });
        }

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.PUSHDATA1))]
        [ArgumentsSource(nameof(PushData1Cases))]
        public void PUSHDATA1(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.PUSHDATA2))]
        [ArgumentsSource(nameof(PushData2Cases))]
        public void PUSHDATA2(OpcodeCase @case) => ExecuteCase(@case);

        [Benchmark]
        [BenchmarkCategory(nameof(OpCode.PUSHDATA4))]
        [ArgumentsSource(nameof(PushData4Cases))]
        public void PUSHDATA4(OpcodeCase @case) => ExecuteCase(@case);

        public IEnumerable<OpcodeCase> PushData1Cases() => _pushData1Cases;
        public IEnumerable<OpcodeCase> PushData2Cases() => _pushData2Cases;
        public IEnumerable<OpcodeCase> PushData4Cases() => _pushData4Cases;

        #region Case builders

        private static OpcodeCase[] BuildPushDataCases(string opcodeLabel, int[] sizes)
        {
            var cases = new List<OpcodeCase>();
            foreach (var size in sizes)
            {
                cases.Add(new OpcodeCase($"{opcodeLabel}_{size}", BuildPushDataScript(size)));
            }
            return cases.ToArray();
        }

        private static byte[] BuildPushDataScript(int payloadLength)
        {
            using var builder = new ScriptBuilder();
            var payload = GeneratePayload(payloadLength);

            for (int i = 0; i < Iterations; i++)
            {
                builder.EmitPush(payload);
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

        private static byte[] GeneratePayload(int length)
        {
            var data = new byte[length];
            for (int i = 0; i < length; i++)
                data[i] = (byte)(i % 251);
            return data;
        }

        #endregion
    }
}
