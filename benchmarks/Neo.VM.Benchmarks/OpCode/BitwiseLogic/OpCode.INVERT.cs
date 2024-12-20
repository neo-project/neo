// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.INVERT.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_INVERT : OpCodeBase
    {

        protected override VM.OpCode Opcode => VM.OpCode.INVERT;


        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();

            builder.Push(int.MaxValue);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.Push(int.MaxValue);
            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean           | Error          | StdDev         | Median         |
/// |---------------- |---------- |---------------:|---------------:|---------------:|---------------:|
/// | Bench_OneOpCode | 4         |       2.136 us |      0.0441 us |      0.0699 us |       2.100 us |
/// | Bench_OneGAS    | 4         | 517,900.200 us | 10,238.4927 us | 10,055.5688 us | 515,023.050 us |
/// | Bench_OneOpCode | 8         |       2.673 us |      0.1755 us |      0.5092 us |       2.400 us |
/// | Bench_OneGAS    | 8         | 507,894.387 us |  3,377.2096 us |  3,159.0439 us | 507,480.200 us |
/// | Bench_OneOpCode | 16        |       2.433 us |      0.0687 us |      0.1810 us |       2.400 us |
/// | Bench_OneGAS    | 16        | 512,351.667 us |  8,417.1203 us |  7,873.3795 us | 513,100.400 us |
/// | Bench_OneOpCode | 32        |       2.284 us |      0.0496 us |      0.1252 us |       2.300 us |
/// | Bench_OneGAS    | 32        | 517,617.308 us |  4,624.9112 us |  3,862.0103 us | 518,845.800 us |
/// | Bench_OneOpCode | 64        |       3.280 us |      0.2466 us |      0.7155 us |       3.300 us |
/// | Bench_OneGAS    | 64        | 516,523.047 us |  5,194.9112 us |  4,859.3232 us | 517,113.500 us |
/// | Bench_OneOpCode | 128       |       2.519 us |      0.1523 us |      0.4444 us |       2.400 us |
/// | Bench_OneGAS    | 128       | 520,631.093 us |  9,584.1032 us |  8,964.9761 us | 517,092.700 us |
/// | Bench_OneOpCode | 256       |       2.363 us |      0.1080 us |      0.2939 us |       2.300 us |
/// | Bench_OneGAS    | 256       | 521,834.421 us |  4,537.2803 us |  4,022.1808 us | 522,230.050 us |
/// | Bench_OneOpCode | 512       |       2.657 us |      0.1593 us |      0.4646 us |       2.500 us |
/// | Bench_OneGAS    | 512       | 527,529.133 us |  7,069.1943 us |  6,612.5287 us | 526,032.100 us |
/// | Bench_OneOpCode | 1024      |       2.395 us |      0.1524 us |      0.4322 us |       2.200 us |
/// | Bench_OneGAS    | 1024      | 511,269.193 us |  5,636.6702 us |  5,272.5449 us | 511,026.400 us |
/// | Bench_OneOpCode | 2040      |       2.174 us |      0.0509 us |      0.1350 us |       2.200 us |
/// | Bench_OneGAS    | 2040      | 502,937.053 us |  7,638.5339 us |  7,145.0894 us | 503,489.900 us |
