// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PACKMAP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_PACKMAP : OpCodeBase
{
    protected override VM.OpCode Opcode => VM.OpCode.PACKMAP;

    protected override byte[] CreateOneOpCodeScript()
    {
        var builder = new InstructionBuilder();
        var initBegin = new JumpTarget();
        builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.STLOC0);
        initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
        builder.Push(ushort.MaxValue * 2 - ItemCount);
        builder.AddInstruction(VM.OpCode.NEWBUFFER);
        // builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.AddInstruction(VM.OpCode.DEC);
        builder.AddInstruction(VM.OpCode.STLOC0);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.Jump(VM.OpCode.JMPIF, initBegin);
        builder.Push(ItemCount);
        builder.AddInstruction(Opcode);
        return builder.ToArray();
    }

    protected override byte[] CreateOneGASScript(InstructionBuilder builder)
    {
        throw new NotImplementedException();
    }
}


// for 0
// | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
//     |---------------- |---------- |---------:|---------:|---------:|---------:|
//     | Bench_OneOpCode | 1         | 12.93 us | 0.899 us | 2.521 us | 11.80 us |
//     | Bench_OneOpCode | 32        | 14.15 us | 1.164 us | 3.413 us | 12.40 us |
//     | Bench_OneOpCode | 128       | 12.62 us | 0.772 us | 2.139 us | 11.90 us |
//     | Bench_OneOpCode | 1024      | 15.44 us | 1.043 us | 3.059 us | 14.00 us |
//     | Bench_OneOpCode | 2040      | 17.00 us | 0.924 us | 2.723 us | 17.05 us |

// for ushor.max * 2

// | Method          | ItemCount | Mean     | Error    | StdDev   | Median   |
//     |---------------- |---------- |---------:|---------:|---------:|---------:|
//     | Bench_OneOpCode | 1         | 13.79 us | 1.081 us | 3.188 us | 12.25 us |
//     | Bench_OneOpCode | 32        | 16.71 us | 0.742 us | 2.177 us | 16.40 us |
//     | Bench_OneOpCode | 128       | 19.92 us | 0.812 us | 2.343 us | 19.75 us |
//     | Bench_OneOpCode | 1024      | 36.31 us | 1.379 us | 3.867 us | 36.00 us |
//     | Bench_OneOpCode | 2040      | 37.46 us | 2.004 us | 5.782 us | 36.45 us |
