// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.MODPOW.cs file belongs to the neo project and is free
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

public class OpCode_MODPOW
{
    [ParamsSource(nameof(ScriptParams))]
    public Script _script = new("0c04ffffff7f0c0100b8".HexToBytes());
    private BenchmarkEngine _engine;

    public static IEnumerable<Script> ScriptParams()
    {
        string[] scripts = [

        ];

        return scripts.Select(p => new Script(p.HexToBytes()));
    }

    [GlobalSetup]
    public void Setup()
    {
        _engine = new BenchmarkEngine();
        _engine.LoadScript(_script);

        const VM.OpCode Opcode = VM.OpCode.MODPOW;
        _engine.ExecuteUntil(Opcode);

#if DEBUG

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

        for (var i = 0; i < values.Length; i++)
        {
            for (var j = 0; j < values.Length; j++)
            {
                for (var k = 0; k < values.Length; k++)
                {
                    if (i != j && j != k && i != k && values[k] != BigInteger.Zero)
                    {
                        CreateBenchScript(values[i], values[j], values[k], Opcode);
                    }
                }
            }
        }

#endif

    }

    [Benchmark]
    public void Bench()
    {
        _engine.ExecuteNext();

    }

    void CreateBenchScript(BigInteger a, BigInteger b, BigInteger m, VM.OpCode opcode)
    {
        var builder = new InstructionBuilder();

        builder.Push(a);
        builder.Push(b);
        builder.Push(m);
        builder.AddInstruction(opcode);

        Console.WriteLine($"\"{builder.ToArray().ToHexString()}\",");
    }
}
