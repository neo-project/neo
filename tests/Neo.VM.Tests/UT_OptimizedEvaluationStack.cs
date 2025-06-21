// Copyright (C) 2015-2025 The Neo Project.
//
// UT_OptimizedEvaluationStack.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;

namespace Neo.VM.Tests
{
    [TestClass]
    public class UT_OptimizedEvaluationStack
    {
        private ReferenceCounter referenceCounter;
        private OptimizedEvaluationStack stack;

        [TestInitialize]
        public void TestSetup()
        {
            referenceCounter = new ReferenceCounter();
            stack = new OptimizedEvaluationStack(referenceCounter);
        }

        [TestMethod]
        public void TestPushPop()
        {
            var item1 = new Integer(1);
            var item2 = new Integer(2);

            Assert.AreEqual(0, stack.Count);

            stack.Push(item1);
            Assert.AreEqual(1, stack.Count);
            Assert.AreEqual(item1, stack.Peek());

            stack.Push(item2);
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual(item2, stack.Peek());

            var popped = stack.Pop();
            Assert.AreEqual(item2, popped);
            Assert.AreEqual(1, stack.Count);

            popped = stack.Pop();
            Assert.AreEqual(item1, popped);
            Assert.AreEqual(0, stack.Count);
        }

        [TestMethod]
        public void TestPeek()
        {
            var item1 = new Integer(1);
            var item2 = new Integer(2);
            var item3 = new Integer(3);

            stack.Push(item1);
            stack.Push(item2);
            stack.Push(item3);

            Assert.AreEqual(item3, stack.Peek(0)); // Top
            Assert.AreEqual(item2, stack.Peek(1)); // Middle
            Assert.AreEqual(item1, stack.Peek(2)); // Bottom
        }

        [TestMethod]
        public void TestInsert()
        {
            var item1 = new Integer(1);
            var item2 = new Integer(2);
            var item3 = new Integer(3);

            stack.Push(item1);
            stack.Push(item2);

            // Insert at position 1 (between item2 and item1)
            stack.Insert(1, item3);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(item2, stack.Peek(0)); // Top
            Assert.AreEqual(item3, stack.Peek(1)); // Middle (inserted)
            Assert.AreEqual(item1, stack.Peek(2)); // Bottom
        }

        [TestMethod]
        public void TestReverse()
        {
            var item1 = new Integer(1);
            var item2 = new Integer(2);
            var item3 = new Integer(3);

            stack.Push(item1);
            stack.Push(item2);
            stack.Push(item3);

            // Reverse top 2 items
            stack.Reverse(2);

            Assert.AreEqual(item2, stack.Peek(0)); // Was item3
            Assert.AreEqual(item3, stack.Peek(1)); // Was item2
            Assert.AreEqual(item1, stack.Peek(2)); // Unchanged
        }

        [TestMethod]
        public void TestClear()
        {
            stack.Push(new Integer(1));
            stack.Push(new Integer(2));
            stack.Push(new Integer(3));

            Assert.AreEqual(3, stack.Count);

            stack.Clear();

            Assert.AreEqual(0, stack.Count);
        }

        [TestMethod]
        public void TestEnumeration()
        {
            var item1 = new Integer(1);
            var item2 = new Integer(2);
            var item3 = new Integer(3);

            stack.Push(item1);
            stack.Push(item2);
            stack.Push(item3);

            var items = new StackItem[3];
            int i = 0;
            foreach (var item in stack)
            {
                items[i++] = item;
            }

            // Enumeration should go from bottom to top
            Assert.AreEqual(item1, items[0]);
            Assert.AreEqual(item2, items[1]);
            Assert.AreEqual(item3, items[2]);
        }

        [TestMethod]
        public void TestPopGeneric()
        {
            var intItem = new Integer(42);
            var boolItem = StackItem.True;

            stack.Push(intItem);
            stack.Push(boolItem);

            var poppedBool = stack.Pop<StackItem>();
            Assert.AreEqual(boolItem, poppedBool);

            var poppedInt = stack.Pop<Integer>();
            Assert.AreEqual(intItem, poppedInt);
        }

        [TestMethod]
        public void TestCompatibilityWithRegularStack()
        {
            var regularStack = new EvaluationStack(referenceCounter);
            var optimizedStack = new OptimizedEvaluationStack(referenceCounter);

            // Both should behave the same way
            var item1 = new Integer(1);
            var item2 = new Integer(2);

            regularStack.Push(item1);
            optimizedStack.Push(item1);

            regularStack.Push(item2);
            optimizedStack.Push(item2);

            Assert.AreEqual(regularStack.Count, optimizedStack.Count);
            Assert.AreEqual(regularStack.Peek(), optimizedStack.Peek());
            Assert.AreEqual(regularStack.Pop(), optimizedStack.Pop());
            Assert.AreEqual(regularStack.Pop(), optimizedStack.Pop());
        }

        [TestMethod]
        public void TestCapacityGrowth()
        {
            // Test that the stack grows properly when capacity is exceeded
            for (int i = 0; i < 100; i++)
            {
                stack.Push(new Integer(i));
            }

            Assert.AreEqual(100, stack.Count);

            // Verify all items are still accessible
            for (int i = 0; i < 100; i++)
            {
                var expected = new Integer(99 - i);
                var actual = stack.Pop<Integer>();
                Assert.AreEqual(expected.GetInteger(), actual.GetInteger());
            }
        }
    }
}