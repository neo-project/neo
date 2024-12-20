// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.TUCK.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_TUCK : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.TUCK;

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
            builder.AddInstruction(VM.OpCode.PACK);
            builder.AddInstruction(VM.OpCode.DUP);
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
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.PACK);
            builder.AddInstruction(VM.OpCode.DUP);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        |
/// |---------------- |---------- |---------------:|--------------:|--------------:|
/// | Bench_OneOpCode | 4         |       1.545 us |     0.0828 us |     0.2336 us |
/// | Bench_OneGAS    | 4         | 457,338.067 us | 4,663.8474 us | 4,362.5657 us |
/// | Bench_OneOpCode | 8         |       1.931 us |     0.1532 us |     0.4516 us |
/// | Bench_OneGAS    | 8         | 460,598.029 us | 3,742.8600 us | 3,317.9479 us |
/// | Bench_OneOpCode | 16        |       2.012 us |     0.1266 us |     0.3732 us |
/// | Bench_OneGAS    | 16        | 467,061.447 us | 4,553.1478 us | 4,259.0173 us |
/// | Bench_OneOpCode | 32        |       2.335 us |     0.1521 us |     0.4363 us |
/// | Bench_OneGAS    | 32        | 457,109.850 us | 3,799.6440 us | 3,368.2854 us |
/// | Bench_OneOpCode | 64        |       2.179 us |     0.1685 us |     0.4835 us |
/// | Bench_OneGAS    | 64        | 452,048.163 us | 3,608.6281 us | 3,375.5130 us |
/// | Bench_OneOpCode | 128       |       2.573 us |     0.3168 us |     0.9291 us |
/// | Bench_OneGAS    | 128       | 451,358.386 us | 3,976.5519 us | 3,525.1097 us |
/// | Bench_OneOpCode | 256       |       2.949 us |     0.3639 us |     1.0614 us |
/// | Bench_OneGAS    | 256       | 461,951.486 us | 2,692.6532 us | 2,386.9669 us |
/// | Bench_OneOpCode | 512       |       2.735 us |     0.2671 us |     0.7750 us |
/// | Bench_OneGAS    | 512       | 464,112.033 us | 2,913.3354 us | 2,725.1358 us |
/// | Bench_OneOpCode | 1024      |       3.076 us |     0.2568 us |     0.7409 us |
/// | Bench_OneGAS    | 1024      | 462,900.927 us | 4,775.3663 us | 4,466.8806 us |
/// | Bench_OneOpCode | 2040      |       2.565 us |     0.2920 us |     0.8517 us |
/// | Bench_OneGAS    | 2040      | 457,070.423 us | 1,803.5550 us | 1,506.0501 us |
