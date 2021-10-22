using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.Network.P2P.Payloads;
using System;

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
            relayCache.TryGet(ECCurve.Secp256r1.G.EncodePoint(true).ToHexString(), out ECPoint tmp).Should().BeTrue();
            (tmp is ECPoint).Should().BeTrue();
        }
    }
}
