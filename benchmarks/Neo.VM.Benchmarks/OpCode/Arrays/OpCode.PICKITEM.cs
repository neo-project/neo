// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PICKITEM.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_PICKITEM : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.PICKITEM;



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

            builder.Push(new BigInteger(0));
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
            builder.Push(new BigInteger(0));
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean          | Error         | StdDev        | Median        |
/// |---------------- |---------- |--------------:|--------------:|--------------:|--------------:|
/// | Bench_OneOpCode | 4         |      2.913 us |     0.0986 us |     0.2798 us |      2.900 us |
/// | Bench_OneGAS    | 4         | 71,912.345 us | 1,284.5414 us | 1,961.6306 us | 71,412.700 us |
/// | Bench_OneOpCode | 8         |      3.084 us |     0.0875 us |     0.2482 us |      3.100 us |
/// | Bench_OneGAS    | 8         | 72,587.154 us | 1,101.3158 us |   919.6485 us | 72,799.900 us |
/// | Bench_OneOpCode | 16        |      3.600 us |     0.1373 us |     0.3938 us |      3.500 us |
/// | Bench_OneGAS    | 16        | 71,937.327 us | 1,104.4270 us | 1,033.0817 us | 71,956.300 us |
/// | Bench_OneOpCode | 32        |      4.352 us |     0.1478 us |     0.4240 us |      4.300 us |
/// | Bench_OneGAS    | 32        | 70,200.475 us |   365.4561 us |   285.3241 us | 70,190.100 us |
/// | Bench_OneOpCode | 64        |      5.726 us |     0.2459 us |     0.7094 us |      5.600 us |
/// | Bench_OneGAS    | 64        | 71,708.813 us | 1,210.9946 us | 1,132.7651 us | 71,528.400 us |
/// | Bench_OneOpCode | 128       |      6.753 us |     0.9665 us |     2.7887 us |      7.700 us |
/// | Bench_OneGAS    | 128       | 70,837.500 us |   826.4024 us |   732.5842 us | 70,733.900 us |
/// | Bench_OneOpCode | 256       |      9.413 us |     2.1033 us |     6.2017 us |      5.050 us |
/// | Bench_OneGAS    | 256       | 71,808.200 us | 1,393.4335 us | 1,430.9537 us | 71,253.600 us |
/// | Bench_OneOpCode | 512       |      5.431 us |     0.5685 us |     1.5467 us |      5.200 us |
/// | Bench_OneGAS    | 512       | 72,296.917 us | 1,427.1765 us | 1,334.9818 us | 72,720.050 us |
/// | Bench_OneOpCode | 1024      |      6.528 us |     0.4585 us |     1.2552 us |      6.300 us |
/// | Bench_OneGAS    | 1024      | 73,804.593 us | 1,447.1079 us | 1,282.8235 us | 73,699.100 us |
/// | Bench_OneOpCode | 2040      |     11.297 us |     0.7711 us |     2.1999 us |     11.050 us |
/// | Bench_OneGAS    | 2040      | 71,122.007 us | 1,000.4552 us |   886.8775 us | 71,063.750 us |
