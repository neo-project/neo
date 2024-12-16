// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWSTRUCT0.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_NEWSTRUCT0 : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.NEWSTRUCT0;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();

            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopBegin);

            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
/// |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       1.752 us |     0.0974 us |     0.2748 us |       1.700 us |
/// | Bench_OneGAS    | 4         | 152,200.267 us | 1,241.3584 us | 1,161.1674 us | 152,453.900 us |
/// | Bench_OneOpCode | 8         |       1.702 us |     0.0378 us |     0.0737 us |       1.700 us |
/// | Bench_OneGAS    | 8         | 150,072.186 us | 1,234.9855 us | 1,094.7825 us | 150,164.250 us |
/// | Bench_OneOpCode | 16        |       1.736 us |     0.0386 us |     0.0916 us |       1.700 us |
/// | Bench_OneGAS    | 16        | 154,124.779 us | 2,600.0229 us | 2,889.9186 us | 153,718.800 us |
/// | Bench_OneOpCode | 32        |       1.780 us |     0.0412 us |     0.1106 us |       1.800 us |
/// | Bench_OneGAS    | 32        | 151,427.667 us | 1,317.5607 us | 1,232.4471 us | 151,544.500 us |
/// | Bench_OneOpCode | 64        |       1.793 us |     0.0397 us |     0.1087 us |       1.800 us |
/// | Bench_OneGAS    | 64        | 152,349.440 us | 2,549.1057 us | 2,384.4350 us | 152,085.900 us |
/// | Bench_OneOpCode | 128       |       1.701 us |     0.0578 us |     0.1563 us |       1.750 us |
/// | Bench_OneGAS    | 128       | 149,315.900 us | 1,176.5503 us | 1,100.5459 us | 149,253.800 us |
/// | Bench_OneOpCode | 256       |       1.706 us |     0.0404 us |     0.1125 us |       1.700 us |
/// | Bench_OneGAS    | 256       | 152,080.287 us | 1,446.2441 us | 1,352.8176 us | 151,597.300 us |
/// | Bench_OneOpCode | 512       |       1.743 us |     0.0382 us |     0.0781 us |       1.700 us |
/// | Bench_OneGAS    | 512       | 153,904.773 us | 1,804.9919 us | 1,688.3906 us | 154,223.800 us |
/// | Bench_OneOpCode | 1024      |       1.740 us |     0.0564 us |     0.1544 us |       1.700 us |
/// | Bench_OneGAS    | 1024      | 152,672.679 us | 2,020.8597 us | 1,791.4395 us | 152,596.600 us |
/// | Bench_OneOpCode | 2040      |       1.721 us |     0.0383 us |     0.0825 us |       1.700 us |
/// | Bench_OneGAS    | 2040      | 151,173.753 us | 2,131.0360 us | 1,993.3724 us | 151,544.500 us |
