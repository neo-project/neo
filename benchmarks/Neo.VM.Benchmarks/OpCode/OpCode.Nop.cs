// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.Nop.cs file belongs to the neo project and is free
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
    public class OpCode_Nop
    {
        protected VM.OpCode Opcode => VM.OpCode.NOP;

        private BenchmarkEngine _engine;

        [IterationSetup]
        public void Setup()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(VM.OpCode.NOP);

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
        public void Bench() => _engine.ExecuteNext();
    }
}


//     | Method | Mean     | Error    | StdDev   | Median   |
//     |------- |---------:|---------:|---------:|---------:|
//     | Bench  | 951.6 ns | 57.35 ns | 160.8 ns | 900.0 ns |
