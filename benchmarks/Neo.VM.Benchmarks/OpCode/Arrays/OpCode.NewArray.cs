// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NewArray.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_NewArray : OpCodeBase
{
    protected override byte[] CreateScript(BenchmarkMode benchmarkMode)
    {
        var builder = new InstructionBuilder();
        builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
        builder.Push(ItemCount);

        if (benchmarkMode == BenchmarkMode.BaseLine)
        {
            return builder.ToArray();
        }

        builder.AddInstruction(VM.OpCode.NEWARRAY);
        builder.AddInstruction(VM.OpCode.DROP);
        if (benchmarkMode == BenchmarkMode.OneGAS)
        {
            // just keep running until GAS is exhausted
            var loopStart = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.NEWARRAY);
            builder.AddInstruction(VM.OpCode.DROP);
            builder.Jump(VM.OpCode.JMP, loopStart);
        }

        return builder.ToArray();
    }
}

// BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4249/23H2/2023Update/SunValley3)
// Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
// .NET SDK 8.0.205
//   [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//   DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//
//
// | Method          | ItemCount | Mean            | Error         | StdDev        | Ratio     | RatioSD |
// |---------------- |---------- |----------------:|--------------:|--------------:|----------:|--------:|
// | Bench_BaseLine  | 1         |        62.80 us |      0.626 us |      0.586 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 1         |        63.25 us |      0.307 us |      0.287 us |      1.01 |    0.01 |
// | Bench_OneGAS    | 1         |    47,245.44 us |    285.612 us |    267.162 us |    752.43 |    7.76 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 2         |        62.19 us |      0.285 us |      0.266 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 2         |        61.59 us |      0.475 us |      0.421 us |      0.99 |    0.01 |
// | Bench_OneGAS    | 2         |    49,352.17 us |    386.045 us |    361.106 us |    793.58 |    6.00 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 4         |        61.81 us |      0.410 us |      0.384 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 4         |        61.05 us |      0.551 us |      0.489 us |      0.99 |    0.01 |
// | Bench_OneGAS    | 4         |    52,454.12 us |    664.488 us |    621.563 us |    848.61 |   10.32 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 8         |        62.29 us |      0.356 us |      0.333 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 8         |        60.39 us |      0.299 us |      0.280 us |      0.97 |    0.01 |
// | Bench_OneGAS    | 8         |    59,353.67 us |    551.736 us |    516.095 us |    952.99 |   12.23 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 16        |        60.83 us |      0.245 us |      0.230 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 16        |        60.45 us |      0.477 us |      0.446 us |      0.99 |    0.01 |
// | Bench_OneGAS    | 16        |    77,824.14 us |    734.465 us |    687.019 us |  1,279.46 |   14.54 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 32        |        62.60 us |      0.340 us |      0.302 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 32        |        60.49 us |      0.422 us |      0.395 us |      0.97 |    0.01 |
// | Bench_OneGAS    | 32        |   109,717.10 us |    930.827 us |    870.696 us |  1,752.52 |   19.79 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 64        |        60.71 us |      0.305 us |      0.286 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 64        |        60.70 us |      0.189 us |      0.158 us |      1.00 |    0.00 |
// | Bench_OneGAS    | 64        |   174,078.59 us |    403.340 us |    336.807 us |  2,869.71 |   14.86 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 128       |        60.36 us |      0.326 us |      0.305 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 128       |        61.40 us |      0.298 us |      0.279 us |      1.02 |    0.01 |
// | Bench_OneGAS    | 128       |   293,284.17 us |    794.785 us |    620.516 us |  4,854.95 |   26.45 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 256       |        59.64 us |      0.176 us |      0.156 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 256       |        62.97 us |      0.392 us |      0.367 us |      1.06 |    0.01 |
// | Bench_OneGAS    | 256       |   538,342.81 us |  4,732.706 us |  4,195.420 us |  9,027.24 |   73.49 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 512       |        60.01 us |      0.208 us |      0.194 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 512       |        64.68 us |      0.407 us |      0.380 us |      1.08 |    0.01 |
// | Bench_OneGAS    | 512       | 1,030,016.69 us |  6,468.866 us |  5,734.481 us | 17,163.71 |  104.01 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 1024      |        60.49 us |      0.231 us |      0.217 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 1024      |        64.97 us |      0.230 us |      0.215 us |      1.07 |    0.00 |
// | Bench_OneGAS    | 1024      | 2,010,862.39 us | 14,764.440 us | 13,810.666 us | 33,243.12 |  266.64 |
// |                 |           |                 |               |               |           |         |
// | Bench_BaseLine  | 2040      |        59.69 us |      0.325 us |      0.304 us |      1.00 |    0.00 |
// | Bench_OneOpCode | 2040      |        68.75 us |      0.347 us |      0.308 us |      1.15 |    0.01 |
// | Bench_OneGAS    | 2040      | 3,888,712.51 us | 13,530.909 us | 11,994.798 us | 65,120.45 |  397.58 |
