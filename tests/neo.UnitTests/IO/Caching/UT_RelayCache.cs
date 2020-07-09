using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_RelayCache
    {
        RelayCache relayCache;

        [TestInitialize]
        public void SetUp()
        {
            relayCache = new RelayCache(10);
        }

        [TestMethod]
        public void TestGetKeyForItem()
        {
            Transaction tx = new Transaction()
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = Array.Empty<TransactionAttribute>(),
                Signers = Array.Empty<Signer>(),
                Script = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 },
                Witnesses = Array.Empty<Witness>()
            };
            relayCache.Add(tx);
            relayCache.Contains(tx).Should().BeTrue();
            relayCache.TryGet(tx.Hash, out IInventory tmp).Should().BeTrue();
            (tmp is Transaction).Should().BeTrue();
        }
    }
}
