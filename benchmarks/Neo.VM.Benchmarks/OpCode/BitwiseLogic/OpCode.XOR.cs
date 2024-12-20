// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.XOR.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_XOR : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.XOR;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(int.MaxValue);
            builder.Push(int.MaxValue);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.Push(int.MaxValue);
            builder.Push(int.MaxValue);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error         | StdDev        | Median         |
/// |---------------- |---------- |---------------:|--------------:|--------------:|---------------:|
/// | Bench_OneOpCode | 4         |       2.003 us |     0.0430 us |     0.0707 us |       2.000 us |
/// | Bench_OneGAS    | 4         | 410,004.977 us | 3,931.7463 us | 3,283.1863 us | 410,763.900 us |
/// | Bench_OneOpCode | 8         |       2.124 us |     0.0607 us |     0.1702 us |       2.100 us |
/// | Bench_OneGAS    | 8         | 410,823.327 us | 3,090.0525 us | 2,890.4371 us | 410,876.200 us |
/// | Bench_OneOpCode | 16        |       2.091 us |     0.0479 us |     0.1319 us |       2.100 us |
/// | Bench_OneGAS    | 16        | 413,055.164 us | 3,072.9533 us | 2,724.0931 us | 412,989.750 us |
/// | Bench_OneOpCode | 32        |       2.092 us |     0.0458 us |     0.1052 us |       2.100 us |
/// | Bench_OneGAS    | 32        | 417,668.443 us | 2,971.0729 us | 2,633.7787 us | 418,098.600 us |
/// | Bench_OneOpCode | 64        |       2.072 us |     0.0488 us |     0.1285 us |       2.050 us |
/// | Bench_OneGAS    | 64        | 410,061.086 us | 3,594.8986 us | 3,186.7840 us | 410,972.200 us |
/// | Bench_OneOpCode | 128       |       2.142 us |     0.0459 us |     0.0852 us |       2.100 us |
/// | Bench_OneGAS    | 128       | 418,706.447 us | 3,501.9563 us | 3,275.7321 us | 419,174.000 us |
/// | Bench_OneOpCode | 256       |       2.065 us |     0.0525 us |     0.1429 us |       2.000 us |
/// | Bench_OneGAS    | 256       | 415,821.964 us | 4,325.4530 us | 3,834.4014 us | 415,590.050 us |
/// | Bench_OneOpCode | 512       |       2.003 us |     0.0435 us |     0.0955 us |       2.000 us |
/// | Bench_OneGAS    | 512       | 444,446.960 us | 4,627.7795 us | 4,328.8278 us | 443,320.900 us |
/// | Bench_OneOpCode | 1024      |       2.522 us |     0.1683 us |     0.4882 us |       2.300 us |
/// | Bench_OneGAS    | 1024      | 407,820.679 us | 2,748.5073 us | 2,436.4801 us | 407,415.350 us |
/// | Bench_OneOpCode | 2040      |       2.465 us |     0.1527 us |     0.4477 us |       2.300 us |
/// | Bench_OneGAS    | 2040      | 412,915.323 us | 3,418.9878 us | 2,855.0097 us | 412,854.300 us |
