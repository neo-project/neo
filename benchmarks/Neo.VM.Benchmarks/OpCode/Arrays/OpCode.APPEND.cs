// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.APPEND.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_APPEND : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.APPEND;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.AddInstruction(VM.OpCode.NEWARRAY0);
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);

            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript( )
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.AddInstruction(VM.OpCode.NEWARRAY0);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);

            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);

            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(VM.OpCode.CLEARITEMS);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }
    }
}

//     | Method          | ItemCount | Mean           | Error         | StdDev         | Median         |
//     |---------------- |---------- |---------------:|--------------:|---------------:|---------------:|
//     | Bench_OneOpCode | 2         |       2.243 us |     0.0951 us |      0.2538 us |       2.200 us |
//     | Bench_OneGAS    | 2         |  21,094.679 us |   364.2267 us |    784.0363 us |  20,909.700 us |
//     | Bench_OneOpCode | 32        |       1.886 us |     0.0416 us |      0.1088 us |       1.900 us |
//     | Bench_OneGAS    | 32        |  53,720.466 us | 3,006.1763 us |  8,863.7786 us |  52,817.050 us |
//     | Bench_OneOpCode | 128       |       1.998 us |     0.0417 us |      0.0879 us |       2.000 us |
//     | Bench_OneGAS    | 128       | 106,883.038 us | 4,400.3383 us | 12,625.3953 us | 106,513.700 us |
//     | Bench_OneOpCode | 1024      |       2.006 us |     0.0416 us |      0.1073 us |       2.000 us |
//     | Bench_OneGAS    | 1024      |  76,923.633 us | 1,534.4009 us |  2,919.3571 us |  76,860.800 us |
//     | Bench_OneOpCode | 2040      |       1.936 us |     0.0421 us |      0.0879 us |       1.900 us |
//     | Bench_OneGAS    | 2040      |  64,089.405 us | 1,238.9347 us |  1,377.0726 us |  64,310.000 us |
