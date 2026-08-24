// Copyright (C) 2015-2026 The Neo Project.
//
// UT_RandomBeacon.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using System;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_RandomBeacon
    {
        [TestMethod]
        public void RoundId_BindsNetworkHeightAndView()
        {
            var a = RandomBeacon.ComputeRoundId(0x4e454f33, 12_000_000, 0);
            var b = RandomBeacon.ComputeRoundId(0x4e454f33, 12_000_000, 1);
            var c = RandomBeacon.ComputeRoundId(0x4e454f33, 12_000_001, 0);
            var d = RandomBeacon.ComputeRoundId(0x4e454f34, 12_000_000, 0);

            Assert.HasCount(32, a);
            Assert.AreNotEqual(Convert.ToHexString(a), Convert.ToHexString(b));
            Assert.AreNotEqual(Convert.ToHexString(a), Convert.ToHexString(c));
            Assert.AreNotEqual(Convert.ToHexString(a), Convert.ToHexString(d));
            Assert.AreEqual(Convert.ToHexString(a), Convert.ToHexString(RandomBeacon.ComputeRoundId(0x4e454f33, 12_000_000, 0)));
        }

        [TestMethod]
        public void Derive_SeparatesTxCounterAndNetwork()
        {
            var beacon = new byte[32];
            beacon[0] = 7;
            var txA = new byte[32];
            txA[31] = 1;
            var txB = new byte[32];
            txB[31] = 2;

            var r0 = RandomBeacon.Derive(beacon, 1, txA, 0);
            var r1 = RandomBeacon.Derive(beacon, 1, txA, 1);
            var rNet = RandomBeacon.Derive(beacon, 2, txA, 0);
            var rTx = RandomBeacon.Derive(beacon, 1, txB, 0);

            Assert.HasCount(RandomBeacon.DerivedSize, r0);
            Assert.AreEqual(sizeof(uint), r0.Length);
            Assert.AreNotEqual(Convert.ToHexString(r0), Convert.ToHexString(r1));
            Assert.AreNotEqual(Convert.ToHexString(r0), Convert.ToHexString(rNet));
            Assert.AreNotEqual(Convert.ToHexString(r0), Convert.ToHexString(rTx));
        }

        [TestMethod]
        public void Finalize_IncludesRoundId()
        {
            var sig = new byte[48];
            sig[0] = 1;
            var rn = RandomBeacon.ComputeRoundId(1, 2, 0);
            var other = RandomBeacon.ComputeRoundId(1, 2, 1);
            Assert.AreNotEqual(
                Convert.ToHexString(RandomBeacon.Finalize(sig, rn)),
                Convert.ToHexString(RandomBeacon.Finalize(sig, other)));
        }
    }
}
