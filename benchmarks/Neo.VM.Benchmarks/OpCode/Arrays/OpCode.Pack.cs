// Copyright (C) 2015-2024 The Neo Project.
//
// OpCode.Pack.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;

namespace Neo.VM.Benchmark.OpCode;

public class OpCode_Pack : OpCodeBase
{
    [GlobalSetup]
    public void Setup()
    {
        var scriptBuilder = new ScriptBuilder();
        for (var i = 0; i < ItemCount; i++)
            scriptBuilder.EmitPush(i);

        scriptBuilder.EmitPush(ItemCount);
        scriptBuilder.Emit(VM.OpCode.PACK);

        script = scriptBuilder.ToArray();
    }

    [Benchmark]
    public void Bench_Pack() => Benchmark_Opcode.RunScript(script);
}
