// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.ReverseItems.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_ReverseItems : OpCodeBase
{
    [GlobalSetup]
    public void Setup()
    {
        var scriptBuilder = new ScriptBuilder();
        for (var i = 0; i < ItemCount; i++)
            scriptBuilder.EmitPush(i);

        scriptBuilder.EmitPush(ItemCount);
        scriptBuilder.Emit(VM.OpCode.PACK);
        scriptBuilder.Emit(VM.OpCode.REVERSEITEMS);

        script = scriptBuilder.ToArray();

        var scriptMultiBuilder = new ScriptBuilder();

        for (var i = 0; i < ItemCount; i++)
            scriptMultiBuilder.EmitPush(i);
        scriptMultiBuilder.EmitPush(ItemCount).Emit(VM.OpCode.PACK);

        var count = Benchmark_Opcode.OneGasDatoshi / Benchmark_Opcode.OpCodePrices[VM.OpCode.REVERSEITEMS];
        for (long i = 0; i < count; i++)
        {
            scriptMultiBuilder.Emit(VM.OpCode.REVERSEITEMS);
        }

        multiScript = scriptMultiBuilder.ToArray();
    }

    [Benchmark]
    public void Bench_ReverseItems() => Benchmark_Opcode.RunScript(script);

    /// <summary>
    /// Benchmark how long 1 GAS can run OpCode.REVERSEITEMS.
    /// </summary>
    [Benchmark]
    public void Bench_OneGasReverseItems() => Benchmark_Opcode.LoadScript(multiScript).ExecuteOneGASBenchmark();

    protected override byte[] CreateScript(BenchmarkMode benchmarkMode)
    {
        throw new NotImplementedException();
    }
}
