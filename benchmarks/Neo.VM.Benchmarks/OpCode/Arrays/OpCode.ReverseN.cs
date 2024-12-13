// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.ReverseN.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_ReverseN : OpCodeBase
    {
        protected override byte[] CreateScript(BenchmarkMode benchmarkMode)
        {
            var builder = new InstructionBuilder();
            var initBegin = new JumpTarget();
            builder.AddInstruction(new Instruction { _opCode = VM.OpCode.INITSLOT, _operand = [1, 0] });
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.STLOC0);
            initBegin._instruction = builder.AddInstruction(VM.OpCode.NOP);
            builder.Push(0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.AddInstruction(VM.OpCode.DEC);
            builder.AddInstruction(VM.OpCode.STLOC0);
            builder.AddInstruction(VM.OpCode.LDLOC0);
            builder.Jump(VM.OpCode.JMPIF, initBegin);
            if (benchmarkMode == BenchmarkMode.BaseLine)
            {
                return builder.ToArray();
            }
            builder.Push(ItemCount);
            builder.AddInstruction(VM.OpCode.REVERSEN);
            if (benchmarkMode == BenchmarkMode.OneGAS)
            {
                // just keep running until GAS is exhausted
                var loopStart = new JumpTarget { _instruction = builder.AddInstruction(VM.OpCode.NOP) };
                builder.Push(ItemCount);
                builder.AddInstruction(VM.OpCode.REVERSEN);
                builder.Jump(VM.OpCode.JMP, loopStart);
            }

            return builder.ToArray();
        }
    }
}

// for 0

// BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4249/23H2/2023Update/SunValley3)
// Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
// .NET SDK 8.0.205
//   [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//   DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
//
//
// | Method               | ItemCount | Mean             | Error         | StdDev         | Median           | Ratio     | RatioSD  |
// |--------------------- |---------- |-----------------:|--------------:|---------------:|-----------------:|----------:|---------:|
// | Bench_ReverseN       | 1         |         63.43 us |      0.466 us |       0.518 us |         63.45 us |      0.99 |     0.01 |
// | Bench_OneGasReverseN | 1         |    403,904.11 us |  6,492.511 us |   6,073.099 us |    402,932.40 us |  6,315.67 |    89.44 |
// | Bench_BaseLine       | 1         |         63.96 us |      0.763 us |       0.714 us |         63.92 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 2         |         62.55 us |      0.988 us |       0.924 us |         62.46 us |      0.95 |     0.02 |
// | Bench_OneGasReverseN | 2         |    424,297.10 us |  8,453.137 us |   7,493.486 us |    423,912.90 us |  6,446.21 |   118.35 |
// | Bench_BaseLine       | 2         |         65.83 us |      0.845 us |       0.749 us |         65.95 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 4         |         63.93 us |      0.418 us |       0.371 us |         63.89 us |      0.95 |     0.03 |
// | Bench_OneGasReverseN | 4         |    443,708.92 us |  6,689.013 us |   6,256.907 us |    444,636.60 us |  6,560.69 |   229.86 |
// | Bench_BaseLine       | 4         |         67.64 us |      1.281 us |       1.524 us |         67.79 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 8         |         66.69 us |      0.757 us |       0.671 us |         66.69 us |      1.00 |     0.02 |
// | Bench_OneGasReverseN | 8         |    463,571.38 us |  6,614.687 us |   6,187.382 us |    465,568.00 us |  6,963.59 |    85.80 |
// | Bench_BaseLine       | 8         |         66.64 us |      0.870 us |       0.771 us |         66.68 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 16        |         65.85 us |      0.994 us |       0.929 us |         65.61 us |      0.94 |     0.02 |
// | Bench_OneGasReverseN | 16        |    740,905.55 us | 71,090.901 us | 209,613.127 us |    653,644.75 us |  9,341.86 | 3,092.85 |
// | Bench_BaseLine       | 16        |         70.08 us |      1.376 us |       1.638 us |         70.15 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 32        |         66.47 us |      0.928 us |       2.187 us |         65.77 us |      1.02 |     0.04 |
// | Bench_OneGasReverseN | 32        |    631,596.65 us | 11,060.847 us |  10,346.323 us |    629,654.10 us |  9,360.06 |   221.36 |
// | Bench_BaseLine       | 32        |         67.49 us |      0.900 us |       0.842 us |         67.56 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 64        |         72.21 us |      0.921 us |       0.862 us |         72.05 us |      1.02 |     0.02 |
// | Bench_OneGasReverseN | 64        |    787,570.95 us |  6,915.746 us |   6,468.994 us |    786,778.70 us | 11,090.76 |   177.74 |
// | Bench_BaseLine       | 64        |         71.02 us |      0.946 us |       0.884 us |         71.06 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 128       |         80.17 us |      0.723 us |       0.676 us |         80.19 us |      0.98 |     0.01 |
// | Bench_OneGasReverseN | 128       |  1,134,510.61 us | 14,991.714 us |  14,023.259 us |  1,133,177.90 us | 13,828.61 |   184.58 |
// | Bench_BaseLine       | 128       |         81.90 us |      0.623 us |       0.553 us |         81.77 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 256       |         98.24 us |      1.140 us |       1.067 us |         98.05 us |      0.99 |     0.01 |
// | Bench_OneGasReverseN | 256       |  1,785,906.33 us | 13,785.746 us |  12,895.195 us |  1,788,819.30 us | 18,067.20 |   198.87 |
// | Bench_BaseLine       | 256       |         98.85 us |      0.961 us |       0.899 us |         98.95 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 512       |        136.19 us |      1.614 us |       1.510 us |        136.34 us |      1.02 |     0.02 |
// | Bench_OneGasReverseN | 512       |  3,100,087.41 us | 16,564.249 us |  15,494.209 us |  3,097,066.60 us | 23,209.57 |   381.50 |
// | Bench_BaseLine       | 512       |        133.60 us |      2.144 us |       2.006 us |        132.73 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 1024      |        207.06 us |      2.213 us |       2.070 us |        206.76 us |      1.01 |     0.01 |
// | Bench_OneGasReverseN | 1024      |  5,762,294.72 us | 20,289.404 us |  16,942.572 us |  5,764,133.80 us | 28,109.14 |   349.87 |
// | Bench_BaseLine       | 1024      |        205.07 us |      2.360 us |       2.208 us |        205.07 us |      1.00 |     0.00 |
// |                      |           |                  |               |                |                  |           |          |
// | Bench_ReverseN       | 2040      |        345.09 us |      4.271 us |       3.995 us |        345.40 us |      0.97 |     0.01 |
// | Bench_OneGasReverseN | 2040      | 11,005,147.03 us | 37,306.455 us |  33,071.200 us | 11,003,479.70 us | 31,019.36 |   356.02 |
// | Bench_BaseLine       | 2040      |        354.62 us |      4.623 us |       4.325 us |        353.32 us |      1.00 |     0.00 |



// for StackItems that has a size of maximum stack size.
// | Method               | ItemCount | Mean            | Error         | StdDev        |
// |--------------------- |---------- |----------------:|--------------:|--------------:|
// | Bench_ReverseN       | 1         |        104.0 us |       0.77 us |       0.72 us |
// | Bench_OneGasReverseN | 1         |    389,585.4 us |   4,740.18 us |   4,433.96 us |
// | Bench_ReverseN       | 2         |        148.3 us |       2.25 us |       2.10 us |
// | Bench_OneGasReverseN | 2         |    417,831.5 us |   6,651.20 us |   6,221.53 us |
// | Bench_ReverseN       | 4         |        231.8 us |       3.92 us |       3.67 us |
// | Bench_OneGasReverseN | 4         |    428,442.6 us |   8,034.41 us |   7,515.39 us |
// | Bench_ReverseN       | 8         |        387.8 us |       5.23 us |       4.89 us |
// | Bench_OneGasReverseN | 8         |    448,046.9 us |   6,270.18 us |   5,235.89 us |
// | Bench_ReverseN       | 16        |        240.0 us |       7.30 us |      21.53 us |
// | Bench_OneGasReverseN | 16        |    522,904.3 us |   7,157.93 us |   6,695.54 us |
// | Bench_ReverseN       | 32        |        302.4 us |       9.53 us |      27.79 us |
// | Bench_OneGasReverseN | 32        |    626,536.6 us |   6,629.69 us |   6,201.42 us |
// | Bench_ReverseN       | 64        |      1,728.3 us |      34.27 us |      58.19 us |
// | Bench_OneGasReverseN | 64        |    827,284.5 us |  15,943.00 us |  14,913.09 us |
// | Bench_ReverseN       | 128       |      3,704.5 us |      73.98 us |     175.82 us |
// | Bench_OneGasReverseN | 128       |  1,125,104.6 us |  10,629.65 us |   9,942.98 us |
// | Bench_ReverseN       | 256       |      6,381.1 us |     127.42 us |     290.21 us |
// | Bench_OneGasReverseN | 256       |  1,804,355.7 us |   9,690.50 us |   8,590.37 us |
// | Bench_ReverseN       | 512       |      9,485.9 us |     184.52 us |     492.52 us |
// | Bench_OneGasReverseN | 512       |  3,159,411.1 us |  28,901.54 us |  27,034.52 us |
// | Bench_ReverseN       | 1024      |     14,125.6 us |     282.51 us |     577.08 us |
// | Bench_OneGasReverseN | 1024      |  5,799,154.5 us |  33,817.93 us |  31,633.31 us |
// | Bench_ReverseN       | 2040      |     22,868.0 us |     449.84 us |     929.00 us |
// | Bench_OneGasReverseN | 2040      | 11,100,853.9 us | 159,980.97 us | 141,818.97 us |
