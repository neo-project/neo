// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ECDsaCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using System.Security.Cryptography;
using ECCurve = Neo.Cryptography.ECC.ECCurve;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_ECDsaCache
    {
        [TestMethod]
        public void Add_And_TryGet_ByPoint()
        {
            using var cache = new ECDsaCache(4);
            var point = ECCurve.Secp256r1.G;
            using var ecdsa = ECDsa.Create();
            // Create a disposable ECDsa instance; cache stores wrapper
            var item = new ECDsaCacheItem(point, ecdsa);
            cache.Add(item);

            Assert.AreEqual(1, cache.Count);
            Assert.IsTrue(cache.TryGet(point, out var found));
            Assert.AreSame(item, found);
            Assert.AreEqual(point, found.Key);
        }

        [TestMethod]
        public void DefaultCapacity_IsPositive()
        {
            using var cache = new ECDsaCache();
            Assert.AreEqual(0, cache.Count);
        }
    }
}
