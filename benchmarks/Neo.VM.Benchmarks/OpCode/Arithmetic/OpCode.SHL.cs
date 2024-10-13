// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SHL.cs file belongs to the neo project and is free
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

public class OpCode_SHL
{
    [ParamsSource(nameof(ShiftParams))]
    public int _shift;

    [ParamsSource(nameof(IntegerParams))]
    public BigInteger _initeger;


    private BenchmarkEngine _engine;

    private const VM.OpCode Opcode = VM.OpCode.SHL;

    public static IEnumerable<int> ShiftParams()
    {
        return [
            0,
            2,
            4,
            32,
            64,
            128,
            256
          ];
    }

    public static IEnumerable<BigInteger> IntegerParams()
    {
        return
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
    }

    [IterationSetup]
    public void Setup()
    {
        var builder = new InstructionBuilder();
        builder.Push(_initeger);
        builder.Push(_shift);
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
    public void Bench()
    {
        _engine.ExecuteNext();

    }
}

// | Method | _shift | _initeger            | Mean      | Error     | StdDev    | Median    |
// |------- |------- |--------------------- |----------:|----------:|----------:|----------:|
// | Bench  | 0      | -5789(...)19968 [78] |  1.154 us | 0.0363 us | 0.0975 us |  1.100 us |
// | Bench  | 0      | -9223372036854775808 |  1.094 us | 0.0683 us | 0.1870 us |  1.000 us |
// | Bench  | 0      | -2147483648          |  1.271 us | 0.1318 us | 0.3845 us |  1.100 us |
// | Bench  | 0      | 0                    |  1.199 us | 0.1172 us | 0.3436 us |  1.000 us |
// | Bench  | 0      | 1                    |  1.086 us | 0.0641 us | 0.1723 us |  1.000 us |
// | Bench  | 0      | 2147483647           |  1.445 us | 0.1461 us | 0.4307 us |  1.200 us |
// | Bench  | 0      | 9223372036854775807  |  1.051 us | 0.0437 us | 0.1181 us |  1.000 us |
// | Bench  | 0      | 17014(...)05727 [39] |  1.285 us | 0.1302 us | 0.3779 us |  1.100 us |
// | Bench  | 0      | 57896(...)19967 [77] |  1.264 us | 0.1205 us | 0.3477 us |  1.150 us |
// | Bench  | 2      | -5789(...)19968 [78] | 17.120 us | 1.1400 us | 3.3434 us | 15.500 us |
// | Bench  | 2      | -9223372036854775808 |  3.311 us | 0.2759 us | 0.8135 us |  3.050 us |
// | Bench  | 2      | -2147483648          |  3.323 us | 0.2637 us | 0.7733 us |  3.100 us |
// | Bench  | 2      | 0                    |  2.741 us | 0.2486 us | 0.7251 us |  2.400 us |
// | Bench  | 2      | 1                    |  2.465 us | 0.1354 us | 0.3660 us |  2.300 us |
// | Bench  | 2      | 2147483647           |  2.827 us | 0.1276 us | 0.3362 us |  2.800 us |
// | Bench  | 2      | 9223372036854775807  |  3.109 us | 0.2504 us | 0.7303 us |  2.800 us |
// | Bench  | 2      | 17014(...)05727 [39] |  2.868 us | 0.2155 us | 0.6044 us |  2.600 us |
// | Bench  | 2      | 57896(...)19967 [77] | 18.106 us | 1.1378 us | 3.3548 us | 17.050 us |
// | Bench  | 4      | -5789(...)19968 [78] | 17.415 us | 1.1680 us | 3.4255 us | 16.000 us |
// | Bench  | 4      | -9223372036854775808 |  3.378 us | 0.3433 us | 0.9849 us |  3.000 us |
// | Bench  | 4      | -2147483648          |  3.116 us | 0.2739 us | 0.8076 us |  2.900 us |
// | Bench  | 4      | 0                    |  2.116 us | 0.0437 us | 0.0754 us |  2.100 us |
// | Bench  | 4      | 1                    |  2.335 us | 0.0982 us | 0.2672 us |  2.250 us |
// | Bench  | 4      | 2147483647           |  2.460 us | 0.0745 us | 0.2013 us |  2.400 us |
// | Bench  | 4      | 9223372036854775807  |  2.496 us | 0.0538 us | 0.1087 us |  2.500 us |
// | Bench  | 4      | 17014(...)05727 [39] |  2.600 us | 0.1087 us | 0.2901 us |  2.500 us |
// | Bench  | 4      | 57896(...)19967 [77] | 15.208 us | 0.5675 us | 1.5342 us | 14.800 us |
// | Bench  | 32     | -5789(...)19968 [78] | 17.648 us | 1.1474 us | 3.3471 us | 16.050 us |
// | Bench  | 32     | -9223372036854775808 |  2.647 us | 0.0860 us | 0.2384 us |  2.600 us |
// | Bench  | 32     | -2147483648          |  2.655 us | 0.0905 us | 0.2447 us |  2.600 us |
// | Bench  | 32     | 0                    |  2.381 us | 0.0600 us | 0.1622 us |  2.300 us |
// | Bench  | 32     | 1                    |  2.525 us | 0.0914 us | 0.2533 us |  2.400 us |
// | Bench  | 32     | 2147483647           |  2.703 us | 0.1631 us | 0.4491 us |  2.500 us |
// | Bench  | 32     | 9223372036854775807  |  2.693 us | 0.1162 us | 0.3180 us |  2.600 us |
// | Bench  | 32     | 17014(...)05727 [39] |  2.635 us | 0.0944 us | 0.2600 us |  2.600 us |
// | Bench  | 32     | 57896(...)19967 [77] | 15.541 us | 0.7765 us | 2.2153 us | 14.600 us |
// | Bench  | 64     | -5789(...)19968 [78] | 17.519 us | 1.2186 us | 3.5548 us | 16.000 us |
// | Bench  | 64     | -9223372036854775808 |  2.508 us | 0.0636 us | 0.1720 us |  2.500 us |
// | Bench  | 64     | -2147483648          |  2.722 us | 0.0882 us | 0.2428 us |  2.600 us |
// | Bench  | 64     | 0                    |  2.381 us | 0.0980 us | 0.2699 us |  2.300 us |
// | Bench  | 64     | 1                    |  2.542 us | 0.0928 us | 0.2588 us |  2.500 us |
// | Bench  | 64     | 2147483647           |  2.690 us | 0.1310 us | 0.3607 us |  2.550 us |
// | Bench  | 64     | 9223372036854775807  |  2.562 us | 0.0839 us | 0.2340 us |  2.500 us |
// | Bench  | 64     | 17014(...)05727 [39] |  2.568 us | 0.0872 us | 0.2356 us |  2.500 us |
// | Bench  | 64     | 57896(...)19967 [77] | 15.586 us | 0.7423 us | 2.0814 us | 14.800 us |
// | Bench  | 128    | -5789(...)19968 [78] | 18.670 us | 1.2010 us | 3.5413 us | 18.400 us |
// | Bench  | 128    | -9223372036854775808 |  2.434 us | 0.0527 us | 0.0909 us |  2.450 us |
// | Bench  | 128    | -2147483648          |  2.622 us | 0.0969 us | 0.2702 us |  2.500 us |
// | Bench  | 128    | 0                    |  2.402 us | 0.1390 us | 0.3852 us |  2.200 us |
// | Bench  | 128    | 1                    |  2.520 us | 0.0757 us | 0.2046 us |  2.500 us |
// | Bench  | 128    | 2147483647           |  2.947 us | 0.2281 us | 0.6582 us |  2.700 us |
// | Bench  | 128    | 9223372036854775807  |  2.585 us | 0.0860 us | 0.2339 us |  2.500 us |
// | Bench  | 128    | 17014(...)05727 [39] |  2.553 us | 0.1049 us | 0.2889 us |  2.500 us |
// | Bench  | 128    | 57896(...)19967 [77] | 15.019 us | 0.6669 us | 1.7802 us | 14.400 us |
// | Bench  | 256    | -5789(...)19968 [78] | 16.017 us | 0.9051 us | 2.5676 us | 15.100 us |
// | Bench  | 256    | -9223372036854775808 | 15.927 us | 0.9452 us | 2.6967 us | 14.750 us |
// | Bench  | 256    | -2147483648          | 15.445 us | 0.6759 us | 1.8503 us | 14.800 us |
// | Bench  | 256    | 0                    |  2.274 us | 0.0714 us | 0.1956 us |  2.200 us |
// | Bench  | 256    | 1                    | 15.079 us | 0.7463 us | 2.0805 us | 14.300 us |
// | Bench  | 256    | 2147483647           | 18.140 us | 1.1967 us | 3.5285 us | 17.150 us |
// | Bench  | 256    | 9223372036854775807  | 15.482 us | 0.6804 us | 1.8626 us | 14.900 us |
// | Bench  | 256    | 17014(...)05727 [39] | 15.298 us | 0.6336 us | 1.7558 us | 14.700 us |
// | Bench  | 256    | 57896(...)19967 [77] | 14.780 us | 0.5659 us | 1.5204 us | 14.300 us |
