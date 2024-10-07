// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.UnPack.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_UnPack : OpCodeBase
{

    [GlobalSetup]
    public void Setup()
    {
        var scriptBuilder = new ScriptBuilder();
        for (var i = 0; i < ItemCount; i++)
            scriptBuilder.EmitPush(i);

        scriptBuilder.EmitPush(ItemCount);
        scriptBuilder.Emit(VM.OpCode.PACK);
        scriptBuilder.Emit(VM.OpCode.UNPACK);

        script = scriptBuilder.ToArray();

        // Setup for multiple PACK and UNPACK operations
        var multiScriptBuilder = new ScriptBuilder();
        for (var i = 0; i < ItemCount; i++)
            multiScriptBuilder.EmitPush(i);

        var count = Benchmark_Opcode.OneGasDatoshi / (Benchmark_Opcode.OpCodePrices[VM.OpCode.PUSHINT32] + Benchmark_Opcode.OpCodePrices[VM.OpCode.PACK] + Benchmark_Opcode.OpCodePrices[VM.OpCode.UNPACK]);

        multiScriptBuilder.EmitPush(ItemCount);
        for (long i = 0; i < count; i++)
        {
            multiScriptBuilder.Emit(VM.OpCode.PACK);
            multiScriptBuilder.Emit(VM.OpCode.UNPACK);
        }

        multiScript = multiScriptBuilder.ToArray();
    }

    [Benchmark]
    public void Bench_UnPack() => Benchmark_Opcode.RunScript(script);

    /// <summary>
    /// Benchmark how long 1 GAS can run OpCode.PACK and OpCode.UNPACK pairs.
    /// </summary>
    [Benchmark]
    public void Bench_OneGasUnpack() => Benchmark_Opcode.LoadScript(multiScript).ExecuteOneGASBenchmark();
}
