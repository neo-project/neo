// Copyright (C) 2015-2026 The Neo Project.
//
// UT_HeaderCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_HeaderCache
    {
        [TestMethod]
        public void TestHeaderCache()
        {
            var cache = new HeaderCache();
            var header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = null!,
                Index = 1,
                NextConsensus = null!,
                Witness = null!
            };
            cache.Add(header);

            var got = cache[1];
            Assert.IsNotNull(got);
            Assert.AreEqual((uint)1, got.Index);

            var count = cache.Count;
            Assert.AreEqual(1, count);

            var full = cache.Full;
            Assert.IsFalse(full);

            var last = cache.Last;
            Assert.IsNotNull(last);
            Assert.AreEqual((uint)1, last.Index);

            got = cache[2];
            Assert.IsNull(got);

            // enumerate
            var enumerator = cache.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual((uint)1, enumerator.Current.Index);
            Assert.IsFalse(enumerator.MoveNext());

            var removed = cache.TryRemoveFirst(out header);
            Assert.IsTrue(removed);

            count = cache.Count;
            Assert.AreEqual(0, count);

            full = cache.Full;
            Assert.IsFalse(full);

            last = cache.Last;
            Assert.IsNull(last);

            got = cache[1];
            Assert.IsNull(got);
        }

        [TestMethod]
        public void TestHeaderCache_Limit()
        {
            var cache = new HeaderCache();
            uint capacity = 10000;

            // Fill the cache
            for (uint i = 0; i < capacity; i++)
            {
                cache.Add(new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = null!,
                    Index = i,
                    NextConsensus = null!,
                    Witness = null!
                });
            }

            Assert.AreEqual((int)capacity, cache.Count);
            Assert.IsTrue(cache.Full);
            Assert.AreEqual(capacity - 1, cache.Last.Index);

            // Try adding one more
            cache.Add(new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                Index = capacity,
                NextConsensus = null!,
                Witness = null!
            });

            // Verify count did not increase and last item remains the same
            Assert.AreEqual((int)capacity, cache.Count);
            Assert.IsTrue(cache.Full);
            Assert.AreEqual(capacity - 1, cache.Last.Index);

            // Verify the extra item was not added
            Assert.IsNull(cache[capacity]);
        }
    }
}
