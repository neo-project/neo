// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PUSHINT8UtoUPUSHINT256.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System.Numerics;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_PUSHINT8UtoUPUSHINT256
{

    private BenchmarkEngine _engine;

    [ParamsSource(nameof(PushValues))]
    public BigInteger _value;

    public static IEnumerable<BigInteger> PushValues()
    {
        return
        [
            Benchmark_Opcode.MAX_INT,
            Benchmark_Opcode.MIN_INT,
            short.MaxValue,
            short.MinValue,
            ushort.MaxValue,
            ushort.MinValue,
            int.MaxValue,
            int.MinValue,
            uint.MaxValue,
            uint.MaxValue,
            ulong.MaxValue,
            ulong.MinValue,
            long.MaxValue,
            long.MinValue
        ];
    }

    [IterationSetup]
    public void Setup()
    {
        var builder = new InstructionBuilder();
        builder.AddInstruction(VM.OpCode.NOP);
        builder.Push(_value);
        _engine = new BenchmarkEngine();
        _engine.LoadScript(builder.ToArray());
        _engine.ExecuteUntil(VM.OpCode.NOP);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public void Bench() => _engine.ExecuteNext();
}


// | Method | _value               | Mean     | Error     | StdDev    | Median   |
//     |------- |--------------------- |---------:|----------:|----------:|---------:|
//     | Bench  | -5789(...)19968 [78] | 2.147 us | 0.2107 us | 0.6212 us | 1.800 us |
//     | Bench  | -9223372036854775808 | 1.605 us | 0.0549 us | 0.1456 us | 1.600 us |
//     | Bench  | -2147483648          | 1.967 us | 0.1781 us | 0.5251 us | 1.800 us |
//     | Bench  | -32768               | 1.600 us | 0.0487 us | 0.1375 us | 1.600 us |
//     | Bench  | 0                    | 1.665 us | 0.0819 us | 0.2213 us | 1.600 us |
//     | Bench  | 0                    | 1.679 us | 0.0535 us | 0.1456 us | 1.650 us |
//     | Bench  | 32767                | 1.580 us | 0.0688 us | 0.1813 us | 1.500 us |
//     | Bench  | 65535                | 1.597 us | 0.0506 us | 0.1376 us | 1.600 us |
//     | Bench  | 2147483647           | 1.609 us | 0.0476 us | 0.1288 us | 1.600 us |
//     | Bench  | 4294967295           | 1.605 us | 0.0459 us | 0.1296 us | 1.600 us |
//     | Bench  | 4294967295           | 1.613 us | 0.0469 us | 0.1299 us | 1.600 us |
//     | Bench  | 9223372036854775807  | 1.619 us | 0.0564 us | 0.1571 us | 1.600 us |
//     | Bench  | 18446744073709551615 | 1.650 us | 0.0537 us | 0.1478 us | 1.600 us |
//     | Bench  | 57896(...)19967 [77] | 1.613 us | 0.0562 us | 0.1539 us | 1.600 us |
