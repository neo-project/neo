// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RelayCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
