// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.LE.cs file belongs to the neo project and is free
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

public class OpCode_LE
{
    [ParamsSource(nameof(ScriptParams))]
    public Script _script = new("0c04ffffff7f0c0100b8".HexToBytes());
    private BenchmarkEngine _engine;

    public static IEnumerable<Script> ScriptParams()
    {
        string[] scripts = [
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080b6",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fb6",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fb6",
            "05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080b6",
            "05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb6",
            "0500000000000000000000000000000000000000000000000000000000000000800200000080b6",
            "05000000000000000000000000000000000000000000000000000000000000008002ffffff7fb6",
            "05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fb6",
            "050000000000000000000000000000000000000000000000000000000000000080030000000000000080b6",
            "020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb6",
            "0200000080050000000000000000000000000000000000000000000000000000000000000080b6",
            "020000008002ffffff7fb6",
            "020000008003ffffffffffffff7fb6",
            "0200000080030000000000000080b6",
            "02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb6",
            "02ffffff7f050000000000000000000000000000000000000000000000000000000000000080b6",
            "02ffffff7f0200000080b6",
            "02ffffff7f03ffffffffffffff7fb6",
            "02ffffff7f030000000000000080b6",
            "03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb6",
            "03ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080b6",
            "03ffffffffffffff7f0200000080b6",
            "03ffffffffffffff7f02ffffff7fb6",
            "03ffffffffffffff7f030000000000000080b6",
            "03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb6",
            "030000000000000080050000000000000000000000000000000000000000000000000000000000000080b6",
            "0300000000000000800200000080b6",
            "03000000000000008002ffffff7fb6",
            "03000000000000008003ffffffffffffff7fb6",
        ];

        return scripts.Select(p => new Script(p.HexToBytes()));
    }

    [GlobalSetup]
    public void Setup()
    {
        _engine = new BenchmarkEngine();
        _engine.LoadScript(_script);

        const VM.OpCode Opcode = VM.OpCode.LE;
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
// | Bench  | Neo.VM.Script | 0.5123 ns | 0.0305 ns | 0.0313 ns |
// | Bench  | Neo.VM.Script | 0.4724 ns | 0.0144 ns | 0.0128 ns |
// | Bench  | Neo.VM.Script | 0.4907 ns | 0.0131 ns | 0.0116 ns |
// | Bench  | Neo.VM.Script | 0.4568 ns | 0.0058 ns | 0.0054 ns |
// | Bench  | Neo.VM.Script | 0.4934 ns | 0.0099 ns | 0.0083 ns |
// | Bench  | Neo.VM.Script | 0.4827 ns | 0.0078 ns | 0.0061 ns |
// | Bench  | Neo.VM.Script | 0.4803 ns | 0.0092 ns | 0.0082 ns |
// | Bench  | Neo.VM.Script | 0.4837 ns | 0.0083 ns | 0.0074 ns |
// | Bench  | Neo.VM.Script | 0.4819 ns | 0.0124 ns | 0.0116 ns |
// | Bench  | Neo.VM.Script | 0.4732 ns | 0.0111 ns | 0.0104 ns |
// | Bench  | Neo.VM.Script | 0.4838 ns | 0.0099 ns | 0.0088 ns |
// | Bench  | Neo.VM.Script | 0.4852 ns | 0.0118 ns | 0.0110 ns |
// | Bench  | Neo.VM.Script | 0.4936 ns | 0.0135 ns | 0.0112 ns |
// | Bench  | Neo.VM.Script | 0.4859 ns | 0.0133 ns | 0.0124 ns |
// | Bench  | Neo.VM.Script | 0.4911 ns | 0.0140 ns | 0.0124 ns |
// | Bench  | Neo.VM.Script | 0.4835 ns | 0.0183 ns | 0.0171 ns |
// | Bench  | Neo.VM.Script | 0.5251 ns | 0.0235 ns | 0.0220 ns |
// | Bench  | Neo.VM.Script | 0.5161 ns | 0.0231 ns | 0.0216 ns |
// | Bench  | Neo.VM.Script | 0.4900 ns | 0.0152 ns | 0.0143 ns |
// | Bench  | Neo.VM.Script | 0.4836 ns | 0.0212 ns | 0.0198 ns |
// | Bench  | Neo.VM.Script | 0.5080 ns | 0.0189 ns | 0.0167 ns |
// | Bench  | Neo.VM.Script | 0.4873 ns | 0.0312 ns | 0.0291 ns |
// | Bench  | Neo.VM.Script | 0.5540 ns | 0.0276 ns | 0.0258 ns |
// | Bench  | Neo.VM.Script | 0.5072 ns | 0.0206 ns | 0.0192 ns |
// | Bench  | Neo.VM.Script | 0.4934 ns | 0.0276 ns | 0.0245 ns |
// | Bench  | Neo.VM.Script | 0.5319 ns | 0.0303 ns | 0.0297 ns |
// | Bench  | Neo.VM.Script | 0.4841 ns | 0.0075 ns | 0.0070 ns |
// | Bench  | Neo.VM.Script | 0.4853 ns | 0.0129 ns | 0.0121 ns |
// | Bench  | Neo.VM.Script | 0.4828 ns | 0.0076 ns | 0.0071 ns |
