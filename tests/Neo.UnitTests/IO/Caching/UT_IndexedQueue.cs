// Copyright (C) 2015-2025 The Neo Project.
//
// UT_IndexedQueue.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System;
using System.Linq;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_IndexedQueue
    {
        [TestMethod]
        public void TestDefault()
        {
            var queue = new IndexedQueue<int>(10);
            Assert.AreEqual(0, queue.Count);

            queue = new IndexedQueue<int>();
            Assert.AreEqual(0, queue.Count);
            queue.TrimExcess();
            Assert.AreEqual(0, queue.Count);

            queue = new IndexedQueue<int>(Array.Empty<int>());
            Assert.AreEqual(0, queue.Count);
            Assert.IsFalse(queue.TryPeek(out var a));
            Assert.AreEqual(0, a);
            Assert.IsFalse(queue.TryDequeue(out a));
            Assert.AreEqual(0, a);

            Assert.ThrowsExactly<InvalidOperationException>(() => _ = queue.Peek());
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = queue.Dequeue());
            Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = _ = queue[-1]);
            Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = queue[-1] = 1);
            Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = _ = queue[1]);
            Assert.ThrowsExactly<IndexOutOfRangeException>(() => _ = queue[1] = 1);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = new IndexedQueue<int>(-1));
        }

        [TestMethod]
        public void TestQueue()
        {
            var queue = new IndexedQueue<int>(new int[] { 1, 2, 3 });
            Assert.AreEqual(3, queue.Count);

            queue.Enqueue(4);
            Assert.AreEqual(4, queue.Count);
            Assert.AreEqual(1, queue.Peek());
            Assert.IsTrue(queue.TryPeek(out var a));
            Assert.AreEqual(1, a);

            Assert.AreEqual(1, queue[0]);
            Assert.AreEqual(2, queue[1]);
            Assert.AreEqual(3, queue[2]);
            Assert.AreEqual(1, queue.Dequeue());
            Assert.AreEqual(2, queue.Dequeue());
            Assert.AreEqual(3, queue.Dequeue());
            queue[0] = 5;
            Assert.IsTrue(queue.TryDequeue(out a));
            Assert.AreEqual(5, a);

            queue.Enqueue(4);
            queue.Clear();
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void TestEnumerator()
        {
            int[] arr = new int[3] { 1, 2, 3 };
            var queue = new IndexedQueue<int>(arr);

            Assert.IsTrue(arr.SequenceEqual(queue));
        }

        [TestMethod]
        public void TestCopyTo()
        {
            int[] arr = new int[3];
            var queue = new IndexedQueue<int>(new int[] { 1, 2, 3 });

            Assert.ThrowsExactly<ArgumentNullException>(() => queue.CopyTo(null, 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => queue.CopyTo(arr, -1));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => queue.CopyTo(arr, 2));

            queue.CopyTo(arr, 0);

            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual(2, arr[1]);
            Assert.AreEqual(3, arr[2]);

            arr = queue.ToArray();

            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual(2, arr[1]);
            Assert.AreEqual(3, arr[2]);
        }

        [TestMethod]
        public void TestQueueClass()
        {
            var q = new IndexedQueue<int?>([1, 2]);
            var item = q.Dequeue();
            Assert.AreEqual(1, item);

            item = q.Dequeue();
            Assert.AreEqual(2, item);

            Assert.ThrowsExactly<InvalidOperationException>(() => q.Dequeue());
        }
    }
}
