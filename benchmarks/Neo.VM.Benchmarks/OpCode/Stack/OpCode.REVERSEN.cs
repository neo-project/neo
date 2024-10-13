// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.REVERSEN.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_REVERSEN : OpCodeBase
{
    protected override VM.OpCode Opcode => VM.OpCode.REVERSEN;

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
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.AddInstruction(VM.OpCode.DEC);
        builder.AddInstruction(VM.OpCode.STLOC0);
        builder.AddInstruction(VM.OpCode.LDLOC0);
        builder.Jump(VM.OpCode.JMPIF, initBegin);
        builder.Push(ItemCount);
        builder.AddInstruction(VM.OpCode.REVERSEN);
        return builder.ToArray();
    }

    protected override byte[] CreateOneGASScript(InstructionBuilder builder)
    {
        throw new NotImplementedException();
    }
}

// | Method          | ItemCount | Mean     | Error     | StdDev    | Median   |
//     |---------------- |---------- |---------:|----------:|----------:|---------:|
//     | Bench_OneOpCode | 1         | 1.089 us | 0.0546 us | 0.1457 us | 1.100 us |
//     | Bench_OneOpCode | 32        | 1.344 us | 0.0446 us | 0.1214 us | 1.300 us |
//     | Bench_OneOpCode | 128       | 1.540 us | 0.0532 us | 0.1393 us | 1.500 us |
//     | Bench_OneOpCode | 1024      | 3.968 us | 0.1582 us | 0.4614 us | 3.800 us |
//     | Bench_OneOpCode | 2040      | 6.327 us | 0.1916 us | 0.5620 us | 6.200 us |


// | Method          | ItemCount | Mean     | Error     | StdDev    |
//     |---------------- |---------- |---------:|----------:|----------:|
//     | Bench_OneOpCode | 2         | 1.285 us | 0.0383 us | 0.1061 us |
//     | Bench_OneOpCode | 32        | 2.029 us | 0.1094 us | 0.3208 us |
//     | Bench_OneOpCode | 128       | 2.709 us | 0.1647 us | 0.4779 us |
//     | Bench_OneOpCode | 1024      | 4.553 us | 0.6520 us | 1.9121 us |
//     | Bench_OneOpCode | 2040      | 6.112 us | 0.6347 us | 1.8715 us |
