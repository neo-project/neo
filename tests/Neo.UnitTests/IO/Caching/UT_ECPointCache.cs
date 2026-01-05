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

using Neo.Cryptography.ECC;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching;

[TestClass]
public class UT_ECPointCache
{
    ECPointCache relayCache = null!;

    [TestInitialize]
    public void SetUp()
    {
        relayCache = new ECPointCache(10);
    }

    [TestMethod]
    public void TestGetKeyForItem()
    {
        relayCache.Add(ECCurve.Secp256r1.G);
        Assert.Contains(ECCurve.Secp256r1.G, relayCache);
        Assert.IsTrue(relayCache.TryGet(ECCurve.Secp256r1.G.EncodePoint(true), out _));
    }
}
