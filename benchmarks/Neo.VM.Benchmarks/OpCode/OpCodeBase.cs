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
        [Params(1, 2, 32, 128, 1024, 2040)]
        public int ItemCount { get; set; } = 2;
        protected byte[] baseLineScript;
        protected byte[] script;
        protected byte[] multiScript;

        private readonly byte[] nopScript;

        private BenchmarkEngine engine;

        protected abstract VM.OpCode Opcode { get; }

        [IterationSetup]
        public void IterationSetup()
        {
            engine = new BenchmarkEngine();
            engine.LoadScript(CreateOneOpCodeScript());
            engine.ExecuteUntil(Opcode);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            engine.Dispose();
        }

        [Benchmark]
        public void Bench_OneOpCode() =>
            engine.ExecuteNext();

        protected abstract byte[] CreateOneOpCodeScript();

        protected abstract byte[] CreateOneGASScript(InstructionBuilder builder);
    }
}
