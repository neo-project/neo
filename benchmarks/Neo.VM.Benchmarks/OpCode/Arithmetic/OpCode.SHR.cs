// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SHR.cs file belongs to the neo project and is free
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

public class OpCode_SHR
{
    [ParamsSource(nameof(ScriptParams))]
    public Script _script = new("0c04ffffff7f0c0100b8".HexToBytes());
    private BenchmarkEngine _engine;

    private const VM.OpCode Opcode = VM.OpCode.SHR;

    public static IEnumerable<Script> ScriptParams()
    {
        string[] scripts = [
"Bf////////////////////////////////////////9/AFmp",
            "Bf////////////////////////////////////////9/Ab4AqQ==",
            "Bf////////////////////////////////////////9/AC6p",
            "Bf////////////////////////////////////////9/AckAqQ==",
            "Bf////////////////////////////////////////9/AZoAqQ==",
            "Bf////////////////////////////////////////9/ADup",
            "Bf////////////////////////////////////////9/AdYAqQ==",
            "Bf////////////////////////////////////////9/AdoAqQ==",
            "Bf////////////////////////////////////////9/AC+p",
            "Bf////////////////////////////////////////9/AaYAqQ==",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAFWp",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAGmp",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAEqp",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAGOp",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAdkAqQ==",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAHWp",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAbIAqQ==",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAcoAqQ==",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAdIAqQ==",
            "BQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAAGWp",
            "EQBTqQ==",
            "EQGsAKk=",
            "EQAaqQ==",
            "EQHjAKk=",
            "EQBDqQ==",
            "EQB8qQ==",
            "EQAgqQ==",
            "EQBuqQ==",
            "EQAwqQ==",
            "EQGJAKk=",
            "EAGqAKk=",
            "EAGyAKk=",
            "EAHHAKk=",
            "EAGzAKk=",
            "EByp",
            "EAH0AKk=",
            "EABZqQ==",
            "EAHNAKk=",
            "EBip",
            "EAA7qQ==",
            "Av///38AMqk=",
            "Av///38Abak=",
            "Av///38AU6k=",
            "Av///38AF6k=",
            "Av///38BhwCp",
            "Av///38B0wCp",
            "Av///38ANqk=",
            "Av///38eqQ==",
            "Av///38B4QCp",
            "Av///38AL6k=",
            "AgAAAIAB6wCp",
            "AgAAAIABmACp",
            "AgAAAIAAIqk=",
            "AgAAAIAAH6k=",
            "AgAAAIAAIKk=",
            "AgAAAIABvQCp",
            "AgAAAIAAUak=",
            "AgAAAIAB/QCp",
            "AgAAAIABAAGp",
            "AgAAAIAAHKk=",
            "A/////////9/AcUAqQ==",
            "A/////////9/AGKp",
            "A/////////9/AegAqQ==",
            "A/////////9/AcsAqQ==",
            "A/////////9/AGGp",
            "A/////////9/AaIAqQ==",
            "A/////////9/AGGp",
            "A/////////9/Fak=",
            "A/////////9/AZcAqQ==",
            "A/////////9/ABOp",
            "AwAAAAAAAACAAFOp",
            "AwAAAAAAAACAAa4AqQ==",
            "AwAAAAAAAACAAa4AqQ==",
            "AwAAAAAAAACAADqp",
            "AwAAAAAAAACAAEyp",
            "AwAAAAAAAACAAYQAqQ==",
            "AwAAAAAAAACAABmp",
            "AwAAAAAAAACAAZYAqQ==",
            "AwAAAAAAAACAACqp",
            "AwAAAAAAAACAAHap",
            "BP///////////////////38B+gCp",
            "BP///////////////////38BwwCp",
            "BP///////////////////38Aaak=",
            "BP///////////////////38B0wCp",
            "BP///////////////////38BogCp",
            "BP///////////////////38TqQ==",
            "BP///////////////////38B9QCp",
            "BP///////////////////38BrgCp",
            "BP///////////////////38B2ACp",
            "BP///////////////////38B0QCp",
        ];

        return scripts.Select(p => new Script(Convert.FromBase64String(p)));
    }

    [IterationSetup]
    public void Setup()
    {
        _engine = new BenchmarkEngine();
        _engine.LoadScript(_script);
        _engine.ExecuteUntil(Opcode);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public void Bench() => _engine.ExecuteNext();

    [GenerateTests]
    public void CreateBenchScript()
    {
        var values = new[]
        {
            Benchmark_Opcode.MAX_INT,
            Benchmark_Opcode.MIN_INT,
            BigInteger.One,
            BigInteger.Zero,
            int.MaxValue,
            int.MinValue,
            long.MaxValue,
            long.MinValue,
            BigInteger.Parse("170141183460469231731687303715884105727"),  // Mersenne prime 2^127 - 1
        };

        foreach (var t in values)
        {
            for (var j = 0; j < 10; j++)
            {
                var shift = Random.Shared.Next(0, 257);
                CreateBenchScript(t, shift, Opcode);
            }
        }
    }

    void CreateBenchScript(BigInteger a, BigInteger b, VM.OpCode opcode)
    {
        var builder = new InstructionBuilder();
        builder.Push(a);
        builder.Push(b);
        builder.AddInstruction(opcode);
        Console.WriteLine($"\"{Convert.ToBase64String(builder.ToArray())}\",");
    }
}

//
// | Method | _script       | Mean     | Error     | StdDev    | Median   |
// |------- |-------------- |---------:|----------:|----------:|---------:|
// | Bench  | Neo.VM.Script | 2.464 us | 0.0478 us | 0.0638 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.608 us | 0.0714 us | 0.1967 us | 2.500 us |
// | Bench  | Neo.VM.Script | 3.159 us | 0.2751 us | 0.8067 us | 2.800 us |
// | Bench  | Neo.VM.Script | 2.912 us | 0.2281 us | 0.6435 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.262 us | 0.2929 us | 0.8591 us | 2.900 us |
// | Bench  | Neo.VM.Script | 3.004 us | 0.2262 us | 0.6380 us | 2.700 us |
// | Bench  | Neo.VM.Script | 3.252 us | 0.2857 us | 0.8242 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.672 us | 0.0980 us | 0.2699 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.911 us | 0.2144 us | 0.6046 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.442 us | 0.0810 us | 0.2190 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.567 us | 0.0678 us | 0.1809 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.981 us | 0.2118 us | 0.5940 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.494 us | 0.0446 us | 0.0870 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.667 us | 0.0695 us | 0.1950 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.808 us | 0.1261 us | 0.3387 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.631 us | 0.0954 us | 0.2628 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.589 us | 0.0491 us | 0.0820 us | 2.600 us |
// | Bench  | Neo.VM.Script | 2.968 us | 0.2116 us | 0.5933 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.548 us | 0.0549 us | 0.1272 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.877 us | 0.2061 us | 0.5606 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.492 us | 0.1423 us | 0.3943 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.361 us | 0.0912 us | 0.2464 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.378 us | 0.0908 us | 0.2471 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.180 us | 0.0443 us | 0.0414 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.259 us | 0.0794 us | 0.2160 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.200 us | 0.0467 us | 0.0767 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.405 us | 0.1045 us | 0.2860 us | 2.350 us |
// | Bench  | Neo.VM.Script | 2.338 us | 0.0926 us | 0.2488 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.328 us | 0.1132 us | 0.3061 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.249 us | 0.0804 us | 0.2227 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.287 us | 0.0534 us | 0.1461 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.237 us | 0.0489 us | 0.1304 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.336 us | 0.1069 us | 0.2925 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.362 us | 0.1027 us | 0.2862 us | 2.250 us |
// | Bench  | Neo.VM.Script | 2.326 us | 0.1022 us | 0.2797 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.370 us | 0.1232 us | 0.3310 us | 2.250 us |
// | Bench  | Neo.VM.Script | 2.366 us | 0.0777 us | 0.2190 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.377 us | 0.0864 us | 0.2350 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.227 us | 0.0446 us | 0.0921 us | 2.250 us |
// | Bench  | Neo.VM.Script | 2.208 us | 0.0455 us | 0.1239 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.294 us | 0.0972 us | 0.2660 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.346 us | 0.1016 us | 0.2731 us | 2.250 us |
// | Bench  | Neo.VM.Script | 3.270 us | 0.3244 us | 0.9566 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.257 us | 0.0717 us | 0.1976 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.163 us | 0.0472 us | 0.1284 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.217 us | 0.0478 us | 0.0785 us | 2.200 us |
// | Bench  | Neo.VM.Script | 3.430 us | 0.3143 us | 0.9219 us | 3.200 us |
// | Bench  | Neo.VM.Script | 2.426 us | 0.1066 us | 0.2828 us | 2.300 us |
// | Bench  | Neo.VM.Script | 3.037 us | 0.2926 us | 0.8582 us | 2.650 us |
// | Bench  | Neo.VM.Script | 2.175 us | 0.0473 us | 0.1143 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.727 us | 0.1786 us | 0.5095 us | 2.550 us |
// | Bench  | Neo.VM.Script | 2.771 us | 0.2337 us | 0.6705 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.364 us | 0.0803 us | 0.2170 us | 2.300 us |
// | Bench  | Neo.VM.Script | 3.391 us | 0.3043 us | 0.8877 us | 3.200 us |
// | Bench  | Neo.VM.Script | 2.298 us | 0.0807 us | 0.2182 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.952 us | 0.2802 us | 0.8261 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.803 us | 0.1886 us | 0.5256 us | 2.600 us |
// | Bench  | Neo.VM.Script | 3.065 us | 0.3063 us | 0.9030 us | 2.650 us |
// | Bench  | Neo.VM.Script | 2.244 us | 0.0488 us | 0.0759 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.421 us | 0.0523 us | 0.0845 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.384 us | 0.0927 us | 0.2538 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.372 us | 0.1038 us | 0.2806 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.562 us | 0.1632 us | 0.4521 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.413 us | 0.0987 us | 0.2667 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.857 us | 0.2743 us | 0.8045 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.348 us | 0.0811 us | 0.2220 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.303 us | 0.0404 us | 0.0912 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.728 us | 0.1659 us | 0.4597 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.471 us | 0.1379 us | 0.3775 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.301 us | 0.1037 us | 0.2822 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.596 us | 0.1276 us | 0.3448 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.325 us | 0.1326 us | 0.3585 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.170 us | 0.0753 us | 0.1998 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.208 us | 0.0901 us | 0.2451 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.627 us | 0.1452 us | 0.3951 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.205 us | 0.0875 us | 0.2365 us | 2.200 us |
// | Bench  | Neo.VM.Script | 2.094 us | 0.0414 us | 0.0983 us | 2.100 us |
// | Bench  | Neo.VM.Script | 2.659 us | 0.1378 us | 0.3771 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.227 us | 0.0937 us | 0.2644 us | 2.200 us |
// | Bench  | Neo.VM.Script | 3.794 us | 0.3559 us | 1.0326 us | 3.900 us |
// | Bench  | Neo.VM.Script | 2.850 us | 0.2905 us | 0.8380 us | 2.500 us |
// | Bench  | Neo.VM.Script | 2.325 us | 0.1031 us | 0.2821 us | 2.300 us |
// | Bench  | Neo.VM.Script | 3.002 us | 0.2797 us | 0.8115 us | 2.800 us |
// | Bench  | Neo.VM.Script | 3.166 us | 0.2537 us | 0.7278 us | 3.000 us |
// | Bench  | Neo.VM.Script | 2.423 us | 0.1205 us | 0.3278 us | 2.300 us |
// | Bench  | Neo.VM.Script | 2.619 us | 0.2462 us | 0.6902 us | 2.400 us |
// | Bench  | Neo.VM.Script | 2.928 us | 0.2063 us | 0.5717 us | 2.700 us |
// | Bench  | Neo.VM.Script | 2.340 us | 0.0606 us | 0.1699 us | 2.300 us |
// | Bench  | Neo.VM.Script | 3.099 us | 0.2616 us | 0.7589 us | 2.900 us |
// | Bench  | Neo.VM.Script | 2.362 us | 0.1164 us | 0.3127 us | 2.300 us |
