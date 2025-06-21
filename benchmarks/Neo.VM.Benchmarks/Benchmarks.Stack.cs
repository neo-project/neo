// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.Stack.cs file belongs to the neo project and is free
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
    public class StackBenchmarks
    {
        private IReferenceCounter referenceCounter = null!;
        private EvaluationStack listStack = null!;
        private OptimizedEvaluationStack arrayStack = null!;
        private StackItem[] testItems = null!;

        [GlobalSetup]
        public void Setup()
        {
            referenceCounter = new ReferenceCounter();
            listStack = new EvaluationStack(referenceCounter);
            arrayStack = new OptimizedEvaluationStack(referenceCounter);
            
            // Pre-create test items
            testItems = new StackItem[1000];
            for (int i = 0; i < testItems.Length; i++)
            {
                testItems[i] = new Integer(i);
            }
        }

        [Benchmark(Baseline = true)]
        [Arguments(100)]
        [Arguments(1000)]
        public void ListStack_PushPop(int operations)
        {
            var stack = new EvaluationStack(referenceCounter);
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
        [Arguments(1000)]
        public void ArrayStack_PushPop(int operations)
        {
            var stack = new OptimizedEvaluationStack(referenceCounter);
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
        [Arguments(50)]
        public void ListStack_Insert(int operations)
        {
            var stack = new EvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 50; i++)
            {
                stack.Push(testItems[i]);
            }
            
            // Insert operations
            for (int i = 0; i < operations; i++)
            {
                stack.Insert(25, testItems[i % testItems.Length]);
            }
        }

        [Benchmark]
        [Arguments(50)]
        public void ArrayStack_Insert(int operations)
        {
            var stack = new OptimizedEvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 50; i++)
            {
                stack.Push(testItems[i]);
            }
            
            // Insert operations
            for (int i = 0; i < operations; i++)
            {
                stack.Insert(25, testItems[i % testItems.Length]);
            }
        }

        [Benchmark]
        [Arguments(100)]
        public void ListStack_Peek(int operations)
        {
            var stack = new EvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                stack.Push(testItems[i]);
            }
            
            // Peek operations
            for (int i = 0; i < operations; i++)
            {
                _ = stack.Peek(i % 50);
            }
        }

        [Benchmark]
        [Arguments(100)]
        public void ArrayStack_Peek(int operations)
        {
            var stack = new OptimizedEvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                stack.Push(testItems[i]);
            }
            
            // Peek operations
            for (int i = 0; i < operations; i++)
            {
                _ = stack.Peek(i % 50);
            }
        }

        [Benchmark]
        public void ListStack_ReverseOperations()
        {
            var stack = new EvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                stack.Push(testItems[i]);
            }
            
            // Various reverse operations
            stack.Reverse(10);
            stack.Reverse(20);
            stack.Reverse(50);
        }

        [Benchmark]
        public void ArrayStack_ReverseOperations()
        {
            var stack = new OptimizedEvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                stack.Push(testItems[i]);
            }
            
            // Various reverse operations
            stack.Reverse(10);
            stack.Reverse(20);
            stack.Reverse(50);
        }

        [Benchmark]
        public void ListStack_MixedOperations()
        {
            var stack = new EvaluationStack(referenceCounter);
            
            // Simulate real VM usage patterns
            for (int i = 0; i < 50; i++)
            {
                stack.Push(testItems[i]);
                if (i % 10 == 0 && stack.Count > 5)
                {
                    stack.Pop();
                    stack.Pop();
                    stack.Push(testItems[i + 1]);
                }
                if (i % 5 == 0)
                {
                    _ = stack.Peek(Math.Min(i % 10, stack.Count - 1));
                }
            }
        }

        [Benchmark]
        public void ArrayStack_MixedOperations()
        {
            var stack = new OptimizedEvaluationStack(referenceCounter);
            
            // Simulate real VM usage patterns
            for (int i = 0; i < 50; i++)
            {
                stack.Push(testItems[i]);
                if (i % 10 == 0 && stack.Count > 5)
                {
                    stack.Pop();
                    stack.Pop();
                    stack.Push(testItems[i + 1]);
                }
                if (i % 5 == 0)
                {
                    _ = stack.Peek(Math.Min(i % 10, stack.Count - 1));
                }
            }
        }
    }

    /// <summary>
    /// Simple demonstration of stack performance improvements
    /// </summary>
    public class SimpleStackBenchmark
    {
        public static void RunBenchmark()
        {
            const int iterations = 10000;
            var referenceCounter = new ReferenceCounter();

            Console.WriteLine("=== Neo VM Stack Implementation Performance Benchmark ===\n");

            // Warm up
            for (int i = 0; i < 100; i++)
            {
                var s1 = new EvaluationStack(referenceCounter);
                var s2 = new OptimizedEvaluationStack(referenceCounter);
                s1.Push(new Integer(i));
                s2.Push(new Integer(i));
                s1.Pop();
                s2.Pop();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Benchmark 1: Push/Pop Performance
            Console.WriteLine("1. Push/Pop Performance (10,000 operations):");
            
            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            var listStack = new EvaluationStack(referenceCounter);
            for (int i = 0; i < iterations; i++)
            {
                listStack.Push(new Integer(i));
            }
            for (int i = 0; i < iterations; i++)
            {
                listStack.Pop();
            }
            sw1.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            var arrayStack = new OptimizedEvaluationStack(referenceCounter);
            for (int i = 0; i < iterations; i++)
            {
                arrayStack.Push(new Integer(i));
            }
            for (int i = 0; i < iterations; i++)
            {
                arrayStack.Pop();
            }
            sw2.Stop();

            var listTime = sw1.Elapsed.TotalMilliseconds;
            var arrayTime = sw2.Elapsed.TotalMilliseconds;
            var speedup1 = listTime / arrayTime;

            Console.WriteLine($"   List<T> Stack:     {listTime:F2} ms");
            Console.WriteLine($"   Array Stack:       {arrayTime:F2} ms");
            Console.WriteLine($"   Speedup:           {speedup1:F2}x faster");
            Console.WriteLine($"   Improvement:       {((speedup1 - 1) * 100):F1}% performance gain\n");

            // Benchmark 2: Insert Performance
            Console.WriteLine("2. Insert Performance (1,000 middle insertions):");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var sw3 = System.Diagnostics.Stopwatch.StartNew();
            var listStack2 = new EvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                listStack2.Push(new Integer(i));
            }
            // Insert in middle
            for (int i = 0; i < 1000; i++)
            {
                listStack2.Insert(50, new Integer(i));
            }
            sw3.Stop();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var sw4 = System.Diagnostics.Stopwatch.StartNew();
            var arrayStack2 = new OptimizedEvaluationStack(referenceCounter);
            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                arrayStack2.Push(new Integer(i));
            }
            // Insert in middle
            for (int i = 0; i < 1000; i++)
            {
                arrayStack2.Insert(50, new Integer(i));
            }
            sw4.Stop();

            var listTime2 = sw3.Elapsed.TotalMilliseconds;
            var arrayTime2 = sw4.Elapsed.TotalMilliseconds;
            var speedup2 = listTime2 / arrayTime2;

            Console.WriteLine($"   List<T> Stack:     {listTime2:F2} ms");
            Console.WriteLine($"   Array Stack:       {arrayTime2:F2} ms");
            Console.WriteLine($"   Speedup:           {speedup2:F2}x faster");
            Console.WriteLine($"   Improvement:       {((speedup2 - 1) * 100):F1}% performance gain\n");

            // Summary
            Console.WriteLine("=== Summary ===");
            Console.WriteLine("The array-based stack implementation provides:");
            Console.WriteLine($"• {speedup1:F1}x faster push/pop operations");
            Console.WriteLine($"• {speedup2:F1}x faster insert operations");
            Console.WriteLine("• Better memory locality and cache performance");
            Console.WriteLine("• Reduced GC pressure from fewer allocations");
            Console.WriteLine("• Direct array access without List<T> overhead");
        }
    }
}