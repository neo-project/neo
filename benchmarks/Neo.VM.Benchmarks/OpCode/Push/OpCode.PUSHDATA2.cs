// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PUSHDATA2.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_PUSHDATA2
    {
        protected VM.OpCode Opcode => VM.OpCode.PUSHDATA2;

        private BenchmarkEngine _engine;

        [ParamsSource(nameof(StrLen))]
        public byte[] _value;

        public static IEnumerable<byte[]> StrLen =>
        [
            new byte[byte.MaxValue + 1],
            new byte[ushort.MaxValue - 1],
        ];

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(VM.OpCode.NOP);
            builder.Push(_value);

            _engine = new BenchmarkEngine();
            _engine.LoadScript(builder.ToArray());
            _engine.ExecuteUntil(VM.OpCode.NOP);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _engine.Dispose();
        }

        [Benchmark]
        public void Bench() => _engine.ExecuteNext();
    }
}

// | Method | _value      | Mean     | Error     | StdDev    | Median   |
//     |------- |------------ |---------:|----------:|----------:|---------:|
//     | Bench  | Byte[256]   | 2.024 us | 0.1621 us | 0.4753 us | 1.800 us |
//     | Bench  | Byte[65534] | 1.981 us | 0.1299 us | 0.3684 us | 1.800 us |
