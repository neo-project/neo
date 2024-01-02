// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ECPointCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_ECPointCache
    {
        ECPointCache relayCache;

        [TestInitialize]
        public void SetUp()
        {
            relayCache = new ECPointCache(10);
        }

        [TestMethod]
        public void TestGetKeyForItem()
        {
            relayCache.Add(ECCurve.Secp256r1.G);
            relayCache.Contains(ECCurve.Secp256r1.G).Should().BeTrue();
            relayCache.TryGet(ECCurve.Secp256r1.G.EncodePoint(true), out ECPoint tmp).Should().BeTrue();
            (tmp is ECPoint).Should().BeTrue();
        }
    }
}
