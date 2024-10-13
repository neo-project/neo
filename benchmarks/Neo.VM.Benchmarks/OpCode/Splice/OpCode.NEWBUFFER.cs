// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.NEWBUFFER.cs file belongs to the neo project and is free
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

public class OpCode_NEWBUFFER
{

    private BenchmarkEngine _engine;

    private VM.OpCode Opcode => VM.OpCode.NEWBUFFER;

    [ParamsSource(nameof(Values))]
    public BigInteger _value;


    public static IEnumerable<BigInteger> Values =>
    [
       ushort.MaxValue * 2,
        ushort.MaxValue
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

// | Method | _value | Mean     | Error     | StdDev    | Median   |
//     |------- |------- |---------:|----------:|----------:|---------:|
//     | Bench  | 65535  | 4.116 us | 0.4132 us | 1.1987 us | 3.500 us |
//     | Bench  | 131070 | 4.003 us | 0.0833 us | 0.1879 us | 4.000 us |
