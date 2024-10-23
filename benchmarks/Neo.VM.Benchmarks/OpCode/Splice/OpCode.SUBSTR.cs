// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.SUBSTR.cs file belongs to the neo project and is free
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

public class OpCode_SUBSTR
{
    protected VM.OpCode Opcode => VM.OpCode.SUBSTR;

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
//     | Bench  | 0      | 2.940 us | 0.2876 us | 0.8479 us | 2.500 us |
//     | Bench  | 1      | 2.458 us | 0.0779 us | 0.2078 us | 2.400 us |
//     | Bench  | 32767  | 3.034 us | 0.0639 us | 0.1706 us | 3.000 us |
//     | Bench  | 65535  | 3.820 us | 0.1848 us | 0.4869 us | 3.600 us |

// | Method | _value | Mean     | Error     | StdDev    | Median   |
//     |------- |------- |---------:|----------:|----------:|---------:|
//     | Bench  | 0      | 2.506 us | 0.0670 us | 0.1868 us | 2.500 us |
//     | Bench  | 1      | 2.390 us | 0.0627 us | 0.1684 us | 2.300 us |
//     | Bench  | 32767  | 4.342 us | 0.4640 us | 1.3681 us | 3.600 us |
//     | Bench  | 65535  | 3.597 us | 0.0758 us | 0.1328 us | 3.600 us |

// | Method | _value | Mean     | Error     | StdDev    | Median   |
//     |------- |------- |---------:|----------:|----------:|---------:|
//     | Bench  | 0      | 2.335 us | 0.0616 us | 0.1706 us | 2.300 us |
//     | Bench  | 1      | 2.443 us | 0.0610 us | 0.1638 us | 2.400 us |
//     | Bench  | 16383  | 2.895 us | 0.1021 us | 0.2708 us | 2.800 us |
//     | Bench  | 32767  | 3.072 us | 0.0870 us | 0.2323 us | 3.000 us |
