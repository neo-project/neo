// Copyright (C) 2015-2024 The Neo Project.
//
// OpCodeBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode;

public abstract class OpCodeBase
{
    [Params(1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2040)]
    public int ItemCount { get; set; } = 10;
    protected byte[] baseLineScript;
    protected byte[] script;
    protected byte[] multiScript;

    [GlobalSetup]
    public void Setup()
    {
        script = CreateScript(BenchmarkMode.SimpleOpCode);
        multiScript = CreateScript(BenchmarkMode.OneGAS);
        baseLineScript = CreateScript(BenchmarkMode.BaseLine);
    }

    [Benchmark(Baseline = true)]
    public void Bench_BaseLine() => Benchmark_Opcode.RunScript(baseLineScript);

    protected abstract byte[] CreateScript(BenchmarkMode benchmarkMode);
}
