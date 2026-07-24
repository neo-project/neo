// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ECPointCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_ECPointCache
    {
        [TestMethod]
        public void Add_And_TryGet_ByEncodedPoint()
        {
            var cache = new ECPointCache(4);
            var point = ECCurve.Secp256r1.G;
            cache.Add(point);

            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(cache.TryGet(point.EncodePoint(true), out var found));
            Assert.AreEqual(point, found);
        }

        [TestMethod]
        public void Evicts_When_OverCapacity()
        {
            var cache = new ECPointCache(1);
            cache.Add(ECCurve.Secp256r1.G);
            // Second distinct point forces eviction of first under capacity 1
            // Use infinity-sized unique points: multiply G is hard; encode different by using Secp256k1 G
            var k1 = ECCurve.Secp256k1.G;
            cache.Add(k1);
            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(cache.TryGet(k1.EncodePoint(true), out _));
            Assert.IsFalse(cache.TryGet(ECCurve.Secp256r1.G.EncodePoint(true), out _));
        }
    }
}
