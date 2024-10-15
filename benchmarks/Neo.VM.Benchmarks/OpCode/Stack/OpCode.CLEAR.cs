// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.CLEAR.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_CLEAR : OpCodeBase
{
    protected override VM.OpCode Opcode => VM.OpCode.CLEAR;

    protected override byte[] CreateOneOpCodeScript()
    {
        var builder = new InstructionBuilder();
        var initBegin = new JumpTarget();
        builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.STLOC0);
        initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
        builder.Push(ushort.MaxValue * 2);
        builder.AddInstruction(VM.OpCode.NEWBUFFER);
        // builder.Push(0);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.AddInstruction(VM.OpCode.DEC);
        builder.AddInstruction(VM.OpCode.STLOC0);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.Jump(VM.OpCode.JMPIF, initBegin);

        builder.AddInstruction(VM.OpCode.CLEAR);
        return builder.ToArray();
    }

    protected override byte[] CreateOneGASScript(InstructionBuilder builder)
    {
        throw new NotImplementedException();
    }
}


// max buffer
//     | Method          | ItemCount | Mean      | Error      | StdDev     | Median    |
//     |---------------- |---------- |----------:|-----------:|-----------:|----------:|
//     | Bench_OneOpCode | 2         |  2.011 us |  0.0697 us |  0.1884 us |  2.000 us |
//     | Bench_OneOpCode | 32        |  4.488 us |  0.1793 us |  0.5231 us |  4.400 us |
//     | Bench_OneOpCode | 128       |  9.763 us |  0.6970 us |  2.0551 us |  9.000 us |
//     | Bench_OneOpCode | 1024      | 34.498 us |  3.7138 us | 10.2289 us | 38.500 us |
//     | Bench_OneOpCode | 2040      | 79.396 us | 10.8171 us | 31.7247 us | 69.900 us |

// 0
// | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
//     |---------------- |---------- |----------:|----------:|----------:|----------:|
//     | Bench_OneOpCode | 2         |  1.319 us | 0.1278 us | 0.3727 us |  1.200 us |
//     | Bench_OneOpCode | 32        |  1.527 us | 0.0290 us | 0.0672 us |  1.500 us |
//     | Bench_OneOpCode | 128       |  3.282 us | 0.1186 us | 0.3365 us |  3.300 us |
//     | Bench_OneOpCode | 1024      | 18.108 us | 0.4129 us | 1.1980 us | 18.000 us |
//     | Bench_OneOpCode | 2040      | 21.722 us | 0.4409 us | 1.2930 us | 21.800 us |
