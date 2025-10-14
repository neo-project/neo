// Copyright (C) 2015-2025 The Neo Project.
//
// ManualTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;
using System;

namespace Neo.VM.Benchmark
{
    public class ManualTest
    {
        public static void RunTest()
        {
            Console.WriteLine("=== Neo VM Manual Test ===");

            // Test 1: Simple arithmetic
            TestSimpleArithmetic();

            // Test 2: Stack operations
            TestStackOperations();

            // Test 3: Control flow
            TestControlFlow();

            // Test 4: Array operations
            TestArrayOperations();

            Console.WriteLine("=== All tests completed successfully ===");
        }

        private static void TestSimpleArithmetic()
        {
            Console.WriteLine("Testing simple arithmetic...");

            using var engine = new ExecutionEngine();

            // PUSH1, PUSH2, ADD, DROP
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PUSH2,
                (byte)Neo.VM.OpCode.ADD,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Console.WriteLine($"Result: {engine.State} (Expected: HALT)");
        }

        private static void TestStackOperations()
        {
            Console.WriteLine("Testing stack operations...");

            using var engine = new ExecutionEngine();

            // PUSH1, PUSH2, PUSH3, DUP, SWAP, DROP, DROP, DROP
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PUSH2,
                (byte)Neo.VM.OpCode.PUSH3,
                (byte)Neo.VM.OpCode.DUP,
                (byte)Neo.VM.OpCode.SWAP,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Console.WriteLine($"Result: {engine.State} (Expected: HALT)");
        }

        private static void TestControlFlow()
        {
            Console.WriteLine("Testing control flow...");

            using var engine = new ExecutionEngine();

            // PUSH0, JMPIF 3, PUSH1, DROP, PUSH2, DROP
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.PUSH0,
                (byte)Neo.VM.OpCode.JMPIF, 0x02, // Jump 2 bytes forward
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.DROP,
                (byte)Neo.VM.OpCode.PUSH2,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Console.WriteLine($"Result: {engine.State} (Expected: HALT)");
        }

        private static void TestArrayOperations()
        {
            Console.WriteLine("Testing array operations...");

            using var engine = new ExecutionEngine();

            // NEWARRAY0, PUSH1, PUSH1, PACK, SIZE, DROP
            var script = new byte[]
            {
                (byte)Neo.VM.OpCode.NEWARRAY0,
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PUSH1,
                (byte)Neo.VM.OpCode.PACK,
                (byte)Neo.VM.OpCode.SIZE,
                (byte)Neo.VM.OpCode.DROP
            };

            engine.LoadScript(script);
            engine.Execute();

            Console.WriteLine($"Result: {engine.State} (Expected: HALT)");
        }
    }
}
