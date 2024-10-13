// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.CLEARITEMS.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_CLEARITEMS : OpCodeBase
{
    protected override VM.OpCode Opcode => VM.OpCode.CLEARITEMS;

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
        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.PACK);
        builder.AddInstruction(VM.OpCode.DUP);
        builder.AddInstruction(Opcode);
        return builder.ToArray();
    }

    protected override byte[] CreateOneGASScript(InstructionBuilder builder)
    {
        throw new NotImplementedException();
    }
}


// 0
// | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
//     |---------------- |---------- |----------:|----------:|----------:|----------:|
//     | Bench_OneOpCode | 1         |  1.285 us | 0.0368 us | 0.1000 us |  1.300 us |
//     | Bench_OneOpCode | 32        |  2.140 us | 0.1974 us | 0.5821 us |  1.800 us |
//     | Bench_OneOpCode | 128       |  3.188 us | 0.0654 us | 0.0850 us |  3.200 us |
//     | Bench_OneOpCode | 1024      | 18.344 us | 0.4455 us | 1.2711 us | 18.150 us |
//     | Bench_OneOpCode | 2040      | 21.559 us | 0.5000 us | 1.4507 us | 21.300 us |

// ushort.max *2
// | Method          | ItemCount | Mean      | Error     | StdDev     | Median    |
//     |---------------- |---------- |----------:|----------:|-----------:|----------:|
//     | Bench_OneOpCode | 1         |  1.771 us | 0.0460 us |  0.1297 us |  1.700 us |
//     | Bench_OneOpCode | 32        |  3.852 us | 0.1563 us |  0.4583 us |  3.800 us |
//     | Bench_OneOpCode | 128       |  9.921 us | 0.8772 us |  2.5588 us |  8.750 us |
//     | Bench_OneOpCode | 1024      | 29.629 us | 3.6489 us | 10.1110 us | 34.700 us |
//     | Bench_OneOpCode | 2040      | 49.573 us | 5.8885 us | 16.5120 us | 49.800 us |
