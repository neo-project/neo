// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWARRAYUT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_NEWARRAYUT : OpCodeBase
{

    protected override VM.OpCode Opcode => VM.OpCode.NEWARRAY_T;
    protected override InstructionBuilder CreateBaseLineScript()
    {
        var builder = new InstructionBuilder();
        // builder.Push(ushort.MaxValue*2);
        // builder.AddInstruction(VM.OpCode.NEWBUFFER);
        builder.Push(0);
        builder.Push(ItemCount);
        return builder;
    }

    protected override byte[] CreateOneOpCodeScript(ref InstructionBuilder builder)
    {
        builder.AddInstruction(Opcode);
        return builder.ToArray();
    }

    protected override byte[] CreateOneGASScript(InstructionBuilder builder)
    {
        throw new NotImplementedException();
    }
}

//0
// | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
//     |---------------- |---------- |---------:|---------:|---------:|---------:|
//     | Bench_OneOpCode | 1         | 21.06 us | 0.420 us | 1.031 us | 20.80 us |
//     | Bench_OneOpCode | 32        | 21.56 us | 0.668 us | 1.771 us | 21.00 us |
//     | Bench_OneOpCode | 128       | 20.24 us | 0.390 us | 0.596 us | 20.10 us |
//     | Bench_OneOpCode | 1024      | 23.98 us | 1.818 us | 5.362 us | 20.95 us |
//     | Bench_OneOpCode | 2040      | 20.93 us | 0.444 us | 1.192 us | 20.70 us |

// buffer
// | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
//     |---------------- |---------- |---------:|---------:|---------:|---------:|
//     | Bench_OneOpCode | 1         | 22.17 us | 0.798 us | 2.158 us | 21.40 us |
//     | Bench_OneOpCode | 32        | 20.92 us | 0.419 us | 0.733 us | 20.80 us |
//     | Bench_OneOpCode | 128       | 20.84 us | 0.413 us | 0.795 us | 20.75 us |
//     | Bench_OneOpCode | 1024      | 20.74 us | 0.415 us | 0.726 us | 20.60 us |
//     | Bench_OneOpCode | 2040      | 23.74 us | 1.764 us | 5.117 us | 21.10 us |


