// Copyright (C) 2015-2026 The Neo Project.
//
// UT_PingPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_PingPayload
    {
        [TestMethod]
        public void Size_IsTwelveBytes()
        {
            var payload = new PingPayload
            {
                LastBlockIndex = 1,
                Timestamp = 2,
                Nonce = 3
            };
            Assert.AreEqual(12, payload.Size);
        }

        [TestMethod]
        public void Create_WithHeightAndNonce_SetsFields()
        {
            var payload = PingPayload.Create(42, 0xAABBCCDD);
            Assert.AreEqual(42u, payload.LastBlockIndex);
            Assert.AreEqual(0xAABBCCDDu, payload.Nonce);
            Assert.IsTrue(payload.Timestamp > 0);
        }

        [TestMethod]
        public void Create_WithHeightOnly_SetsNonce()
        {
            var payload = PingPayload.Create(7);
            Assert.AreEqual(7u, payload.LastBlockIndex);
            Assert.IsTrue(payload.Timestamp > 0);
        }

        [TestMethod]
        public void DeserializeAndSerialize_RoundTrip()
        {
            var original = new PingPayload
            {
                LastBlockIndex = 100,
                Timestamp = 1_700_000_000,
                Nonce = 0x11223344
            };
            var clone = original.ToArray().AsSerializable<PingPayload>();
            Assert.AreEqual(original.LastBlockIndex, clone.LastBlockIndex);
            Assert.AreEqual(original.Timestamp, clone.Timestamp);
            Assert.AreEqual(original.Nonce, clone.Nonce);
        }
    }
}
