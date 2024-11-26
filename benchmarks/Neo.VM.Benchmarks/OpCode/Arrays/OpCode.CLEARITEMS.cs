// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.CLEARITEMS.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_CLEARITEMS : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.CLEARITEMS;

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

        protected override byte[] CreateOneGASScript( )
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
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

    // | Method          | ItemCount | Mean             | Error           | StdDev          | Median           |
    // |---------------- |---------- |-----------------:|----------------:|----------------:|-----------------:|
    // | Bench_OneOpCode | 2         |         2.582 us |       0.1131 us |       0.3154 us |         2.500 us |
    // | Bench_OneGAS    | 2         | 1,008,326.260 us |  19,946.4808 us |  49,302.7585 us | 1,012,292.300 us |
    // | Bench_OneOpCode | 32        |         4.115 us |       0.1515 us |       0.4370 us |         4.100 us |
    // | Bench_OneGAS    | 32        | 2,011,062.912 us |  77,177.9314 us | 227,560.8721 us | 1,960,167.600 us |
    // | Bench_OneOpCode | 128       |         9.494 us |       0.3439 us |       0.9812 us |         9.600 us |
    // | Bench_OneGAS    | 128       | 2,433,229.635 us | 163,868.4023 us | 483,169.6814 us | 2,398,037.650 us |
    // | Bench_OneOpCode | 1024      |        23.914 us |       5.6959 us |      16.7050 us |        34.000 us |
    // | Bench_OneGAS    | 1024      | 1,738,123.511 us | 111,164.9318 us | 327,772.3096 us | 1,778,298.650 us |
    // | Bench_OneOpCode | 2040      |        30.093 us |       6.0103 us |      17.5325 us |        38.750 us |
    // | Bench_OneGAS    | 2040      | 1,660,719.746 us |  62,732.2144 us | 183,982.7256 us | 1,678,398.200 us |
