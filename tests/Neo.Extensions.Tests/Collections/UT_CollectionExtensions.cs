// Copyright (C) 2015-2024 The Neo Project.
//
// UT_CollectionExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted. 

using Neo.Extensions;
using System;
using System.Collections.Generic;

namespace Neo.Extensions.Tests.Collections
{
    [TestClass]
    public class UT_CollectionExtensions
    {
        [TestMethod]
        public void TestChunk()
        {
            var source = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var chunks = source.Chunk(3).GetEnumerator();

            Assert.IsTrue(chunks.MoveNext());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, chunks.Current);

            Assert.IsTrue(chunks.MoveNext());
            CollectionAssert.AreEqual(new[] { 4, 5, 6 }, chunks.Current);

            Assert.IsTrue(chunks.MoveNext());
            CollectionAssert.AreEqual(new[] { 7, 8, 9 }, chunks.Current);

            Assert.IsTrue(chunks.MoveNext());
            CollectionAssert.AreEqual(new[] { 10 }, chunks.Current);

            // Empty source
            var empty = new List<int>();
            var emptyChunks = empty.Chunk(3).GetEnumerator();
            Assert.IsFalse(emptyChunks.MoveNext());

            // Zero chunk size
            var zero = new List<int> { 1, 2, 3 };
            var zeroChunks = zero.Chunk(0).GetEnumerator();
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => zeroChunks.MoveNext());

            // Null source
            IReadOnlyCollection<int>? nullSource = null;
            var nullChunks = nullSource.Chunk(3).GetEnumerator();
            Assert.IsFalse(emptyChunks.MoveNext());

            // HashSet
            var hashSet = new HashSet<int> { 1, 2, 3, 4 };
            chunks = hashSet.Chunk(3).GetEnumerator();

            Assert.IsTrue(chunks.MoveNext());
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, chunks.Current);

            Assert.IsTrue(chunks.MoveNext());
            CollectionAssert.AreEqual(new[] { 4 }, chunks.Current);
        }

        [TestMethod]
        public void TestRemoveWhere()
        {
            var dict = new Dictionary<int, string>
            {
                [1] = "a",
                [2] = "b",
                [3] = "c"
            };

            dict.RemoveWhere(p => p.Value == "b");

            Assert.AreEqual(2, dict.Count);
            Assert.IsFalse(dict.ContainsKey(2));
            Assert.AreEqual("a", dict[1]);
            Assert.AreEqual("c", dict[3]);
        }
    }
}
