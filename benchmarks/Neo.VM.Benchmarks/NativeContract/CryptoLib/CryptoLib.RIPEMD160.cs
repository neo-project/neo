// Copyright (C) 2015-2024 The Neo Project.
//
// CryptoLib.RIPEMD160.cs file belongs to the neo project and is free
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

namespace Neo.VM.Benchmark.NativeContract.CryptoLib
{
    public class CryptoLib_RIPEMD160
    {
        private BenchmarkEngine _engine;
        private readonly SmartContract.Native.NativeContract _nativeContract = SmartContract.Native.CryptoLib.CryptoLib;

        private const string Method = "ripemd160";

        [ParamsSource(nameof(Params))]
        public object[] _args = Params().First();

        public static IEnumerable<object[]> Params()
        {
            var random = new Random(42); // Use a fixed seed for reproducibility
            return
            [
                [RandomBytes(1, random)],
                [RandomBytes(10, random)],
                [RandomBytes(100, random)],
                [RandomBytes(1000, random)],
                [RandomBytes(10000, random)],
                [RandomBytes(65535, random)],
                [RandomBytes(100000, random)],
                [RandomBytes(ushort.MaxValue * 2, random)]
            ];
        }

        private static byte[] RandomBytes(int length, Random random)
        {
            var buffer = new byte[length];
            random.NextBytes(buffer);
            return buffer;
        }


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

        private byte[] AppCall()
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
            builder.EmitPush(_nativeContract.Hash);
            builder.EmitSysCall(ApplicationEngine.System_Contract_Call);
            return builder.ToArray();
        }
    }
}
