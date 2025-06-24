// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StackOptimizations.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.VM.Tests
{
    [TestClass]
    public class UT_StackOptimizations
    {
        [TestMethod]
        public void TestEvaluationStackPopOptimization()
        {
            // Test that Pop() operation is optimized and doesn't use the expensive Remove() path
            var referenceCounter = new ReferenceCounter();
            var stack = new EvaluationStack(referenceCounter);

            // Push some items
            for (int i = 0; i < 100; i++)
            {
                stack.Push(new Integer(i));
            }

            // Pop them all - this should be fast
            for (int i = 99; i >= 0; i--)
            {
                var item = stack.Pop<Integer>();
                Assert.AreEqual(i, item.GetInteger());
            }

            Assert.AreEqual(0, stack.Count);
        }

        [TestMethod]
        public void TestPopOnEmptyStackThrows()
        {
            var referenceCounter = new ReferenceCounter();
            var stack = new EvaluationStack(referenceCounter);

            Assert.ThrowsExactly<InvalidOperationException>(() => stack.Pop());
            Assert.ThrowsExactly<InvalidOperationException>(() => stack.Pop<Integer>());
        }

        [TestMethod]
        public void TestStackItemCacheForSmallIntegers()
        {
            // Test that small integers use cached instances
            var int1a = StackItemCache.GetInteger(5);
            var int1b = StackItemCache.GetInteger(5);

            // Should be the same instance
            Assert.AreSame(int1a, int1b);

            // Test edge cases
            var intMin = StackItemCache.GetInteger(-8);
            var intMax = StackItemCache.GetInteger(16);

            Assert.AreSame(intMin, StackItemCache.GetInteger(-8));
            Assert.AreSame(intMax, StackItemCache.GetInteger(16));
        }

        [TestMethod]
        public void TestStackItemCacheForLargeIntegers()
        {
            // Test that large integers don't use cache
            var int1a = StackItemCache.GetInteger(100);
            var int1b = StackItemCache.GetInteger(100);

            // Should be different instances
            Assert.AreNotSame(int1a, int1b);
            Assert.AreEqual(int1a.GetInteger(), int1b.GetInteger());
        }

        [TestMethod]
        public void TestBooleanCache()
        {
            var true1 = StackItemCache.GetBoolean(true);
            var true2 = StackItemCache.GetBoolean(true);
            var false1 = StackItemCache.GetBoolean(false);
            var false2 = StackItemCache.GetBoolean(false);

            Assert.AreSame(true1, true2);
            Assert.AreSame(false1, false2);
            Assert.AreNotSame(true1, false1);

            // Test with static instances
            Assert.AreSame(true1, StackItemCache.True);
            Assert.AreSame(false1, StackItemCache.False);
        }

        [TestMethod]
        public void TestImplicitIntegerConversionsUseCaching()
        {
            // Test that implicit conversions use caching
            Integer int1 = 5;
            Integer int2 = 5;

            // Small integers should be cached
            Assert.AreSame(int1, int2);

            Integer int3 = 100;
            Integer int4 = 100;

            // Large integers should not be cached
            Assert.AreNotSame(int3, int4);
        }

        [TestMethod]
        public void TestPushPopPerformance()
        {
            // Simulate typical VM usage patterns
            var referenceCounter = new ReferenceCounter();
            var stack = new EvaluationStack(referenceCounter);

            const int operations = 10000;

            // Push phase
            var start = DateTime.UtcNow;
            for (int i = 0; i < operations; i++)
            {
                stack.Push(new Integer(i % 20)); // Mix of cached and non-cached
            }
            var pushTime = DateTime.UtcNow - start;

            // Pop phase
            start = DateTime.UtcNow;
            for (int i = 0; i < operations; i++)
            {
                stack.Pop();
            }
            var popTime = DateTime.UtcNow - start;

            // Just verify it completes without error and is reasonably fast
            Assert.IsTrue(pushTime.TotalMilliseconds < 1000, $"Push took too long: {pushTime.TotalMilliseconds}ms");
            Assert.IsTrue(popTime.TotalMilliseconds < 1000, $"Pop took too long: {popTime.TotalMilliseconds}ms");
            Assert.AreEqual(0, stack.Count);
        }
    }
}
