// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.LT.cs file belongs to the neo project and is free
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

public class OpCode_LT
{
    [ParamsSource(nameof(ScriptParams))]
    public Script _script = new("0c04ffffff7f0c0100b8".HexToBytes());
    private BenchmarkEngine _engine;

    public static IEnumerable<Script> ScriptParams()
    {
        string[] scripts = [
"05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080b5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080b5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fb5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fb5",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080b5",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb5",
            "0500000000000000000000000000000000000000000000000000000000000000800200000080b5",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7fb5",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fb5",
            "050000000000000000000000000000000000000000000000000000000000000080030000000000000080b5",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb5",
            "0200000080050000000000000000000000000000000000000000000000000000000000000080b5",
            "020000008002ffffff7fb5",
            "020000008003ffffffffffffff7fb5",
            "0200000080030000000000000080b5",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb5",
            "02ffffff7f050000000000000000000000000000000000000000000000000000000000000080b5",
            "02ffffff7f0200000080b5",
            "02ffffff7f03ffffffffffffff7fb5",
            "02ffffff7f030000000000000080b5",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb5",
            "03ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080b5",
            "03ffffffffffffff7f0200000080b5",
            "03ffffffffffffff7f02ffffff7fb5",
            "03ffffffffffffff7f030000000000000080b5",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb5",
            "030000000000000080050000000000000000000000000000000000000000000000000000000000000080b5",
            "0300000000000000800200000080b5",
            "03000000000000008002ffffff7fb5",
            "03000000000000008003ffffffffffffff7fb5",

        ];

        return scripts.Select(p => new Script(p.HexToBytes()));
    }

    [GlobalSetup]
    public void Setup()
    {
        _engine = new BenchmarkEngine();
        _engine.LoadScript(_script);

        const VM.OpCode Opcode = VM.OpCode.LT;
        _engine.ExecuteUntil(Opcode);

#if DEBUG

        var values = new BigInteger[]
        {
            Benchmark_Opcode.MAX_INT,
            Benchmark_Opcode.MIN_INT,
            int.MinValue,
            int.MaxValue,
            long.MaxValue,
            long.MinValue
        };

        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values.Length; j++)
            {
                if (i != j)
                {
                    CreateBenchScript(values[i], values[j], Opcode);
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

    void CreateBenchScript(BigInteger a, BigInteger b, VM.OpCode opcode)
    {
        var builder = new InstructionBuilder();

        builder.Push(a);
        builder.Push(b);
        builder.AddInstruction(opcode);

        Console.WriteLine($"\"{builder.ToArray().ToHexString()}\",");
    }
}

// BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
// Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
// .NET SDK 8.0.403
//   [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
//   DefaultJob : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
//
//
// | Method | _script       | Mean      | Error     | StdDev    |
// |------- |-------------- |----------:|----------:|----------:|
// | Bench  | Neo.VM.Script | 0.5784 ns | 0.0222 ns | 0.0208 ns |
// | Bench  | Neo.VM.Script | 0.5068 ns | 0.0165 ns | 0.0155 ns |
// | Bench  | Neo.VM.Script | 0.5130 ns | 0.0153 ns | 0.0143 ns |
// | Bench  | Neo.VM.Script | 0.5054 ns | 0.0203 ns | 0.0180 ns |
// | Bench  | Neo.VM.Script | 0.5031 ns | 0.0130 ns | 0.0121 ns |
// | Bench  | Neo.VM.Script | 0.5176 ns | 0.0222 ns | 0.0208 ns |
// | Bench  | Neo.VM.Script | 0.5072 ns | 0.0178 ns | 0.0167 ns |
// | Bench  | Neo.VM.Script | 0.5118 ns | 0.0191 ns | 0.0179 ns |
// | Bench  | Neo.VM.Script | 0.5063 ns | 0.0149 ns | 0.0140 ns |
// | Bench  | Neo.VM.Script | 0.5188 ns | 0.0259 ns | 0.0243 ns |
// | Bench  | Neo.VM.Script | 0.5210 ns | 0.0176 ns | 0.0164 ns |
// | Bench  | Neo.VM.Script | 0.5070 ns | 0.0193 ns | 0.0180 ns |
// | Bench  | Neo.VM.Script | 0.5106 ns | 0.0125 ns | 0.0117 ns |
// | Bench  | Neo.VM.Script | 0.5124 ns | 0.0205 ns | 0.0191 ns |
// | Bench  | Neo.VM.Script | 0.5156 ns | 0.0196 ns | 0.0184 ns |
// | Bench  | Neo.VM.Script | 0.4953 ns | 0.0109 ns | 0.0097 ns |
// | Bench  | Neo.VM.Script | 0.5143 ns | 0.0119 ns | 0.0111 ns |
// | Bench  | Neo.VM.Script | 0.5354 ns | 0.0154 ns | 0.0136 ns |
// | Bench  | Neo.VM.Script | 0.5272 ns | 0.0160 ns | 0.0149 ns |
// | Bench  | Neo.VM.Script | 0.5188 ns | 0.0098 ns | 0.0092 ns |
// | Bench  | Neo.VM.Script | 0.4983 ns | 0.0210 ns | 0.0196 ns |
// | Bench  | Neo.VM.Script | 0.5157 ns | 0.0146 ns | 0.0129 ns |
// | Bench  | Neo.VM.Script | 0.5213 ns | 0.0244 ns | 0.0228 ns |
// | Bench  | Neo.VM.Script | 0.5209 ns | 0.0059 ns | 0.0052 ns |
// | Bench  | Neo.VM.Script | 0.5185 ns | 0.0097 ns | 0.0091 ns |
// | Bench  | Neo.VM.Script | 0.5291 ns | 0.0136 ns | 0.0113 ns |
// | Bench  | Neo.VM.Script | 0.5088 ns | 0.0146 ns | 0.0129 ns |
// | Bench  | Neo.VM.Script | 0.5184 ns | 0.0178 ns | 0.0166 ns |
// | Bench  | Neo.VM.Script | 0.5152 ns | 0.0152 ns | 0.0142 ns |
// | Bench  | Neo.VM.Script | 0.5071 ns | 0.0154 ns | 0.0144 ns |
