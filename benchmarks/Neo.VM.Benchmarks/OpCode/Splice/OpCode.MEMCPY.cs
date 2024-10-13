// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.MEMCPY.cs file belongs to the neo project and is free
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

public class OpCode_MEMCPY
{
    //     // INITSLOT 0100
    //     // PUSHINT32 1048576
    //     // NEWBUFFER
    //     // PUSHINT32 1048576
    //     // NEWBUFFER
    //     // PUSHINT32 133333337
    //     // STLOC 00
    //     // OVER
    //     // PUSH0
    //     // PUSH2
    //     // PICK
    //     // PUSH0
    //     // PUSHINT32 1048576
    //     // MEMCPY
    //     // LDLOC 00
    //     // DEC
    //     // STLOC 00
    //     // LDLOC 00
    //     // JMPIF_L eeffffff
    //     // CLEAR
    //     // RET

    private BenchmarkEngine _engine;

    private VM.OpCode Opcode => VM.OpCode.MEMCPY;

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
