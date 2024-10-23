// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SQRT.cs file belongs to the neo project and is free
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

public class OpCode_SQRT
{

    private BenchmarkEngine _engine;

    private VM.OpCode Opcode => VM.OpCode.SQRT;

    [ParamsSource(nameof(Values))]
    public BigInteger _value;


    public static IEnumerable<BigInteger> Values =>
    [
        Benchmark_Opcode.MAX_INT,
        Benchmark_Opcode.MIN_INT,
        BigInteger.One,
        BigInteger.Zero,
        int.MaxValue,
        int.MinValue,
        long.MaxValue,
        long.MinValue,
        BigInteger.Parse("170141183460469231731687303715884105727") // Mersenne prime 2^127 - 1
    ];

    [IterationSetup]
    public void Setup()
    {
        var builder = new InstructionBuilder();
        builder.Push(_value);
        builder.AddInstruction(Opcode);

        _engine = new BenchmarkEngine();
        _engine.LoadScript(builder.ToArray());
        _engine.ExecuteUntil(Opcode);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public void Bench() => _engine.ExecuteNext();
}

// to do, this need to be further checked
// minus value will directly throw an uncatchable exception
// thus transaction will fail anyway.

// | Method | _value               | Mean      | Error     | StdDev    | Median    |
//     |------- |--------------------- |----------:|----------:|----------:|----------:|
//     | Bench  | -5789(...)19968 [78] | 13.493 us | 0.9320 us | 2.7187 us | 12.200 us |
//     | Bench  | -9223372036854775808 | 12.875 us | 0.8781 us | 2.5195 us | 11.800 us |
//     | Bench  | -2147483648          | 12.993 us | 1.0099 us | 2.9138 us | 11.650 us |
//     | Bench  | 0                    |  1.836 us | 0.0593 us | 0.1602 us |  1.800 us |
//     | Bench  | 1                    |  1.969 us | 0.0549 us | 0.1511 us |  2.000 us |
//     | Bench  | 2147483647           |  2.843 us | 0.1212 us | 0.3357 us |  2.700 us |
//     | Bench  | 9223372036854775807  |  5.832 us | 0.3761 us | 1.1090 us |  5.700 us |
//     | Bench  | 17014(...)05727 [39] |  4.776 us | 0.0982 us | 0.2237 us |  4.700 us |
//     | Bench  | 57896(...)19967 [77] |  6.270 us | 0.3819 us | 1.1140 us |  5.700 us |
