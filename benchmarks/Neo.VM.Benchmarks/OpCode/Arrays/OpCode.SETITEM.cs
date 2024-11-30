// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SETITEM.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_SETITEM : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.SETITEM;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWARRAY);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.Push(ItemCount / 2);
            builder.Push(0);
            builder.AddInstruction(VM.OpCode.SETITEM);
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
                builder.Push(ItemCount);
                builder.AddInstruction(VM.OpCode.NEWARRAY);
                var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
                builder.Push(ItemCount);
                builder.AddInstruction(VM.OpCode.STLOC0);
                initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
                builder.AddInstruction(VM.OpCode.DUP);
                builder.Push(ItemCount/2);
                builder.Push(0);
                builder.AddInstruction(VM.OpCode.SETITEM);
                builder.AddInstruction(VM.OpCode.LDLOC0);
                builder.AddInstruction(VM.OpCode.DEC);
                builder.AddInstruction(VM.OpCode.STLOC0);
                builder.AddInstruction(VM.OpCode.LDLOC0);
                builder.Jump(VM.OpCode.JMPIF, initBegin);
                builder.Jump(VM.OpCode.JMP, loopBegin);
                return builder.ToArray();
        }
    }
}


//     | Method          | ItemCount | Mean         | Error       | StdDev      | Median       |
//     |---------------- |---------- |-------------:|------------:|------------:|-------------:|
//     | Bench_OneOpCode | 2         |     1.794 us |   0.0671 us |   0.1731 us |     1.800 us |
//     | Bench_OneGAS    | 2         | 5,674.588 us | 110.5469 us | 175.3391 us | 5,653.100 us |
//     | Bench_OneOpCode | 32        |     1.630 us |   0.0551 us |   0.1479 us |     1.600 us |
//     | Bench_OneGAS    | 32        | 4,512.892 us |  88.7166 us | 118.4341 us | 4,481.100 us |
//     | Bench_OneOpCode | 128       |     1.677 us |   0.0692 us |   0.1787 us |     1.650 us |
//     | Bench_OneGAS    | 128       | 4,621.733 us |  91.9366 us | 119.5435 us | 4,595.000 us |
//     | Bench_OneOpCode | 1024      |     1.661 us |   0.0473 us |   0.1255 us |     1.600 us |
//     | Bench_OneGAS    | 1024      | 4,687.990 us |  92.6155 us | 141.4337 us | 4,680.500 us |
//     | Bench_OneOpCode | 2040      |     2.290 us |   0.2473 us |   0.7293 us |     1.900 us |
//     | Bench_OneGAS    | 2040      | 4,583.771 us |  69.5027 us |  61.6123 us | 4,590.250 us |
