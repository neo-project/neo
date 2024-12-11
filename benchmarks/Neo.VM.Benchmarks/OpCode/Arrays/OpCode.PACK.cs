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
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
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
    }
}


//     | Method       | ItemCount | Mean       | Error     | StdDev    | Median     |
//     |------------- |---------- |-----------:|----------:|----------:|-----------:|
//     | Bench_OneGAS | 4         |   211.7 ms |   4.03 ms |   4.14 ms |   212.3 ms |
//     | Bench_OneGAS | 8         |   438.8 ms |   8.65 ms |  16.87 ms |   436.4 ms |
//     | Bench_OneGAS | 16        |   806.1 ms |  26.74 ms |  77.56 ms |   813.8 ms |
//     | Bench_OneGAS | 32        | 1,991.2 ms |  87.72 ms | 258.63 ms | 1,962.8 ms |
//     | Bench_OneGAS | 64        | 2,509.9 ms | 132.85 ms | 391.70 ms | 2,518.2 ms |
//     | Bench_OneGAS | 128       | 2,570.8 ms | 138.76 ms | 409.14 ms | 2,510.1 ms |
//     | Bench_OneGAS | 256       | 2,221.8 ms | 117.59 ms | 343.00 ms | 2,097.9 ms |
//     | Bench_OneGAS | 1024      | 1,712.6 ms | 112.45 ms | 331.56 ms | 1,713.3 ms |
//     | Bench_OneGAS | 2040      | 1,679.2 ms |  80.55 ms | 234.98 ms | 1,694.8 ms |


//     | Method       | ItemCount | Mean     | Error    | StdDev    |
//     |------------- |---------- |---------:|---------:|----------:|
//     | Bench_OneGAS | 4         | 211.9 ms |  4.13 ms |   4.24 ms |
//     | Bench_OneGAS | 8         | 464.5 ms |  9.14 ms |  25.32 ms |
//     | Bench_OneGAS | 16        | 808.3 ms | 28.21 ms |  83.17 ms |
//     | Bench_OneGAS | 32        | 135.2 ms |  7.29 ms |  21.37 ms |
//     | Bench_OneGAS | 64        | 254.2 ms | 16.88 ms |  49.77 ms |
//     | Bench_OneGAS | 128       | 383.6 ms | 28.51 ms |  84.08 ms |
//     | Bench_OneGAS | 256       | 508.7 ms | 35.22 ms | 103.29 ms |
//     | Bench_OneGAS | 512       | 703.4 ms | 47.16 ms | 139.05 ms |
//     | Bench_OneGAS | 1024      | 879.6 ms | 55.22 ms | 161.09 ms |
//     | Bench_OneGAS | 2040      | 811.7 ms | 23.07 ms |  67.65 ms |


// | Method       | ItemCount | Mean     | Error    | StdDev    |
//     |------------- |---------- |---------:|---------:|----------:|
//     | Bench_OneGAS | 4         | 213.3 ms |  4.15 ms |   3.88 ms |
//     | Bench_OneGAS | 8         | 447.0 ms |  8.92 ms |   9.91 ms |
//     | Bench_OneGAS | 16        | 248.6 ms |  8.97 ms |  26.32 ms |
//     | Bench_OneGAS | 32        | 520.8 ms | 22.70 ms |  66.93 ms |
//     | Bench_OneGAS | 64        | 656.0 ms | 40.10 ms | 116.96 ms |
//     | Bench_OneGAS | 128       | 574.1 ms | 38.14 ms | 112.44 ms |
//     | Bench_OneGAS | 256       | 526.4 ms | 46.25 ms | 135.65 ms |
//     | Bench_OneGAS | 512       | 435.6 ms | 28.42 ms |  83.35 ms |
//     | Bench_OneGAS | 1024      | 465.7 ms | 34.07 ms |  99.37 ms |
//     | Bench_OneGAS | 2040      | 282.0 ms |  5.44 ms |   9.38 ms |
