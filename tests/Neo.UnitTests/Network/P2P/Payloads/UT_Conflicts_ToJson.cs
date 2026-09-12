// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Conflicts_ToJson.cs file belongs to the neo project and is free
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
    /// <summary>
    /// ToJson and AllowMultiple edges not covered by UT_Conflicts fee/verify tests.
    /// </summary>
    [TestClass]
    public class UT_Conflicts_ToJson
    {
        [TestMethod]
        public void ToJson_IncludesTypeAndHash()
        {
            var hash = UInt256.Parse("0x00000000000000000000000000000000000000000000000000000000000000aa");
            var attr = new Conflicts { Hash = hash };
            var json = attr.ToJson();
            Assert.AreEqual("Conflicts", json["type"]!.AsString());
            Assert.AreEqual(hash.ToString(), json["hash"]!.AsString());
        }

        [TestMethod]
        public void AllowMultiple_IsTrue_And_SizeIncludesHash()
        {
            var attr = new Conflicts { Hash = UInt256.Zero };
            Assert.IsTrue(attr.AllowMultiple);
            Assert.AreEqual(1 + UInt256.Zero.Size, attr.Size);
        }

        [TestMethod]
        public void Serialize_RoundTrip_PreservesHash()
        {
            var hash = UInt256.Parse("0x0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20");
            var original = new Conflicts { Hash = hash };
            var clone = original.ToArray().AsSerializable<Conflicts>();
            Assert.AreEqual(hash, clone.Hash);
        }
    }
}
