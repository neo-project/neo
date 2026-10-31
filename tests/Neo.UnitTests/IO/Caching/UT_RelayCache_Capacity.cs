// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RelayCache_Capacity.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System.Linq;

namespace Neo.UnitTests.IO.Caching
{
    /// <summary>
    /// Capacity / eviction coverage not covered by UT_RelayCache.TestGetKeyForItem.
    /// </summary>
    [TestClass]
    public class UT_RelayCache_Capacity
    {
        [TestMethod]
        public void Evicts_Oldest_WhenCapacityExceeded()
        {
            var cache = new RelayCache(2);
            var tx1 = TestUtils.GetTransaction(UInt160.Zero);
            var tx2 = TestUtils.GetTransaction(UInt160.Zero);
            tx2.Nonce = tx1.Nonce + 1;
            var tx3 = TestUtils.GetTransaction(UInt160.Zero);
            tx3.Nonce = tx1.Nonce + 2;

            cache.Add(tx1);
            cache.Add(tx2);
            Assert.HasCount(2, cache.ToArray());
            Assert.IsTrue(cache.Contains(tx1.Hash));
            Assert.IsTrue(cache.Contains(tx2.Hash));

            cache.Add(tx3);
            Assert.HasCount(2, cache.ToArray());
            Assert.IsTrue(cache.Contains(tx3.Hash));
            // FIFO: first inserted should be gone
            Assert.IsFalse(cache.Contains(tx1.Hash));
            Assert.IsTrue(cache.Contains(tx2.Hash));
        }
    }
}
