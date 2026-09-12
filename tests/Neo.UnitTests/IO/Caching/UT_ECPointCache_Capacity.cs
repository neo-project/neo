// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ECPointCache_Capacity.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    /// <summary>
    /// Capacity eviction edges not covered by UT_ECPointCache.
    /// </summary>
    [TestClass]
    public class UT_ECPointCache_Capacity
    {
        [TestMethod]
        public void Evicts_When_OverCapacity()
        {
            var cache = new ECPointCache(1)
            {
                ECCurve.Secp256r1.G
            };
            var k1 = ECCurve.Secp256k1.G;
            cache.Add(k1);
            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(cache.TryGet(k1.EncodePoint(true), out _));
            Assert.IsFalse(cache.TryGet(ECCurve.Secp256r1.G.EncodePoint(true), out _));
        }

        [TestMethod]
        public void Count_TracksAddedPoints()
        {
            var cache = new ECPointCache(4);
            Assert.AreEqual(0, cache.Count);
            cache.Add(ECCurve.Secp256r1.G);
            Assert.AreEqual(1, cache.Count);
            cache.Add(ECCurve.Secp256k1.G);
            Assert.AreEqual(2, cache.Count);
        }
    }
}
