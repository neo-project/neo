// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.DEPTH.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_DEPTH : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.DEPTH;

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
            builder.AddInstruction(VM.OpCode.DEPTH);
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
            // builder.Push(0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(VM.OpCode.DEPTH);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
//     |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
//     | Bench_OneOpCode | 2         |       1.669 us |     0.0373 us |     0.0983 us |       1.700 us |
//     | Bench_OneGAS    | 2         | 413,624.013 us | 5,411.3628 us | 5,061.7921 us | 413,034.900 us |
//     | Bench_OneOpCode | 32        |       2.615 us |     0.1709 us |     0.4985 us |       2.700 us |
//     | Bench_OneGAS    | 32        | 407,138.293 us | 6,059.3635 us | 5,667.9324 us | 407,188.600 us |
//     | Bench_OneOpCode | 128       |       4.305 us |     0.5123 us |     1.5025 us |       3.900 us |
//     | Bench_OneGAS    | 128       | 425,708.095 us | 8,156.7126 us | 9,709.9861 us | 423,600.200 us |
//     | Bench_OneOpCode | 1024      |       4.142 us |     0.3501 us |     0.9874 us |       4.300 us |
//     | Bench_OneGAS    | 1024      | 420,758.107 us | 5,477.4237 us | 5,123.5855 us | 419,661.400 us |
//     | Bench_OneOpCode | 2040      |       3.596 us |     0.4322 us |     1.2401 us |       3.700 us |
//     | Bench_OneGAS    | 2040      | 418,418.767 us | 6,546.9716 us | 6,124.0413 us | 420,333.700 us |
