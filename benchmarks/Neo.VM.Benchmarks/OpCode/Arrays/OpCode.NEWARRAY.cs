// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWARRAY.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWARRAY : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.NEWARRAY;

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

    //     | Method          | ItemCount | Mean             | Error         | StdDev        | Median           |
    //     |---------------- |---------- |-----------------:|--------------:|--------------:|-----------------:|
    //     | Bench_OneOpCode | 2         |         3.784 us |     0.3302 us |     0.9683 us |         3.300 us |
    //     | Bench_OneGAS    | 2         |    13,812.478 us |   267.5614 us |   338.3794 us |    13,746.500 us |
    //     | Bench_OneOpCode | 32        |         4.580 us |     0.3297 us |     0.9722 us |         4.100 us |
    //     | Bench_OneGAS    | 32        |    30,472.653 us |   609.1706 us |   569.8185 us |    30,440.000 us |
    //     | Bench_OneOpCode | 128       |         7.732 us |     0.4097 us |     1.2080 us |         7.550 us |
    //     | Bench_OneGAS    | 128       |    79,703.057 us | 1,425.9022 us | 1,264.0252 us |    79,672.500 us |
    //     | Bench_OneOpCode | 1024      |        34.877 us |     0.6786 us |     1.7637 us |        34.600 us |
    //     | Bench_OneGAS    | 1024      |   543,705.833 us | 6,539.3059 us | 6,116.8708 us |   543,597.300 us |
    //     | Bench_OneOpCode | 2040      |        43.552 us |     0.8678 us |     1.6299 us |        43.150 us |
    //     | Bench_OneGAS    | 2040      | 1,056,640.087 us | 9,672.7187 us | 9,047.8670 us | 1,056,767.500 us |
}
