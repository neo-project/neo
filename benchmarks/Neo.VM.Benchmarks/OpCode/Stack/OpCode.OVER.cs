// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.OVER.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_OVER : OpCodeBase
    {
        protected override VM.OpCode Opcode => VM.OpCode.OVER;

        protected override byte[] CreateOneOpCodeScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(int.MaxValue);
            builder.AddInstruction(VM.OpCode.DUP);
            builder.AddInstruction(Opcode);
            return builder.ToArray();
        }

        protected override byte[] CreateOneGASScript()
        {
            var builder = new InstructionBuilder();
            builder.Push(int.MaxValue);
            builder.AddInstruction(VM.OpCode.DUP);
            var loopBegin = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };

            builder.AddInstruction(Opcode);
            builder.AddInstruction(VM.OpCode.DROP);

            builder.Jump(VM.OpCode.JMP, loopBegin);
            return builder.ToArray();
        }
    }
}

/// | Method          | ItemCount | Mean             | Error           | StdDev          | Median           |
/// |---------------- |---------- |-----------------:|----------------:|----------------:|-----------------:|
/// | Bench_OneOpCode | 4         |       1,322.2 ns |       112.30 ns |       329.36 ns |       1,200.0 ns |
/// | Bench_OneGAS    | 4         | 421,390,313.3 ns | 4,391,292.71 ns | 4,107,617.92 ns | 420,128,600.0 ns |
/// | Bench_OneOpCode | 8         |       1,439.4 ns |       136.02 ns |       398.93 ns |       1,300.0 ns |
/// | Bench_OneGAS    | 8         | 423,047,142.9 ns | 3,634,534.42 ns | 3,221,920.09 ns | 422,495,650.0 ns |
/// | Bench_OneOpCode | 16        |       1,440.6 ns |        84.24 ns |       243.04 ns |       1,400.0 ns |
/// | Bench_OneGAS    | 16        | 419,468,957.1 ns | 2,913,374.16 ns | 2,582,630.31 ns | 419,939,000.0 ns |
/// | Bench_OneOpCode | 32        |       1,156.9 ns |        27.01 ns |        66.77 ns |       1,200.0 ns |
/// | Bench_OneGAS    | 32        | 422,949,806.7 ns | 3,553,895.16 ns | 3,324,315.74 ns | 421,796,400.0 ns |
/// | Bench_OneOpCode | 64        |       1,391.0 ns |       126.45 ns |       372.84 ns |       1,300.0 ns |
/// | Bench_OneGAS    | 64        | 418,667,450.0 ns | 2,156,506.52 ns | 2,017,197.53 ns | 418,702,850.0 ns |
/// | Bench_OneOpCode | 128       |       1,285.0 ns |       147.43 ns |       434.70 ns |       1,100.0 ns |
/// | Bench_OneGAS    | 128       | 417,483,292.9 ns | 1,677,753.46 ns | 1,487,284.74 ns | 417,629,750.0 ns |
/// | Bench_OneOpCode | 256       |         967.3 ns |        22.94 ns |        47.37 ns |       1,000.0 ns |
/// | Bench_OneGAS    | 256       | 428,779,278.6 ns | 3,247,052.01 ns | 2,878,427.03 ns | 429,308,750.0 ns |
/// | Bench_OneOpCode | 512       |       1,167.6 ns |        70.64 ns |       190.97 ns |       1,150.0 ns |
/// | Bench_OneGAS    | 512       | 420,111,173.3 ns | 2,480,418.37 ns | 2,320,184.88 ns | 420,697,700.0 ns |
/// | Bench_OneOpCode | 1024      |       1,153.8 ns |        50.81 ns |       144.13 ns |       1,100.0 ns |
/// | Bench_OneGAS    | 1024      | 418,113,415.4 ns | 2,326,468.24 ns | 1,942,706.35 ns | 418,124,800.0 ns |
/// | Bench_OneOpCode | 2040      |       1,120.9 ns |        74.33 ns |       208.44 ns |       1,100.0 ns |
/// | Bench_OneGAS    | 2040      | 427,939,346.7 ns | 2,335,489.23 ns | 2,184,618.08 ns | 427,588,000.0 ns |
