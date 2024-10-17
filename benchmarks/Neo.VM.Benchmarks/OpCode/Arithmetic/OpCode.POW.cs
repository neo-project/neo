// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.POW.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.Extensions;
using System.Numerics;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_POW
{
    private BenchmarkEngine _engine;

    private const VM.OpCode Opcode = VM.OpCode.SHR;

    [ParamsSource(nameof(Values))]
    public BigInteger _value;

    [Params(0, 16, 32, 64, 128, 256)]
    public int _exponent;


    public static IEnumerable<BigInteger> Values =>
    [
        Benchmark_Opcode.MAX_INT,
        Benchmark_Opcode.MIN_INT,
        BigInteger.One,
        BigInteger.Zero,
        BigInteger.MinusOne,
        uint.MaxValue,
        uint.MaxValue,
        int.MaxValue,
        int.MinValue,
        long.MaxValue,
        long.MinValue,
        BigInteger.Parse("170141183460469231731687303715884105727") // Mersenne prime 2^127 - 1
    ];


    [IterationSetup]
    public void Setup()
    {
        _engine = new BenchmarkEngine();
        _engine.LoadScript(CreateBenchScript(_value, _exponent));
        _engine.ExecuteUntil(Opcode);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public void Bench()
    {
        _engine.ExecuteNext();

    }

    byte[] CreateBenchScript(BigInteger a, BigInteger b)
    {
        var builder = new InstructionBuilder();

        builder.Push(a);
        builder.Push(b); // 0 256
        builder.AddInstruction(Opcode);
        return builder.ToArray();
    }
}


// | Method | _exponent | _value               | Mean       | Error     | StdDev    | Median     |
// |------- |---------- |--------------------- |-----------:|----------:|----------:|-----------:|
// | Bench  | 0         | -5789(...)19968 [78] |   980.8 ns |  19.27 ns |  39.80 ns | 1,000.0 ns |
// | Bench  | 0         | -9223372036854775808 | 1,103.8 ns |  46.44 ns | 130.22 ns | 1,050.0 ns |
// | Bench  | 0         | -2147483648          | 1,063.6 ns |  46.44 ns | 127.92 ns | 1,000.0 ns |
// | Bench  | 0         | -1                   |   993.0 ns |  28.07 ns |  76.37 ns | 1,000.0 ns |
// | Bench  | 0         | 0                    |   974.7 ns |  23.29 ns |  62.16 ns | 1,000.0 ns |
// | Bench  | 0         | 1                    | 1,051.8 ns |  25.75 ns |  68.72 ns | 1,000.0 ns |
// | Bench  | 0         | 2147483647           | 1,096.3 ns |  28.43 ns |  74.91 ns | 1,100.0 ns |
// | Bench  | 0         | 4294967295           | 1,023.2 ns |  28.60 ns |  75.85 ns | 1,000.0 ns |
// | Bench  | 0         | 4294967295           |   945.1 ns |  22.72 ns |  63.71 ns |   900.0 ns |
// | Bench  | 0         | 9223372036854775807  | 1,169.5 ns |  81.61 ns | 216.42 ns | 1,100.0 ns |
// | Bench  | 0         | 17014(...)05727 [39] | 1,000.0 ns |  34.34 ns |  94.01 ns | 1,000.0 ns |
// | Bench  | 0         | 57896(...)19967 [77] | 1,080.9 ns |  58.06 ns | 160.88 ns | 1,000.0 ns |
// | Bench  | 16        | -5789(...)19968 [78] | 2,718.8 ns |  90.87 ns | 245.67 ns | 2,600.0 ns |
// | Bench  | 16        | -9223372036854775808 | 2,468.8 ns |  48.74 ns |  47.87 ns | 2,500.0 ns |
// | Bench  | 16        | -2147483648          | 2,489.8 ns |  43.30 ns |  95.94 ns | 2,500.0 ns |
// | Bench  | 16        | -1                   | 2,574.7 ns | 183.90 ns | 515.66 ns | 2,300.0 ns |
// | Bench  | 16        | 0                    | 2,400.0 ns | 117.94 ns | 318.85 ns | 2,300.0 ns |
// | Bench  | 16        | 1                    | 2,241.1 ns |  61.29 ns | 170.85 ns | 2,200.0 ns |
// | Bench  | 16        | 2147483647           | 2,340.7 ns | 138.73 ns | 377.41 ns | 2,200.0 ns |
// | Bench  | 16        | 4294967295           | 2,378.2 ns |  53.56 ns | 146.61 ns | 2,300.0 ns |
// | Bench  | 16        | 4294967295           | 3,125.0 ns | 287.43 ns | 847.50 ns | 2,850.0 ns |
// | Bench  | 16        | 9223372036854775807  | 2,575.3 ns | 106.30 ns | 287.40 ns | 2,500.0 ns |
// | Bench  | 16        | 17014(...)05727 [39] | 2,575.9 ns |  98.18 ns | 262.07 ns | 2,500.0 ns |
// | Bench  | 16        | 57896(...)19967 [77] | 2,504.7 ns |  73.29 ns | 198.15 ns | 2,400.0 ns |
// | Bench  | 32        | -5789(...)19968 [78] | 2,678.2 ns |  70.12 ns | 191.95 ns | 2,700.0 ns |
// | Bench  | 32        | -9223372036854775808 | 2,650.5 ns |  96.19 ns | 269.72 ns | 2,600.0 ns |
// | Bench  | 32        | -2147483648          | 2,248.2 ns |  51.79 ns | 138.25 ns | 2,200.0 ns |
// | Bench  | 32        | -1                   | 2,040.4 ns |  42.31 ns |  82.51 ns | 2,000.0 ns |
// | Bench  | 32        | 0                    | 2,137.9 ns |  54.93 ns | 150.38 ns | 2,100.0 ns |
// | Bench  | 32        | 1                    | 2,155.8 ns |  52.17 ns | 141.92 ns | 2,100.0 ns |
// | Bench  | 32        | 2147483647           | 2,184.3 ns |  48.93 ns | 135.60 ns | 2,100.0 ns |
// | Bench  | 32        | 4294967295           | 2,222.2 ns |  43.60 ns | 107.76 ns | 2,200.0 ns |
// | Bench  | 32        | 4294967295           | 2,426.5 ns |  98.30 ns | 265.76 ns | 2,350.0 ns |
// | Bench  | 32        | 9223372036854775807  | 2,506.5 ns |  76.24 ns | 206.12 ns | 2,450.0 ns |
// | Bench  | 32        | 17014(...)05727 [39] | 2,655.3 ns | 129.21 ns | 349.32 ns | 2,500.0 ns |
// | Bench  | 32        | 57896(...)19967 [77] | 2,488.5 ns |  56.10 ns | 153.58 ns | 2,500.0 ns |
// | Bench  | 64        | -5789(...)19968 [78] | 2,581.4 ns |  75.96 ns | 206.66 ns | 2,500.0 ns |
// | Bench  | 64        | -9223372036854775808 | 2,368.5 ns |  55.36 ns | 156.14 ns | 2,300.0 ns |
// | Bench  | 64        | -2147483648          | 2,147.5 ns |  46.93 ns | 103.98 ns | 2,100.0 ns |
// | Bench  | 64        | -1                   | 2,030.6 ns |  41.95 ns | 113.41 ns | 2,000.0 ns |
// | Bench  | 64        | 0                    | 2,295.5 ns |  84.55 ns | 232.88 ns | 2,200.0 ns |
// | Bench  | 64        | 1                    | 2,325.0 ns |  80.95 ns | 222.96 ns | 2,250.0 ns |
// | Bench  | 64        | 2147483647           | 2,089.5 ns |  28.37 ns |  31.53 ns | 2,100.0 ns |
// | Bench  | 64        | 4294967295           | 2,368.6 ns |  51.45 ns | 139.97 ns | 2,300.0 ns |
// | Bench  | 64        | 4294967295           | 2,629.4 ns | 160.50 ns | 433.93 ns | 2,500.0 ns |
// | Bench  | 64        | 9223372036854775807  | 2,320.2 ns |  50.30 ns | 135.13 ns | 2,300.0 ns |
// | Bench  | 64        | 17014(...)05727 [39] | 2,393.5 ns |  46.65 ns | 119.58 ns | 2,400.0 ns |
// | Bench  | 64        | 57896(...)19967 [77] | 2,501.1 ns |  74.10 ns | 205.32 ns | 2,400.0 ns |
// | Bench  | 128       | -5789(...)19968 [78] | 2,733.7 ns | 101.90 ns | 277.22 ns | 2,650.0 ns |
// | Bench  | 128       | -9223372036854775808 | 2,792.9 ns | 270.87 ns | 794.40 ns | 2,400.0 ns |
// | Bench  | 128       | -2147483648          | 2,227.1 ns |  54.86 ns | 148.31 ns | 2,200.0 ns |
// | Bench  | 128       | -1                   | 2,112.6 ns |  86.63 ns | 237.14 ns | 2,000.0 ns |
// | Bench  | 128       | 0                    | 2,142.5 ns |  47.15 ns | 129.07 ns | 2,100.0 ns |
// | Bench  | 128       | 1                    | 2,194.3 ns |  71.85 ns | 196.69 ns | 2,100.0 ns |
// | Bench  | 128       | 2147483647           | 2,152.9 ns |  77.96 ns | 213.41 ns | 2,100.0 ns |
// | Bench  | 128       | 4294967295           | 2,395.3 ns |  91.42 ns | 248.72 ns | 2,300.0 ns |
// | Bench  | 128       | 4294967295           | 2,415.7 ns |  50.28 ns | 102.71 ns | 2,400.0 ns |
// | Bench  | 128       | 9223372036854775807  | 2,490.8 ns | 115.87 ns | 317.19 ns | 2,400.0 ns |
// | Bench  | 128       | 17014(...)05727 [39] | 2,315.1 ns |  64.91 ns | 176.58 ns | 2,300.0 ns |
// | Bench  | 128       | 57896(...)19967 [77] | 2,420.6 ns |  52.32 ns |  84.49 ns | 2,400.0 ns |
// | Bench  | 256       | -5789(...)19968 [78] | 2,221.5 ns |  46.31 ns | 108.24 ns | 2,200.0 ns |
// | Bench  | 256       | -9223372036854775808 | 2,218.9 ns |  47.76 ns |  81.10 ns | 2,200.0 ns |
// | Bench  | 256       | -2147483648          | 2,313.6 ns |  96.95 ns | 267.03 ns | 2,200.0 ns |
// | Bench  | 256       | -1                   | 2,114.5 ns |  63.59 ns | 169.74 ns | 2,100.0 ns |
// | Bench  | 256       | 0                    | 2,139.3 ns |  53.28 ns | 143.13 ns | 2,100.0 ns |
// | Bench  | 256       | 1                    | 2,150.6 ns |  53.43 ns | 144.44 ns | 2,100.0 ns |
// | Bench  | 256       | 2147483647           | 2,844.0 ns | 255.27 ns | 752.68 ns | 2,600.0 ns |
// | Bench  | 256       | 4294967295           | 2,206.8 ns |  47.89 ns |  89.96 ns | 2,200.0 ns |
// | Bench  | 256       | 4294967295           | 2,363.6 ns |  93.45 ns | 257.39 ns | 2,300.0 ns |
// | Bench  | 256       | 9223372036854775807  | 2,304.5 ns |  48.48 ns | 103.31 ns | 2,250.0 ns |
// | Bench  | 256       | 17014(...)05727 [39] | 2,403.6 ns | 107.64 ns | 287.31 ns | 2,300.0 ns |
// | Bench  | 256       | 57896(...)19967 [77] | 2,425.0 ns | 102.29 ns | 281.72 ns | 2,300.0 ns |
