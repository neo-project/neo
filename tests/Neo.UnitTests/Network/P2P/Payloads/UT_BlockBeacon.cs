// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BlockBeacon.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.BLS12_381;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_BlockBeacon
    {
        [TestMethod]
        public void BlockBeacon_RoundTrip()
        {
            var value = RandomBeacon.ComputeRoundId(1, 2, 3);
            var payload = new BlockBeacon { Value = value };
            var clone = payload.ToArray().AsSerializable<BlockBeacon>();
            Assert.AreEqual(RandomBeacon.Size, payload.Size);
            Assert.AreEqual(Convert.ToHexString(value), Convert.ToHexString(clone.Value));
        }

        [TestMethod]
        public void BeaconPartial_RoundTrip()
        {
            var sig = G1Affine.Generator.ToCompressed();
            var payload = new BeaconPartial { ValidatorIndex = 4, Signature = sig };
            var clone = payload.ToArray().AsSerializable<BeaconPartial>();
            Assert.AreEqual(1 + BeaconPartial.SignatureSize, payload.Size);
            Assert.AreEqual(4, clone.ValidatorIndex);
            Assert.AreEqual(Convert.ToHexString(sig), Convert.ToHexString(clone.Signature));
            Assert.IsTrue(payload.GetSignature().Equals(G1Affine.Generator));
        }

        [TestMethod]
        public void BlockBeacon_RejectsWrongLength()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _ = new BlockBeacon { Value = new byte[31] });
            Assert.ThrowsExactly<ArgumentException>(() => _ = new BlockBeacon { Value = null });
        }

        [TestMethod]
        public void BeaconPartial_RejectsWrongLength()
        {
            Assert.ThrowsExactly<ArgumentException>(() => _ = new BeaconPartial { Signature = new byte[47] });
            Assert.ThrowsExactly<ArgumentException>(() => _ = new BeaconPartial { Signature = null });
        }

        [TestMethod]
        public void BeaconPartial_FromCompressed_NonG1Stub_Fails()
        {
            var stub = new byte[BeaconPartial.SignatureSize];
            stub[0] = 0xab;
            Assert.Throws<Exception>(() => G1Affine.FromCompressed(stub));
            Assert.ThrowsExactly<FormatException>(() => _ = new BeaconPartial { Signature = stub });
            Assert.ThrowsExactly<FormatException>(() => G1Affine.FromCompressed(new byte[BeaconPartial.SignatureSize]));
        }
    }
}
