// Copyright (C) 2015-2026 The Neo Project.
//
// UT_CryptoLib_BLS_Coverage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.BLS12_381;
using Neo.Extensions;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_CryptoLib_BLS_Coverage
    {
        private static readonly byte[] G1 = (
            "97f1d3a73197d7942695638c4fa9ac0fc3688c4f9774b905a14e3a3f171bac586c55e83ff97a1aeffb3af00adb22c6bb"
        ).HexToBytes();

        private static readonly byte[] G2 = (
            "93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e" +
            "024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8"
        ).HexToBytes();

        [TestMethod]
        public void Bls12381Deserialize_InvalidLength_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => CryptoLib.Bls12381Deserialize([1, 2, 3]));
        }

        [TestMethod]
        public void Bls12381Serialize_UnknownType_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                CryptoLib.Bls12381Serialize(new InteropInterface("not-a-bls-point")));
        }

        [TestMethod]
        public void Bls12381Equal_TypeMismatch_Throws()
        {
            var g1 = CryptoLib.Bls12381Deserialize(G1);
            var g2 = CryptoLib.Bls12381Deserialize(G2);
            Assert.ThrowsExactly<ArgumentException>(() => CryptoLib.Bls12381Equal(g1, g2));
        }

        [TestMethod]
        public void Bls12381Equal_SameG1_True()
        {
            var a = CryptoLib.Bls12381Deserialize(G1);
            var b = CryptoLib.Bls12381Deserialize(G1);
            Assert.IsTrue(CryptoLib.Bls12381Equal(a, b));
        }

        [TestMethod]
        public void Bls12381Serialize_Deserialize_G1_RoundTrip()
        {
            var original = CryptoLib.Bls12381Deserialize(G1);
            var bytes = CryptoLib.Bls12381Serialize(original);
            Assert.AreEqual(48, bytes.Length);
            var restored = CryptoLib.Bls12381Deserialize(bytes);
            Assert.IsTrue(CryptoLib.Bls12381Equal(original, restored));
        }

        [TestMethod]
        public void Bls12381Add_G1_ProducesPoint()
        {
            var a = CryptoLib.Bls12381Deserialize(G1);
            var b = CryptoLib.Bls12381Deserialize(G1);
            var sum = CryptoLib.Bls12381Add(a, b);
            Assert.IsNotNull(sum);
            Assert.IsNotNull(CryptoLib.Bls12381Serialize(sum));
        }

        [TestMethod]
        public void Bls12381Add_TypeMismatch_Throws()
        {
            var g1 = CryptoLib.Bls12381Deserialize(G1);
            var g2 = CryptoLib.Bls12381Deserialize(G2);
            Assert.ThrowsExactly<ArgumentException>(() => CryptoLib.Bls12381Add(g1, g2));
        }

        [TestMethod]
        public void Bls12381Serialize_G1Projective_Works()
        {
            var affine = CryptoLib.Bls12381Deserialize(G1).GetInterface<G1Affine>();
            var projective = new InteropInterface(new G1Projective(affine));
            var bytes = CryptoLib.Bls12381Serialize(projective);
            Assert.AreEqual(48, bytes.Length);
        }
    }
}
