// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.EvaluationStack.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Types;
using System;

namespace Neo.VM.Benchmark
{
    [MemoryDiagnoser]
    public class EvaluationStackBenchmarks
    {
        private IReferenceCounter referenceCounter = null!;
        private EvaluationStack stack = null!;
        private StackItem[] testItems = null!;

        [GlobalSetup]
        public void Setup()
        {
            referenceCounter = new ReferenceCounter();
            stack = new EvaluationStack(referenceCounter);
            
            // Pre-create test items
            testItems = new StackItem[1000];
            for (int i = 0; i < testItems.Length; i++)
            {
                testItems[i] = new Integer(i);
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Clear stack between iterations
            while (stack.Count > 0)
                stack.Pop();
        }

        [Benchmark]
        [Arguments(100)]
        [Arguments(1000)]
        public void PushPop(int operations)
        {
            for (int i = 0; i < operations; i++)
            {
                stack.Push(testItems[i % testItems.Length]);
            }
            for (int i = 0; i < operations; i++)
            {
                stack.Pop();
            }
        }

        [Benchmark]
        [Arguments(100)]
        public void MixedStackOperations(int operations)
        {
            // Simulate realistic VM stack usage patterns
            for (int i = 0; i < operations; i++)
            {
                switch (i % 5)
                {
                    case 0: // PUSH
                        stack.Push(testItems[i % testItems.Length]);
                        break;
                    case 1: // DUP
                        if (stack.Count > 0)
                        {
                            var item = stack.Peek(0);
                            stack.Push(item);
                        }
                        break;
                    case 2: // SWAP
                        if (stack.Count >= 2)
                        {
                            var a = stack.Pop();
                            var b = stack.Pop();
                            stack.Push(a);
                            stack.Push(b);
                        }
                        break;
                    case 3: // PEEK
                        if (stack.Count > 0)
                            _ = stack.Peek(0);
                        break;
                    case 4: // POP
                        if (stack.Count > 0)
                            stack.Pop();
                        break;
                }
            }
        }

        [Benchmark]
        [Arguments(50)]
        public void InsertOperations(int operations)
        {
            // Pre-populate
            for (int i = 0; i < 50; i++)
            {
                stack.Push(testItems[i]);
            }
            
            // Insert in middle
            for (int i = 0; i < operations; i++)
            {
                stack.Insert(25, testItems[i % testItems.Length]);
            }
        }
    }

    /// <summary>
    /// Benchmarks for StackItemCache
    /// </summary>
    [MemoryDiagnoser]
    public class StackItemCacheBenchmarks
    {
        [Benchmark(Baseline = true)]
        public Integer CreateNewInteger()
        {
            return new Integer(5);
        }

        [Benchmark]
        public Integer GetCachedInteger()
        {
            return StackItemCache.GetInteger(5);
        }

        [Benchmark]
        [Arguments(-8, 0, 5, 16)] // Test edge cases of cache
        public Integer GetIntegerWithValue(int value)
        {
            return StackItemCache.GetInteger(value);
        }

        [Benchmark(Baseline = true)]
        public Boolean CreateNewBoolean()
        {
            return new Boolean(true);
        }

        [Benchmark]
        public Boolean GetCachedBoolean()
        {
            return StackItemCache.GetBoolean(true);
        }
    }
}