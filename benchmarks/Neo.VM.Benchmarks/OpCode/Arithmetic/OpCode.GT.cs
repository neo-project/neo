// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.GT.cs file belongs to the neo project and is free
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

namespace Neo.VM.Benchmark.OpCode
{
    public class OpCode_GT
    {
        [ParamsSource(nameof(ScriptParams))]
        public Script _script = new Script("0c04ffffff7f0c0100b8".HexToBytes());
        private BenchmarkEngine _engine;

        public static IEnumerable<Script> ScriptParams()
        {
            return
            [
                new Script("05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080b7".HexToBytes()),
                new Script("05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f0200000080b7".HexToBytes()),
                new Script("05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f02ffffff7fb7".HexToBytes()),
                new Script("05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f03ffffffffffffff7fb7".HexToBytes()),
                new Script("05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f030000000000000080b7".HexToBytes()),
                new Script("05000000000000000000000000000000000000000000000000000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb7".HexToBytes()),
                new Script("0500000000000000000000000000000000000000000000000000000000000000800200000080b7".HexToBytes()),
                new Script("05000000000000000000000000000000000000000000000000000000000000008002ffffff7fb7".HexToBytes()),
                new Script("05000000000000000000000000000000000000000000000000000000000000008003ffffffffffffff7fb7".HexToBytes()),
                new Script("050000000000000000000000000000000000000000000000000000000000000080030000000000000080b7".HexToBytes()),
                new Script("020000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb7".HexToBytes()),
                new Script("0200000080050000000000000000000000000000000000000000000000000000000000000080b7".HexToBytes()),
                new Script("020000008002ffffff7fb7".HexToBytes()),
                new Script("020000008003ffffffffffffff7fb7".HexToBytes()),
                new Script("0200000080030000000000000080b7".HexToBytes()),
                new Script("02ffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb7".HexToBytes()),
                new Script("02ffffff7f050000000000000000000000000000000000000000000000000000000000000080b7".HexToBytes()),
                new Script("02ffffff7f0200000080b7".HexToBytes()),
                new Script("02ffffff7f03ffffffffffffff7fb7".HexToBytes()),
                new Script("02ffffff7f030000000000000080b7".HexToBytes()),
                new Script("03ffffffffffffff7f05ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb7".HexToBytes()),
                new Script("03ffffffffffffff7f050000000000000000000000000000000000000000000000000000000000000080b7".HexToBytes()),
                new Script("03ffffffffffffff7f0200000080b7".HexToBytes()),
                new Script("03ffffffffffffff7f02ffffff7fb7".HexToBytes()),
                new Script("03ffffffffffffff7f030000000000000080b7".HexToBytes()),
                new Script("03000000000000008005ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7fb7".HexToBytes()),
                new Script("030000000000000080050000000000000000000000000000000000000000000000000000000000000080b7".HexToBytes()),
                new Script("0300000000000000800200000080b7".HexToBytes()),
                new Script("03000000000000008002ffffff7fb7".HexToBytes()),
                new Script("03000000000000008003ffffffffffffff7fb7".HexToBytes()),

            ];
        }

        [GlobalSetup]
        public void Setup()
        {
            _engine = new BenchmarkEngine();
            _engine.LoadScript(_script);
            _engine.ExecuteUntil(VM.OpCode.GT);
        }

        [Benchmark]
        public void Bench_GT()
        {

            _engine.ExecuteNext();
            // var values = new BigInteger[]
            // {
            //     Benchmark_Opcode.MAX_INT,
            //     Benchmark_Opcode.MIN_INT,
            //     int.MinValue,
            //     int.MaxValue,
            //     long.MaxValue,
            //     long.MinValue
            // };
            //
            // for (int i = 0; i < values.Length; i++)
            // {
            //     for (int j = 0; j < values.Length; j++)
            //     {
            //         if (i != j)
            //         {
            //             CreateBenchScript(values[i], values[j]);
            //         }
            //     }
            // }

        }

        void CreateBenchScript(BigInteger a, BigInteger b)
        {
            var builder = new InstructionBuilder();

            builder.Push(a);
            builder.Push(b);
            builder.AddInstruction(VM.OpCode.GT);

            Console.WriteLine(builder.ToArray().ToHexString());
        }

    }

    // BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.4317/23H2/2023Update/SunValley3)
    // Intel Core i9-14900HX, 1 CPU, 32 logical and 24 physical cores
    // .NET SDK 8.0.403
    //   [Host]     : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
    //   DefaultJob : .NET 8.0.10 (8.0.1024.46610), X64 RyuJIT AVX2
    //
    //
    // | Method   | _script       | Mean      | Error     | StdDev    |
    // |--------- |-------------- |----------:|----------:|----------:|
    // | Bench_GT | Neo.VM.Script | 0.4850 ns | 0.0113 ns | 0.0100 ns |
    // | Bench_GT | Neo.VM.Script | 0.4807 ns | 0.0166 ns | 0.0155 ns |
    // | Bench_GT | Neo.VM.Script | 0.4912 ns | 0.0192 ns | 0.0180 ns |
    // | Bench_GT | Neo.VM.Script | 0.4837 ns | 0.0123 ns | 0.0115 ns |
    // | Bench_GT | Neo.VM.Script | 0.4846 ns | 0.0118 ns | 0.0110 ns |
    // | Bench_GT | Neo.VM.Script | 0.4871 ns | 0.0130 ns | 0.0121 ns |
    // | Bench_GT | Neo.VM.Script | 0.4840 ns | 0.0173 ns | 0.0162 ns |
    // | Bench_GT | Neo.VM.Script | 0.4891 ns | 0.0163 ns | 0.0152 ns |
    // | Bench_GT | Neo.VM.Script | 0.4834 ns | 0.0152 ns | 0.0142 ns |
    // | Bench_GT | Neo.VM.Script | 0.4793 ns | 0.0087 ns | 0.0081 ns |
    // | Bench_GT | Neo.VM.Script | 0.4782 ns | 0.0100 ns | 0.0078 ns |
    // | Bench_GT | Neo.VM.Script | 0.4867 ns | 0.0108 ns | 0.0084 ns |
    // | Bench_GT | Neo.VM.Script | 0.4868 ns | 0.0129 ns | 0.0121 ns |
    // | Bench_GT | Neo.VM.Script | 0.4927 ns | 0.0179 ns | 0.0159 ns |
    // | Bench_GT | Neo.VM.Script | 0.4820 ns | 0.0101 ns | 0.0084 ns |
    // | Bench_GT | Neo.VM.Script | 0.4854 ns | 0.0101 ns | 0.0094 ns |
    // | Bench_GT | Neo.VM.Script | 0.4898 ns | 0.0118 ns | 0.0105 ns |
    // | Bench_GT | Neo.VM.Script | 0.4881 ns | 0.0143 ns | 0.0134 ns |
    // | Bench_GT | Neo.VM.Script | 0.4885 ns | 0.0164 ns | 0.0154 ns |
    // | Bench_GT | Neo.VM.Script | 0.4993 ns | 0.0299 ns | 0.0294 ns |
    // | Bench_GT | Neo.VM.Script | 0.4870 ns | 0.0182 ns | 0.0170 ns |
    // | Bench_GT | Neo.VM.Script | 0.4905 ns | 0.0138 ns | 0.0130 ns |
    // | Bench_GT | Neo.VM.Script | 0.5039 ns | 0.0160 ns | 0.0150 ns |
    // | Bench_GT | Neo.VM.Script | 0.4892 ns | 0.0157 ns | 0.0147 ns |
    // | Bench_GT | Neo.VM.Script | 0.4942 ns | 0.0213 ns | 0.0199 ns |
    // | Bench_GT | Neo.VM.Script | 0.4940 ns | 0.0160 ns | 0.0150 ns |
    // | Bench_GT | Neo.VM.Script | 0.4907 ns | 0.0199 ns | 0.0186 ns |
    // | Bench_GT | Neo.VM.Script | 0.4988 ns | 0.0112 ns | 0.0105 ns |
    // | Bench_GT | Neo.VM.Script | 0.4799 ns | 0.0108 ns | 0.0101 ns |
    // | Bench_GT | Neo.VM.Script | 0.4777 ns | 0.0129 ns | 0.0114 ns |
}
