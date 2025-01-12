// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PUSHNULL.cs file belongs to the neo project and is free
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
    public class OpCode_PUSHNULL
    {
        protected VM.OpCode Opcode => VM.OpCode.PUSHNULL;

        private BenchmarkEngine _engine;

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            _engine = new BenchmarkEngine();
            _engine.LoadScript(builder.ToArray());
            _engine.ExecuteUntil(Opcode);
            _engine.ExecuteNext();
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

//     | Method | Mean     | Error     | StdDev    |
//     |------- |---------:|----------:|----------:|
//     | Bench  | 1.729 us | 0.0516 us | 0.1396 us |
