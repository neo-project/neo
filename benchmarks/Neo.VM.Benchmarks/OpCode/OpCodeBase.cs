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

namespace Neo.VM.Benchmark.OpCode
{
    public abstract class OpCodeBase
    {
        [Params(4, 8, 16, 32, 64, 128, 256, 512, 1024, 2040)]
        public int ItemCount { get; set; } = 4;

        private readonly byte[] nopScript;

        private BenchmarkEngine engine;
        private BenchmarkEngine oneGasEngine;

        protected abstract VM.OpCode Opcode { get; }

        [IterationSetup]
        public void IterationSetup()
        {
            engine = new BenchmarkEngine();
            engine.LoadScript(CreateOneOpCodeScript());
            engine.ExecuteUntil(Opcode);

            oneGasEngine = new BenchmarkEngine();
            oneGasEngine.LoadScript(CreateOneGASScript());
            oneGasEngine.ExecuteUntil(Opcode);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            engine.Dispose();
            oneGasEngine.Dispose();
        }

        [Benchmark]
        public void Bench_OneOpCode() =>
            engine.ExecuteNext();

        [Benchmark]
        public void Bench_OneGAS()
        {
            oneGasEngine.ExecuteOneGASBenchmark();
            if (oneGasEngine.State != VMState.HALT)
            {
                throw new Exception();
            }
        }

        protected abstract byte[] CreateOneOpCodeScript();

        protected abstract byte[] CreateOneGASScript();
    }
}
