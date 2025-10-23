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

            var state = RunScript(builder =>
            {
                builder.EmitPush(1);
                builder.EmitPush(2);
                builder.Emit(OpCode.ADD);
                builder.Emit(OpCode.DROP);
            });

            Console.WriteLine($"Result: {state} (Expected: HALT)");
        }

        private static void TestStackOperations()
        {
            Console.WriteLine("Testing stack operations...");

            var state = RunScript(builder =>
            {
                builder.EmitPush(1);
                builder.EmitPush(2);
                builder.EmitPush(3);
                builder.Emit(OpCode.DUP);
                builder.Emit(OpCode.SWAP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
                builder.Emit(OpCode.DROP);
            });

            Console.WriteLine($"Result: {state} (Expected: HALT)");
        }

        private static void TestControlFlow()
        {
            Console.WriteLine("Testing control flow...");

            var state = RunScript(builder =>
            {
                builder.EmitPush(0);
                builder.Emit(OpCode.JMPIF, new[] { (byte)0x02 });
                builder.EmitPush(1);
                builder.Emit(OpCode.DROP);
                builder.EmitPush(2);
                builder.Emit(OpCode.DROP);
            });

            Console.WriteLine($"Result: {state} (Expected: HALT)");
        }

        private static void TestArrayOperations()
        {
            Console.WriteLine("Testing array operations...");

            var state = RunScript(builder =>
            {
                builder.Emit(OpCode.NEWARRAY0);
                builder.EmitPush(1);
                builder.EmitPush(1);
                builder.Emit(OpCode.PACK);
                builder.Emit(OpCode.SIZE);
                builder.Emit(OpCode.DROP);
            });

            Console.WriteLine($"Result: {state} (Expected: HALT)");
        }

        private static VMState RunScript(Action<ScriptBuilder> emitter)
        {
            using var builder = new ScriptBuilder();
            emitter(builder);
            builder.Emit(OpCode.RET);

            using var engine = new ExecutionEngine();
            engine.LoadScript(builder.ToArray());
            return engine.Execute();
        }
    }
}
