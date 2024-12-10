// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.REVERSEITEMS.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_REVERSEITEMS : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.REVERSEITEMS;

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
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(Opcode);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

//     | Method          | ItemCount | Mean         | Error       | StdDev      | Median       |
//     |---------------- |---------- |-------------:|------------:|------------:|-------------:|
//     | Bench_OneOpCode | 2         |     1.655 us |   0.0487 us |   0.1325 us |     1.600 us |
//     | Bench_OneGAS    | 2         | 1,351.479 us |  26.7038 us |  42.3550 us | 1,353.700 us |
//     | Bench_OneOpCode | 32        |     2.369 us |   0.1228 us |   0.3602 us |     2.400 us |
//     | Bench_OneGAS    | 32        | 1,622.432 us |  31.9275 us |  54.2153 us | 1,626.200 us |
//     | Bench_OneOpCode | 128       |     3.741 us |   0.2807 us |   0.8055 us |     3.700 us |
//     | Bench_OneGAS    | 128       | 1,604.499 us | 309.4976 us | 912.5608 us | 1,016.350 us |
//     | Bench_OneOpCode | 1024      |     4.708 us |   0.5489 us |   1.5837 us |     4.400 us |
//     | Bench_OneGAS    | 1024      | 5,026.131 us |  99.7594 us |  97.9771 us | 5,003.600 us |
//     | Bench_OneOpCode | 2040      |     6.045 us |   0.6570 us |   1.9166 us |     5.400 us |
//     | Bench_OneGAS    | 2040      | 9,734.672 us | 192.4691 us | 243.4118 us | 9,695.350 us |
