// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.ROT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_ROT : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.ROT;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(VM.OpCode.DUP);

            builder.AddInstruction(Opcode);

            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(ushort.MaxValue * 2);
            builder.AddInstruction(VM.OpCode.NEWBUFFER);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(VM.OpCode.DUP);

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
/// |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       1.745 us |     0.1738 us |     0.5125 us |       1.550 us |
/// | Bench_OneGAS    | 4         | 516,158.636 us | 5,862.5054 us | 5,196.9583 us | 515,452.950 us |
/// | Bench_OneOpCode | 8         |       1.756 us |     0.1489 us |     0.4366 us |       1.700 us |
/// | Bench_OneGAS    | 8         | 510,746.407 us | 1,862.8748 us | 1,742.5343 us | 510,228.900 us |
/// | Bench_OneOpCode | 16        |       1.720 us |     0.1568 us |     0.4598 us |       1.600 us |
/// | Bench_OneGAS    | 16        | 509,133.100 us | 5,402.8303 us | 5,053.8108 us | 510,704.500 us |
/// | Bench_OneOpCode | 32        |       1.669 us |     0.0590 us |     0.1635 us |       1.700 us |
/// | Bench_OneGAS    | 32        | 503,492.153 us | 2,621.1257 us | 2,451.8026 us | 503,364.600 us |
/// | Bench_OneOpCode | 64        |       1.800 us |     0.1395 us |     0.4091 us |       1.700 us |
/// | Bench_OneGAS    | 64        | 505,663.392 us | 5,302.9736 us | 4,428.2231 us | 505,637.700 us |
/// | Bench_OneOpCode | 128       |       1.577 us |     0.1403 us |     0.4091 us |       1.400 us |
/// | Bench_OneGAS    | 128       | 513,814.353 us | 2,841.7644 us | 2,658.1882 us | 513,830.400 us |
/// | Bench_OneOpCode | 256       |       2.121 us |     0.1553 us |     0.4379 us |       2.000 us |
/// | Bench_OneGAS    | 256       | 511,672.107 us | 3,170.5584 us | 2,965.7423 us | 511,026.200 us |
/// | Bench_OneOpCode | 512       |       1.780 us |     0.1386 us |     0.4087 us |       1.600 us |
/// | Bench_OneGAS    | 512       | 512,673.487 us | 3,530.6301 us | 3,302.5536 us | 512,084.400 us |
/// | Bench_OneOpCode | 1024      |       1.761 us |     0.1595 us |     0.4576 us |       1.600 us |
/// | Bench_OneGAS    | 1024      | 513,566.586 us | 2,777.7146 us | 2,462.3717 us | 514,272.250 us |
/// | Bench_OneOpCode | 2040      |       1.575 us |     0.1010 us |     0.2865 us |       1.500 us |
/// | Bench_OneGAS    | 2040      | 515,193.064 us | 5,347.2003 us | 4,740.1538 us | 515,853.000 us |
