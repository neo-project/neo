using System;
using System.Collections;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.Test
{
    [TestClass]
    public class UtEvaluationStack
    {
        private static EvaluationStack CreateOrderedStack(int count)
        {
            var check = new Integer[count];
            var stack = new EvaluationStack(new ReferenceCounter());

            for (int x = 1; x <= count; x++)
            {
                stack.Push(x);
                check[x - 1] = x;
            }

            Assert.AreEqual(count, stack.Count);
            CollectionAssert.AreEqual(check, stack.ToArray());

            return stack;
        }

        public static IEnumerable GetEnumerable(IEnumerator enumerator)
        {
            while (enumerator.MoveNext()) yield return enumerator.Current;
        }

        [TestMethod]
        public void TestClear()
        {
            var stack = CreateOrderedStack(3);
            stack.Clear();
            Assert.AreEqual(0, stack.Count);
        }

        [TestMethod]
        public void TestCopyTo()
        {
            var stack = CreateOrderedStack(3);
            var copy = new EvaluationStack(new ReferenceCounter());

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.CopyTo(copy, -2));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.CopyTo(copy, 4));

            stack.CopyTo(copy, 0);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(0, copy.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, stack.ToArray());

            stack.CopyTo(copy, -1);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(3, copy.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, stack.ToArray());

            // Test IEnumerable

            var enumerable = (IEnumerable)copy;
            var enumerator = enumerable.GetEnumerator();

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, GetEnumerable(enumerator).Cast<Integer>().ToArray());

            copy.CopyTo(stack, 2);

            Assert.AreEqual(5, stack.Count);
            Assert.AreEqual(3, copy.Count);

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3, 2, 3 }, stack.ToArray());
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, copy.ToArray());
        }

        [TestMethod]
        public void TestMoveTo()
        {
            var stack = CreateOrderedStack(3);
            var other = new EvaluationStack(new ReferenceCounter());

            stack.MoveTo(other, 0);

            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual(0, other.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, stack.ToArray());

            stack.MoveTo(other, -1);

            Assert.AreEqual(0, stack.Count);
            Assert.AreEqual(3, other.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, other.ToArray());

            // Test IEnumerable

            var enumerable = (IEnumerable)other;
            var enumerator = enumerable.GetEnumerator();

            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, GetEnumerable(enumerator).Cast<Integer>().ToArray());

            other.MoveTo(stack, 2);

            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual(1, other.Count);

            CollectionAssert.AreEqual(new Integer[] { 2, 3 }, stack.ToArray());
            CollectionAssert.AreEqual(new Integer[] { 1 }, other.ToArray());
        }

        [TestMethod]
        public void TestInsertPeek()
        {
            var stack = new EvaluationStack(new ReferenceCounter());

            stack.Insert(0, 3);
            stack.Insert(1, 1);
            stack.Insert(1, 2);

            Assert.ThrowsException<InvalidOperationException>(() => stack.Insert(4, 2));

            Assert.AreEqual(3, stack.Count);
            CollectionAssert.AreEqual(new Integer[] { 1, 2, 3 }, stack.ToArray());

            Assert.AreEqual(3, stack.Peek(0));
            Assert.AreEqual(2, stack.Peek(1));
            Assert.AreEqual(1, stack.Peek(-1));

            Assert.ThrowsException<InvalidOperationException>(() => stack.Peek(-4));
        }

        [TestMethod]
        public void TestPopPush()
        {
            var stack = CreateOrderedStack(3);

            Assert.AreEqual(3, stack.Pop());
            Assert.AreEqual(2, stack.Pop());
            Assert.AreEqual(1, stack.Pop());

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Pop());

            stack = CreateOrderedStack(3);

            Assert.IsTrue(stack.Pop<Integer>().Equals(3));
            Assert.IsTrue(stack.Pop<Integer>().Equals(2));
            Assert.IsTrue(stack.Pop<Integer>().Equals(1));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Pop<Integer>());
        }

        [TestMethod]
        public void TestRemove()
        {
            var stack = CreateOrderedStack(3);

            Assert.IsTrue(stack.Remove<Integer>(0).Equals(3));
            Assert.IsTrue(stack.Remove<Integer>(0).Equals(2));
            Assert.IsTrue(stack.Remove<Integer>(-1).Equals(1));

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Remove<Integer>(0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Remove<Integer>(-1));
        }

        [TestMethod]
        public void TestReverse()
        {
            var stack = CreateOrderedStack(3);

            stack.Reverse(3);
            Assert.IsTrue(stack.Pop<Integer>().Equals(1));
            Assert.IsTrue(stack.Pop<Integer>().Equals(2));
            Assert.IsTrue(stack.Pop<Integer>().Equals(3));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Pop<Integer>().Equals(0));

            stack = CreateOrderedStack(3);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Reverse(-1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Reverse(4));

            stack.Reverse(1);
            Assert.IsTrue(stack.Pop<Integer>().Equals(3));
            Assert.IsTrue(stack.Pop<Integer>().Equals(2));
            Assert.IsTrue(stack.Pop<Integer>().Equals(1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.Pop<Integer>().Equals(0));
        }
    }
}
