// Copyright (C) 2015-2024 The Neo Project.
//
// UT_IndexedQueue.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
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
            queue.Count.Should().Be(0);

            queue = new IndexedQueue<int>();
            queue.Count.Should().Be(0);
            queue.TrimExcess();
            queue.Count.Should().Be(0);

            queue = new IndexedQueue<int>(Array.Empty<int>());
            queue.Count.Should().Be(0);
            queue.TryPeek(out var a).Should().BeFalse();
            a.Should().Be(0);
            queue.TryDequeue(out a).Should().BeFalse();
            a.Should().Be(0);

            Assert.ThrowsException<InvalidOperationException>(() => queue.Peek());
            Assert.ThrowsException<InvalidOperationException>(() => queue.Dequeue());
            Assert.ThrowsException<IndexOutOfRangeException>(() => _ = queue[-1]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => queue[-1] = 1);
            Assert.ThrowsException<IndexOutOfRangeException>(() => _ = queue[1]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => queue[1] = 1);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new IndexedQueue<int>(-1));
        }

        [TestMethod]
        public void TestQueue()
        {
            var queue = new IndexedQueue<int>(new int[] { 1, 2, 3 });
            queue.Count.Should().Be(3);

            queue.Enqueue(4);
            queue.Count.Should().Be(4);
            queue.Peek().Should().Be(1);
            queue.TryPeek(out var a).Should().BeTrue();
            a.Should().Be(1);

            queue[0].Should().Be(1);
            queue[1].Should().Be(2);
            queue[2].Should().Be(3);
            queue.Dequeue().Should().Be(1);
            queue.Dequeue().Should().Be(2);
            queue.Dequeue().Should().Be(3);
            queue[0] = 5;
            queue.TryDequeue(out a).Should().BeTrue();
            a.Should().Be(5);

            queue.Enqueue(4);
            queue.Clear();
            queue.Count.Should().Be(0);
        }

        [TestMethod]
        public void TestEnumerator()
        {
            int[] arr = new int[3] { 1, 2, 3 };
            var queue = new IndexedQueue<int>(arr);

            arr.SequenceEqual(queue).Should().BeTrue();
        }

        [TestMethod]
        public void TestCopyTo()
        {
            int[] arr = new int[3];
            var queue = new IndexedQueue<int>(new int[] { 1, 2, 3 });

            Assert.ThrowsException<ArgumentNullException>(() => queue.CopyTo(null, 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.CopyTo(arr, -1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.CopyTo(arr, 2));

            queue.CopyTo(arr, 0);

            arr[0].Should().Be(1);
            arr[1].Should().Be(2);
            arr[2].Should().Be(3);

            arr = queue.ToArray();

            arr[0].Should().Be(1);
            arr[1].Should().Be(2);
            arr[2].Should().Be(3);
        }
    }
}
