// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PICK.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_PICK : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.PICK;

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

            builder.Push(ItemCount - 1);
            builder.AddInstruction(Opcode);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
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

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.Push(ItemCount - 1);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error          | StdDev         |
/// |---------------- |---------- |---------------:|---------------:|---------------:|
/// | Bench_OneOpCode | 4         |       2.000 us |      0.1860 us |      0.5338 us |
/// | Bench_OneGAS    | 4         | 579,425.887 us | 10,936.7302 us | 10,741.3314 us |
/// | Bench_OneOpCode | 8         |       2.097 us |      0.1784 us |      0.5175 us |
/// | Bench_OneGAS    | 8         | 567,562.490 us |  5,999.8416 us |  5,612.2556 us |
/// | Bench_OneOpCode | 16        |       2.132 us |      0.1597 us |      0.4710 us |
/// | Bench_OneGAS    | 16        | 579,255.321 us |  7,734.9779 us |  6,856.8565 us |
/// | Bench_OneOpCode | 32        |       2.486 us |      0.1863 us |      0.5223 us |
/// | Bench_OneGAS    | 32        | 622,212.292 us |  6,718.7233 us |  5,610.4382 us |
/// | Bench_OneOpCode | 64        |       2.646 us |      0.2568 us |      0.7490 us |
/// | Bench_OneGAS    | 64        | 595,508.173 us |  8,101.9014 us |  7,578.5236 us |
/// | Bench_OneOpCode | 128       |       3.043 us |      0.3620 us |      1.0446 us |
/// | Bench_OneGAS    | 128       | 585,245.413 us |  7,432.6460 us |  6,952.5017 us |
/// | Bench_OneOpCode | 256       |       2.237 us |      0.3377 us |      0.9743 us |
/// | Bench_OneGAS    | 256       | 611,472.827 us |  7,812.6213 us |  7,307.9308 us |
/// | Bench_OneOpCode | 512       |       3.645 us |      0.5955 us |      1.7558 us |
/// | Bench_OneGAS    | 512       | 613,935.146 us |  7,359.1517 us |  6,145.2249 us |
/// | Bench_OneOpCode | 1024      |       3.598 us |      0.4337 us |      1.2583 us |
/// | Bench_OneGAS    | 1024      | 603,081.820 us | 10,936.9450 us | 10,230.4251 us |
/// | Bench_OneOpCode | 2040      |       2.491 us |      0.3579 us |      1.0498 us |
/// | Bench_OneGAS    | 2040      | 602,212.573 us |  4,152.2401 us |  3,884.0079 us |
