// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.PUSHM1UtoUPUSH16.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_PUSHM1UtoUPUSH16
{
    protected VM.OpCode Opcode => VM.OpCode.PUSHM1;

    private BenchmarkEngine _engine;

    [ParamsSource(nameof(PushValues))]
    public int _value;

    public static IEnumerable<int> PushValues => Enumerable.Range(-1, 18);

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


// | Method | _value | Mean     | Error     | StdDev    | Median   |
//     |------- |------- |---------:|----------:|----------:|---------:|
//     | Bench  | -1     | 2.105 us | 0.1722 us | 0.5049 us | 1.800 us |
//     | Bench  | 0      | 1.717 us | 0.0486 us | 0.1331 us | 1.700 us |
//     | Bench  | 1      | 1.765 us | 0.0951 us | 0.2571 us | 1.700 us |
//     | Bench  | 2      | 1.764 us | 0.0453 us | 0.1269 us | 1.700 us |
//     | Bench  | 3      | 1.643 us | 0.0463 us | 0.1261 us | 1.600 us |
//     | Bench  | 4      | 1.710 us | 0.0522 us | 0.1446 us | 1.700 us |
//     | Bench  | 5      | 1.675 us | 0.0462 us | 0.1241 us | 1.700 us |
//     | Bench  | 6      | 1.661 us | 0.0595 us | 0.1577 us | 1.600 us |
//     | Bench  | 7      | 1.781 us | 0.0920 us | 0.2549 us | 1.700 us |
//     | Bench  | 8      | 1.605 us | 0.0575 us | 0.1564 us | 1.600 us |
//     | Bench  | 9      | 1.739 us | 0.0484 us | 0.1299 us | 1.700 us |
//     | Bench  | 10     | 2.027 us | 0.1835 us | 0.5381 us | 1.800 us |
//     | Bench  | 11     | 1.750 us | 0.0931 us | 0.2533 us | 1.700 us |
//     | Bench  | 12     | 1.671 us | 0.0753 us | 0.1969 us | 1.600 us |
//     | Bench  | 13     | 1.633 us | 0.0516 us | 0.1420 us | 1.600 us |
//     | Bench  | 14     | 1.638 us | 0.0360 us | 0.0804 us | 1.600 us |
//     | Bench  | 15     | 1.694 us | 0.0863 us | 0.2348 us | 1.600 us |
//     | Bench  | 16     | 1.649 us | 0.0443 us | 0.1205 us | 1.600 us |

