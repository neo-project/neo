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
    [ParamsSource(nameof(ScriptParams))]
    public Script _script = new("0c04ffffff7f0c0100b8".HexToBytes());
    private BenchmarkEngine _engine;

    private const VM.OpCode Opcode = VM.OpCode.SHL;

    public static IEnumerable<Script> ScriptParams()
    {
        string[] scripts = [
"05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0078a8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0059a8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0070a8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f01db00a8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f006ea8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f01cb00a8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f01cd00a8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f01b800a8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f002ea8",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f018200a8",
            "0500000000000000000000000000000000000000000000000000000000000000801ea8",
            "0500000000000000000000000000000000000000000000000000000000000000800018a8",
            "05000000000000000000000000000000000000000000000000000000000000008001d100a8",
            "05000000000000000000000000000000000000000000000000000000000000008001a700a8",
            "050000000000000000000000000000000000000000000000000000000000000080004aa8",
            "050000000000000000000000000000000000000000000000000000000000000080018300a8",
            "050000000000000000000000000000000000000000000000000000000000000080004ba8",
            "05000000000000000000000000000000000000000000000000000000000000008001ae00a8",
            "050000000000000000000000000000000000000000000000000000000000000080019100a8",
            "05000000000000000000000000000000000000000000000000000000000000008001d100a8",
            "1101fb00a8",
            "1101c000a8",
            "110017a8",
            "11018800a8",
            "1117a8",
            "11005da8",
            "1101a600a8",
            "11006aa8",
            "110052a8",
            "1101bf00a8",
            "10019c00a8",
            "1001d100a8",
            "10018500a8",
            "1001ae00a8",
            "1001fa00a8",
            "100078a8",
            "10018700a8",
            "100017a8",
            "100016a8",
            "100055a8",
            "02ffffff7f1ea8",
            "02ffffff7f0079a8",
            "02ffffff7f0062a8",
            "02ffffff7f01f900a8",
            "02ffffff7f019000a8",
            "02ffffff7f0011a8",
            "02ffffff7f0029a8",
            "02ffffff7f019100a8",
            "02ffffff7f01b500a8",
            "02ffffff7f0015a8",
            "02000000800013a8",
            "02000000800052a8",
            "0200000080005da8",
            "020000008001e400a8",
            "0200000080018e00a8",
            "02000000801ca8",
            "020000008001fa00a8",
            "02000000800028a8",
            "020000008001b300a8",
            "0200000080002da8",
            "03ffffffffffffff7f01c900a8",
            "03ffffffffffffff7f0048a8",
            "03ffffffffffffff7f0071a8",
            "03ffffffffffffff7f0033a8",
            "03ffffffffffffff7f018500a8",
            "03ffffffffffffff7f0033a8",
            "03ffffffffffffff7f0069a8",
            "03ffffffffffffff7f0028a8",
            "03ffffffffffffff7f0037a8",
            "03ffffffffffffff7f002ca8",
            "030000000000000080018400a8",
            "03000000000000008001a900a8",
            "03000000000000008001c800a8",
            "03000000000000008001c000a8",
            "03000000000000008001be00a8",
            "030000000000000080010001a8",
            "03000000000000008001d600a8",
            "0300000000000000800022a8",
            "03000000000000008001a100a8",
            "03000000000000008001d700a8",
            "04ffffffffffffffffffffffffffffff7f019e00a8",
            "04ffffffffffffffffffffffffffffff7f003ea8",
            "04ffffffffffffffffffffffffffffff7f0014a8",
            "04ffffffffffffffffffffffffffffff7f019b00a8",
            "04ffffffffffffffffffffffffffffff7f007fa8",
            "04ffffffffffffffffffffffffffffff7f01ca00a8",
            "04ffffffffffffffffffffffffffffff7f01cb00a8",
            "04ffffffffffffffffffffffffffffff7f005fa8",
            "04ffffffffffffffffffffffffffffff7f019700a8",
            "04ffffffffffffffffffffffffffffff7f018000a8",


        ];

        return scripts.Select(p => new Script(p.HexToBytes()));
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
    public void Bench()
    {
        _engine.ExecuteNext();

    }

    [GenerateTests]
    public void CreateBenchScript()
    {
        var values = new BigInteger[]
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
        Console.WriteLine($"\"{builder.ToArray().ToHexString()}\",");
    }
}


// | Method | _script       | Mean      | Error     | StdDev    | Median    |
// |------- |-------------- |----------:|----------:|----------:|----------:|
// | Bench  | Neo.VM.Script | 14.699 us | 0.5136 us | 1.3708 us | 14.300 us |
// | Bench  | Neo.VM.Script | 16.433 us | 1.1801 us | 3.4610 us | 14.900 us |
// | Bench  | Neo.VM.Script | 15.445 us | 0.8731 us | 2.4627 us | 14.400 us |
// | Bench  | Neo.VM.Script | 17.297 us | 1.3914 us | 4.1026 us | 15.500 us |
// | Bench  | Neo.VM.Script | 16.856 us | 1.3829 us | 4.0775 us | 14.950 us |
// | Bench  | Neo.VM.Script | 15.129 us | 0.8729 us | 2.4620 us | 14.200 us |
// | Bench  | Neo.VM.Script | 14.429 us | 0.4559 us | 1.2248 us | 14.200 us |
// | Bench  | Neo.VM.Script | 16.249 us | 1.0937 us | 3.2077 us | 14.700 us |
// | Bench  | Neo.VM.Script | 15.737 us | 0.7761 us | 2.1762 us | 14.900 us |
// | Bench  | Neo.VM.Script |  2.472 us | 0.0524 us | 0.1161 us |  2.450 us |
// | Bench  | Neo.VM.Script | 18.154 us | 1.5437 us | 4.5516 us | 15.900 us |
// | Bench  | Neo.VM.Script | 18.571 us | 1.4612 us | 4.2856 us | 17.300 us |
// | Bench  | Neo.VM.Script | 18.907 us | 1.3956 us | 4.0712 us | 18.100 us |
// | Bench  | Neo.VM.Script | 14.653 us | 0.4779 us | 1.2591 us | 14.400 us |
// | Bench  | Neo.VM.Script | 18.495 us | 1.3308 us | 3.8819 us | 17.150 us |
// | Bench  | Neo.VM.Script | 14.418 us | 0.4578 us | 1.2300 us | 14.150 us |
// | Bench  | Neo.VM.Script | 14.512 us | 0.5221 us | 1.4116 us | 14.000 us |
// | Bench  | Neo.VM.Script | 18.420 us | 1.2555 us | 3.7017 us | 17.400 us |
// | Bench  | Neo.VM.Script | 15.151 us | 0.7952 us | 2.1903 us | 14.250 us |
// | Bench  | Neo.VM.Script | 14.542 us | 0.5094 us | 1.3771 us | 14.100 us |
// | Bench  | Neo.VM.Script | 18.792 us | 1.2005 us | 3.5398 us | 19.900 us |
// | Bench  | Neo.VM.Script |  2.386 us | 0.0494 us | 0.0783 us |  2.350 us |
// | Bench  | Neo.VM.Script |  2.482 us | 0.0733 us | 0.1944 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.255 us | 0.0765 us | 0.2067 us |  2.200 us |
// | Bench  | Neo.VM.Script |  2.502 us | 0.0968 us | 0.2567 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.517 us | 0.0935 us | 0.2512 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.540 us | 0.1102 us | 0.3071 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.553 us | 0.1093 us | 0.2973 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.490 us | 0.0816 us | 0.2122 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.223 us | 0.0457 us | 0.0859 us |  2.200 us |
// | Bench  | Neo.VM.Script |  2.308 us | 0.1377 us | 0.3723 us |  2.200 us |
// | Bench  | Neo.VM.Script |  2.233 us | 0.0665 us | 0.1830 us |  2.200 us |
// | Bench  | Neo.VM.Script |  2.393 us | 0.1419 us | 0.3884 us |  2.250 us |
// | Bench  | Neo.VM.Script |  2.284 us | 0.1026 us | 0.2790 us |  2.200 us |
// | Bench  | Neo.VM.Script |  2.628 us | 0.2179 us | 0.6321 us |  2.300 us |
// | Bench  | Neo.VM.Script |  2.212 us | 0.0631 us | 0.1718 us |  2.200 us |
// | Bench  | Neo.VM.Script |  2.205 us | 0.0470 us | 0.0872 us |  2.200 us |
// | Bench  | Neo.VM.Script |  2.142 us | 0.0422 us | 0.0578 us |  2.100 us |
// | Bench  | Neo.VM.Script |  2.119 us | 0.0457 us | 0.0776 us |  2.100 us |
// | Bench  | Neo.VM.Script |  2.489 us | 0.0951 us | 0.2603 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.326 us | 0.0497 us | 0.0817 us |  2.300 us |
// | Bench  | Neo.VM.Script |  2.442 us | 0.0830 us | 0.2229 us |  2.400 us |
// | Bench  | Neo.VM.Script | 14.402 us | 0.4666 us | 1.2773 us | 14.100 us |
// | Bench  | Neo.VM.Script | 14.419 us | 0.3766 us | 1.0052 us | 14.200 us |
// | Bench  | Neo.VM.Script |  2.317 us | 0.0499 us | 0.0747 us |  2.300 us |
// | Bench  | Neo.VM.Script |  3.065 us | 0.2598 us | 0.7453 us |  2.800 us |
// | Bench  | Neo.VM.Script |  2.467 us | 0.0525 us | 0.1012 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.327 us | 0.0487 us | 0.0667 us |  2.300 us |
// | Bench  | Neo.VM.Script |  2.422 us | 0.0748 us | 0.2059 us |  2.300 us |
// | Bench  | Neo.VM.Script |  2.448 us | 0.0743 us | 0.2034 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.548 us | 0.0890 us | 0.2392 us |  2.500 us |
// | Bench  | Neo.VM.Script |  2.444 us | 0.0522 us | 0.1270 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.756 us | 0.2055 us | 0.5830 us |  2.500 us |
// | Bench  | Neo.VM.Script | 17.585 us | 1.2607 us | 3.6973 us | 15.700 us |
// | Bench  | Neo.VM.Script |  2.495 us | 0.0683 us | 0.1859 us |  2.400 us |
// | Bench  | Neo.VM.Script | 16.858 us | 1.2710 us | 3.7475 us | 14.700 us |
// | Bench  | Neo.VM.Script |  2.480 us | 0.0841 us | 0.2246 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.429 us | 0.0520 us | 0.0898 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.582 us | 0.0903 us | 0.2441 us |  2.500 us |
// | Bench  | Neo.VM.Script |  2.770 us | 0.1672 us | 0.4633 us |  2.600 us |
// | Bench  | Neo.VM.Script | 16.526 us | 1.1621 us | 3.3899 us | 15.050 us |
// | Bench  | Neo.VM.Script |  2.906 us | 0.2254 us | 0.6575 us |  2.600 us |
// | Bench  | Neo.VM.Script |  2.546 us | 0.1100 us | 0.2955 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.361 us | 0.0501 us | 0.0838 us |  2.400 us |
// | Bench  | Neo.VM.Script |  3.065 us | 0.2947 us | 0.8595 us |  2.650 us |
// | Bench  | Neo.VM.Script |  2.581 us | 0.1012 us | 0.2736 us |  2.500 us |
// | Bench  | Neo.VM.Script |  2.357 us | 0.0856 us | 0.2286 us |  2.300 us |
// | Bench  | Neo.VM.Script |  2.640 us | 0.1280 us | 0.3525 us |  2.500 us |
// | Bench  | Neo.VM.Script |  2.553 us | 0.0624 us | 0.1644 us |  2.500 us |
// | Bench  | Neo.VM.Script |  2.385 us | 0.0517 us | 0.1287 us |  2.400 us |
// | Bench  | Neo.VM.Script |  3.384 us | 0.3190 us | 0.9406 us |  2.950 us |
// | Bench  | Neo.VM.Script |  3.449 us | 0.3644 us | 1.0745 us |  2.900 us |
// | Bench  | Neo.VM.Script |  2.471 us | 0.0532 us | 0.1438 us |  2.400 us |
// | Bench  | Neo.VM.Script | 16.819 us | 1.2016 us | 3.5241 us | 15.350 us |
// | Bench  | Neo.VM.Script |  3.322 us | 0.3141 us | 0.9162 us |  2.950 us |
// | Bench  | Neo.VM.Script |  3.003 us | 0.2269 us | 0.6436 us |  2.700 us |
// | Bench  | Neo.VM.Script | 19.699 us | 1.4966 us | 4.3657 us | 18.750 us |
// | Bench  | Neo.VM.Script | 16.780 us | 1.1318 us | 3.2473 us | 15.600 us |
// | Bench  | Neo.VM.Script |  3.269 us | 0.3387 us | 0.9718 us |  2.900 us |
// | Bench  | Neo.VM.Script |  2.639 us | 0.1334 us | 0.3675 us |  2.500 us |
// | Bench  | Neo.VM.Script | 19.294 us | 1.4098 us | 4.1346 us | 19.000 us |
// | Bench  | Neo.VM.Script | 18.398 us | 1.2986 us | 3.8290 us | 17.500 us |
// | Bench  | Neo.VM.Script |  2.873 us | 0.2337 us | 0.6513 us |  2.500 us |
// | Bench  | Neo.VM.Script |  2.800 us | 0.1874 us | 0.5162 us |  2.600 us |
// | Bench  | Neo.VM.Script | 14.185 us | 0.3480 us | 0.9469 us | 14.050 us |
// | Bench  | Neo.VM.Script |  2.887 us | 0.2344 us | 0.6611 us |  2.550 us |
// | Bench  | Neo.VM.Script | 17.584 us | 1.2138 us | 3.5599 us | 16.000 us |
// | Bench  | Neo.VM.Script | 21.493 us | 2.2795 us | 6.6854 us | 20.700 us |
// | Bench  | Neo.VM.Script |  2.464 us | 0.0668 us | 0.1795 us |  2.400 us |
// | Bench  | Neo.VM.Script |  2.755 us | 0.1976 us | 0.5637 us |  2.500 us |
