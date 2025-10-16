// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.VMHotPaths.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM;
using Neo.VM.Benchmark.OpCode;
using VMOpCode = Neo.VM.OpCode;

namespace Neo.VM.Benchmark
{
    /// <summary>
    /// Benchmarks covering hot VM paths that were optimised (instruction decoding and frame returns).
    /// </summary>
    [MemoryDiagnoser]
    public class Benchmarks_VMHotPaths
    {
        private byte[] _scriptBytes = null!;
        private Script _cachedScript = null!;

        [Params(32, 256, 1024)]
        public int PushCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            using ScriptBuilder builder = new(initialCapacity: PushCount * 4 + 8);
            for (int i = 0; i < PushCount; i++)
                builder.Emit(VMOpCode.PUSH1);
            builder.Emit(VMOpCode.RET);
            _scriptBytes = builder.ToArray();
            _cachedScript = new Script(_scriptBytes, strictMode: false);
        }

        /// <summary>
        /// Measures the cost of strict-mode construction and validation using the cached decoder.
        /// </summary>
        [Benchmark]
        public Script ConstructStrictScript()
        {
            return new Script(_scriptBytes, strictMode: true);
        }

        /// <summary>
        /// Measures the steady-state cost of iterating through all instructions via <see cref="Script.GetInstruction"/>.
        /// </summary>
        [Benchmark]
        public int IterateInstructions()
        {
            var script = _cachedScript;
            int ip = 0;
            int count = 0;
            while (ip < script.Length)
            {
                var instruction = script.GetInstruction(ip);
                ip += instruction.Size;
                count++;
            }
            return count;
        }

        /// <summary>
        /// Executes the generated script so the return path copies values into the result stack.
        /// </summary>
        [Benchmark]
        public VMState ExecutePushAndReturn()
        {
            using BenchmarkEngine engine = new();
            engine.LoadScript(_cachedScript, rvcount: PushCount);
            return engine.Execute();
        }
    }
}
