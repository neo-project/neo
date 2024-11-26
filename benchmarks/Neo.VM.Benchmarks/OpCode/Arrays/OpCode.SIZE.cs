// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SIZE.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_SIZE : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.SIZE;

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
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

//     | Method          | ItemCount | Mean           | Error         | StdDev        |
//     |---------------- |---------- |---------------:|--------------:|--------------:|
//     | Bench_OneOpCode | 2         |       2.403 us |     0.0507 us |     0.1134 us |
//     | Bench_OneGAS    | 2         | 363,828.707 us | 4,419.9836 us | 4,134.4554 us |
//     | Bench_OneOpCode | 32        |       4.573 us |     0.2436 us |     0.7181 us |
//     | Bench_OneGAS    | 32        | 364,788.813 us | 4,012.2529 us | 3,753.0638 us |
//     | Bench_OneOpCode | 128       |       8.753 us |     0.3861 us |     1.1141 us |
//     | Bench_OneGAS    | 128       | 368,191.967 us | 4,469.7951 us | 4,181.0491 us |
//     | Bench_OneOpCode | 1024      |       7.992 us |     0.3953 us |     1.0620 us |
//     | Bench_OneGAS    | 1024      | 363,934.540 us | 5,189.3791 us | 4,854.1485 us |
//     | Bench_OneOpCode | 2040      |      12.813 us |     0.3424 us |     0.9714 us |
//     | Bench_OneGAS    | 2040      | 364,057.107 us | 4,586.6919 us | 4,290.3944 us |

