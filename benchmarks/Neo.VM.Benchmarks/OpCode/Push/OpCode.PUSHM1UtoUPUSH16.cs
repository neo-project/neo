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

    [ParamsSource(nameof(StrLen))]
    public BigInteger _value;

    public const int MAX_LEN = ushort.MaxValue;


    public static IEnumerable<BigInteger> StrLen =>
    [
        0,
        1,
        MAX_LEN / 2,
        MAX_LEN / 4
    ];

    [IterationSetup]
    public void Setup()
    {
        var builder = new InstructionBuilder();
        builder.Push(MAX_LEN);
        builder.AddInstruction(VM.OpCode.NEWBUFFER);
        builder.AddInstruction(new Instruction
        {
            _opCode = VM.OpCode.CONVERT,
            _operand = [(byte)StackItemType.ByteString]
        });
        builder.Push(0);
        builder.Push(_value);
        builder.AddInstruction(VM.OpCode.SUBSTR);

        builder.Push(_value);

        _engine = new BenchmarkEngine();
        _engine.LoadScript(builder.ToArray());
        _engine.ExecuteUntil(VM.OpCode.SUBSTR);
        _engine.ExecuteNext();

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


// | Method | _value | Mean     | Error    | StdDev   | Median   |
//     |------- |------- |---------:|---------:|---------:|---------:|
//     | Bench  | -1     | 12.69 us | 0.243 us | 0.260 us | 12.70 us |
//     | Bench  | 0      | 12.70 us | 0.245 us | 0.375 us | 12.60 us |
//     | Bench  | 1      | 13.00 us | 0.261 us | 0.457 us | 12.90 us |
//     | Bench  | 2      | 12.59 us | 0.293 us | 0.821 us | 12.70 us |
//     | Bench  | 3      | 12.65 us | 0.231 us | 0.237 us | 12.60 us |
//     | Bench  | 4      | 12.76 us | 0.257 us | 0.285 us | 12.70 us |
//     | Bench  | 5      | 12.81 us | 0.257 us | 0.369 us | 12.80 us |
//     | Bench  | 6      | 12.87 us | 0.250 us | 0.334 us | 12.80 us |
//     | Bench  | 7      | 14.25 us | 0.662 us | 1.889 us | 13.40 us |
//     | Bench  | 8      | 13.50 us | 0.271 us | 0.718 us | 13.30 us |
//     | Bench  | 9      | 11.35 us | 0.167 us | 0.139 us | 11.40 us |
//     | Bench  | 10     | 13.04 us | 0.265 us | 0.456 us | 13.00 us |
//     | Bench  | 11     | 12.91 us | 0.248 us | 0.387 us | 12.95 us |
//     | Bench  | 12     | 12.92 us | 0.262 us | 0.416 us | 12.80 us |
//     | Bench  | 13     | 13.09 us | 0.254 us | 0.411 us | 13.00 us |
//     | Bench  | 14     | 13.03 us | 0.258 us | 0.451 us | 13.05 us |
//     | Bench  | 15     | 13.45 us | 0.273 us | 0.545 us | 13.40 us |
//     | Bench  | 16     | 12.88 us | 0.230 us | 0.192 us | 12.80 us |
