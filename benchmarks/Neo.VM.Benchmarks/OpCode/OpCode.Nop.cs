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
    public class OpCode_Nop : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.NOP;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ItemCount);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Push(ItemCount);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}


//     | Method | Mean     | Error    | StdDev   | Median   |
//     |------- |---------:|---------:|---------:|---------:|
//     | Bench  | 951.6 ns | 57.35 ns | 160.8 ns | 900.0 ns |
