// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark.Native.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.SmartContract;
using Neo.VM.Benchmark.OpCode;

namespace Neo.VM.Benchmark.NativeContract;

public abstract class Benchmark_Native
{
    private BenchmarkEngine _engine;

    protected abstract SmartContract.Native.NativeContract Native { get; }
    protected abstract string Method { get; }

    [ParamsSource(nameof(Params))]
    private readonly object[] _args;
    protected abstract object[][] Params { get; }

    [IterationSetup]
    public void Setup()
    {

        _engine = new BenchmarkEngine();
        _engine.LoadScript(AppCall());
        _engine.ExecuteUntil(VM.OpCode.SYSCALL);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _engine.Dispose();
    }

    [Benchmark]
    public void Bench()
    {
        _engine.ExecuteNext();
    }

    protected byte[] AppCall()
    {
        var builder = new ScriptBuilder();

        foreach (var o in _args)
        {
            builder.EmitPush(o);
        }

        builder.EmitPush(_args.Length);
        builder.Emit(VM.OpCode.PACK);

        builder.EmitPush((byte)CallFlags.None);
        builder.EmitPush(Method);
        builder.EmitPush(Native.Hash);
        builder.EmitSysCall(ApplicationEngine.System_Contract_Call);
        return builder.ToArray();
    }
}
