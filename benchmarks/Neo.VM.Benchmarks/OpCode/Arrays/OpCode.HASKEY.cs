// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.HASKEY.cs file belongs to the neo project and is free
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
    public class OpCode_HASKEY : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.HASKEY;

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

            builder.Push(ItemCount);// push the number of array items
            builder.AddInstruction(VM.OpCode.PACK); // pack itemcount items as an array

            builder.Push(new BigInteger(0)); // the index
            builder.AddInstruction(Opcode);  // HASKEY

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

            builder.Push(ItemCount);// push the number of array items
            builder.AddInstruction(VM.OpCode.PACK); // pack itemcount items as an array

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) }; // now deadloop until you reach 1 GAS
            builder.AddInstruction(VM.OpCode.DUP);
            builder.Push(new BigInteger(0)); // the index
            builder.AddInstruction(Opcode);  // HASKEY
            builder.AddInstruction(VM.OpCode.DROP); // Drop the haskey result to clear the stack
            builder.Jump(VM.OpCode.JMP, loopBegin); // loop
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean          | Error         | StdDev        | Median        |
/// |---------------- |---------- |--------------:|--------------:|--------------:|--------------:|
/// | Bench_OneOpCode | 4         |      3.281 us |     0.1592 us |     0.4594 us |      3.200 us |
/// | Bench_OneGAS    | 4         | 72,665.600 us | 1,171.0190 us | 1,095.3719 us | 72,533.200 us |
/// | Bench_OneOpCode | 8         |      3.404 us |     0.1509 us |     0.4305 us |      3.300 us |
/// | Bench_OneGAS    | 8         | 73,338.887 us | 1,337.2319 us | 1,250.8475 us | 72,975.200 us |
/// | Bench_OneOpCode | 16        |      3.993 us |     0.1712 us |     0.4967 us |      3.900 us |
/// | Bench_OneGAS    | 16        | 73,448.600 us | 1,412.2191 us | 1,681.1465 us | 72,856.900 us |
/// | Bench_OneOpCode | 32        |      4.548 us |     0.1744 us |     0.5004 us |      4.500 us |
/// | Bench_OneGAS    | 32        | 71,313.640 us | 1,156.6631 us | 1,081.9434 us | 71,279.300 us |
/// | Bench_OneOpCode | 64        |      6.204 us |     0.3064 us |     0.8742 us |      6.150 us |
/// | Bench_OneGAS    | 64        | 71,925.185 us | 1,001.9698 us |   836.6901 us | 72,058.200 us |
/// | Bench_OneOpCode | 128       |      7.931 us |     1.0725 us |     3.1453 us |      8.800 us |
/// | Bench_OneGAS    | 128       | 73,325.780 us | 1,161.4638 us | 1,086.4340 us | 73,661.000 us |
/// | Bench_OneOpCode | 256       |      9.272 us |     1.9466 us |     5.7397 us |      6.250 us |
/// | Bench_OneGAS    | 256       | 72,817.990 us | 1,155.0164 us | 1,080.4031 us | 73,065.850 us |
/// | Bench_OneOpCode | 512       |      6.177 us |     0.6341 us |     1.7357 us |      5.800 us |
/// | Bench_OneGAS    | 512       | 74,204.424 us | 1,475.0676 us | 2,162.1351 us | 74,178.300 us |
/// | Bench_OneOpCode | 1024      |      6.474 us |     0.3918 us |     1.0988 us |      6.600 us |
/// | Bench_OneGAS    | 1024      | 73,362.183 us | 1,432.1909 us | 1,532.4282 us | 73,365.550 us |
/// | Bench_OneOpCode | 2040      |     11.054 us |     0.6791 us |     1.9701 us |     10.600 us |
/// | Bench_OneGAS    | 2040      | 71,487.580 us | 1,120.5841 us | 1,048.1951 us | 70,965.200 us |
