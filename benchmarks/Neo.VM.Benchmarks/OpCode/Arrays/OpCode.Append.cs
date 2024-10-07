// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.Append.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_Append : OpCodeBase
{

    [GlobalSetup(Target = nameof(Bench_Append))]
    public void Setup()
    {
        var scriptBuilder = new ScriptBuilder();
        scriptBuilder.Emit(VM.OpCode.NEWARRAY0);
        scriptBuilder.Emit(VM.OpCode.DUP);
        for (var i = 0; i < ItemCount; i++)
            scriptBuilder.EmitPush(i);

        scriptBuilder.EmitPush(ItemCount);
        scriptBuilder.Emit(VM.OpCode.PACKSTRUCT);

        scriptBuilder.Emit(VM.OpCode.APPEND);
        script = scriptBuilder.ToArray();
    }

    [Benchmark]
    public void Bench_Append() => Benchmark_Opcode.RunScript(script);

    [GlobalSetup(Target = nameof(Bench_OneGasAppend))]
    public void SetupOneGas()
    {
        var multiScriptBuilder = new ScriptBuilder();
        multiScriptBuilder.Emit(VM.OpCode.NEWARRAY0);

        var count = Benchmark_Opcode.OneGasDatoshi / (Benchmark_Opcode.OpCodePrices[VM.OpCode.DUP] + Benchmark_Opcode.OpCodePrices[VM.OpCode.NEWSTRUCT0] + Benchmark_Opcode.OpCodePrices[VM.OpCode.APPEND]);

        if (count > ExecutionEngineLimits.Default.MaxStackSize)
            count = ExecutionEngineLimits.Default.MaxStackSize - 10;

        for (long i = 0; i < count; i++)
        {
            multiScriptBuilder.Emit(VM.OpCode.DUP);
            multiScriptBuilder.Emit(VM.OpCode.NEWSTRUCT0);
            multiScriptBuilder.Emit(VM.OpCode.APPEND);
        }

        multiScript = multiScriptBuilder.ToArray();
    }

    /// <summary>
    /// Benchmark how long 1 GAS can run OpCode.NEWSTRUCT0 and OpCode.APPEND pairs.
    /// </summary>
    [Benchmark]
    public void Bench_OneGasAppend() => Benchmark_Opcode.LoadScript(multiScript).ExecuteOneGASBenchmark();
}
