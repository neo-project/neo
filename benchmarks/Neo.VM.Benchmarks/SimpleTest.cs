// Copyright (C) 2015-2025 The Neo Project.
//
// SimpleTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM;
using System.Diagnostics;

namespace Neo.VM.Benchmark
{
    public class SimpleTest
    {
        [Benchmark]
        public void SimplePushAdd()
        {
            using var engine = new ExecutionEngine();

            // Simple script: PUSH1, PUSH2, ADD, DROP
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PUSH2,
                (byte)Neo.VM.OpCode.ADD,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }

        [Benchmark]
        public void SimpleLoop()
        {
            using var engine = new ExecutionEngine();

            // Simple loop script that runs a few times
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH0,
                (byte)Neo.VM.OpCode.JMP, 0x00, 0x06, // Jump to instruction after this (3 byte jump)
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.INC,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Debug.Assert(engine.State == VMState.HALT);
        }
    }
}
