// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PUSHDATA4.cs file belongs to the neo project and is free
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
    public class OpCode_PUSHDATA4
    {
        protected VM.OpCode Opcode => VM.OpCode.PUSHDATA4;

        private BenchmarkEngine _engine;

        // [ParamsSource(nameof(StrLen))]
        // public byte[] _value;
        //
        // public static IEnumerable<byte[]> StrLen =>
        // [
        //     new byte[ushort.MaxValue/2],
        //     // new byte[ushort.MaxValue * 2-sizeof(uint)],
        // ];

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Push(new byte[ushort.MaxValue - 1]);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            _engine = new BenchmarkEngine();
            _engine.LoadScript(builder.ToArray());
            _engine.ExecuteUntil(Opcode);
        }

        [IterationCleanup]
        public void Cleanup()
        {
            _engine.Dispose();
        }

        [Benchmark]
        public void Bench() => _engine.ExecuteOneGASBenchmark();
    }
}

//     | Method | _value       | Mean     | Error     | StdDev    |
//     |------- |------------- |---------:|----------:|----------:|
//     | Bench  | Byte[131070] | 1.944 us | 0.0643 us | 0.1683 us |
//     | Bench  | Byte[65536]  | 1.903 us | 0.0468 us | 0.1312 us |
