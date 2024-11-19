// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PACK.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_PACK : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.PACK;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
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
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript(InstructionBuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    // | Method          | ItemCount | Mean      | Error     | StdDev    | Median    |
    //     |---------------- |---------- |----------:|----------:|----------:|----------:|
    //     | Bench_OneOpCode | 1         |  2.783 us | 0.0543 us | 0.1269 us |  2.800 us |
    //     | Bench_OneOpCode | 32        |  4.376 us | 0.0962 us | 0.2517 us |  4.300 us |
    //     | Bench_OneOpCode | 128       |  7.482 us | 0.1429 us | 0.1468 us |  7.500 us |
    //     | Bench_OneOpCode | 1024      | 41.694 us | 0.8339 us | 1.6846 us | 41.800 us |
    //     | Bench_OneOpCode | 2040      | 60.055 us | 1.1531 us | 1.4161 us | 60.450 us |

    // for buffer: size ushort.max*2

    // | Method          | ItemCount | Mean       | Error      | StdDev     | Median     |
    //     |---------------- |---------- |-----------:|-----------:|-----------:|-----------:|
    //     | Bench_OneOpCode | 1         |   3.231 us |  0.1004 us |  0.2627 us |   3.100 us |
    //     | Bench_OneOpCode | 32        |   9.148 us |  0.2127 us |  0.6034 us |   9.100 us |
    //     | Bench_OneOpCode | 128       |  26.184 us |  2.2478 us |  6.6276 us |  21.400 us |
    //     | Bench_OneOpCode | 1024      |  81.564 us |  2.6738 us |  7.2287 us |  80.000 us |
    //     | Bench_OneOpCode | 2040      | 198.662 us | 31.1163 us | 90.7677 us | 149.500 us |
}


// |---------------- |---------- |----------:|----------:|-----------:|----------:|
//     | Bench_OneOpCode | 2         |  2.829 us | 0.0710 us |  0.1920 us |  2.800 us |
//     | Bench_OneOpCode | 32        |  5.847 us | 0.2570 us |  0.7373 us |  5.600 us |
//     | Bench_OneOpCode | 128       | 14.698 us | 1.1027 us |  3.1816 us | 13.600 us |
//     | Bench_OneOpCode | 1024      | 27.040 us | 2.3996 us |  6.9233 us | 28.700 us |
//     | Bench_OneOpCode | 2040      | 46.012 us | 4.1584 us | 11.3133 us | 44.350 us |




// | Method          | ItemCount | Mean      | Error    | StdDev    |
//     |---------------- |---------- |----------:|---------:|----------:|
//     | Bench_OneOpCode | 1         |  57.70 ms | 1.150 ms |  2.475 ms |
//     | Bench_OneOpCode | 2         | 100.87 ms | 2.015 ms |  3.530 ms |
//     | Bench_OneOpCode | 32        | 133.31 ms | 4.967 ms | 14.644 ms |
//     | Bench_OneOpCode | 128       | 324.87 ms | 6.387 ms | 12.755 ms |
//     | Bench_OneOpCode | 1024      | 156.63 ms | 3.842 ms | 11.328 ms |
//     | Bench_OneOpCode | 2040      | 120.45 ms | 2.008 ms |  1.879 ms |
