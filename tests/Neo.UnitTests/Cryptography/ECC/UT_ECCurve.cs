// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ECCurve.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;

namespace Neo.UnitTests.Cryptography.ECC
{
    [TestClass]
    public class UT_ECCurve
    {
        [TestMethod]
        public void Secp256r1_And_Secp256k1_AreDistinct()
        {
            Assert.AreNotEqual(ECCurve.Secp256r1.GetHashCode(), ECCurve.Secp256k1.GetHashCode());
            Assert.AreNotEqual(ECCurve.Secp256r1.N, ECCurve.Secp256k1.N);
            Assert.AreNotEqual(ECCurve.Secp256r1.G, ECCurve.Secp256k1.G);
        }

        [TestMethod]
        public void Infinity_And_Generator_AreSet()
        {
            foreach (var curve in new[] { ECCurve.Secp256r1, ECCurve.Secp256k1 })
            {
                Assert.IsNotNull(curve.Infinity);
                Assert.IsTrue(curve.Infinity.IsInfinity);
                Assert.IsFalse(curve.G.IsInfinity);
                Assert.IsNotNull(curve.BouncyCastleCurve);
                Assert.IsNotNull(curve.BouncyCastleDomainParams);
                Assert.IsTrue(curve.N > 0);
            }
        }

        [TestMethod]
        public void GetHashCode_IsStable()
        {
            Assert.AreEqual(ECCurve.Secp256r1.GetHashCode(), ECCurve.Secp256r1.GetHashCode());
            Assert.AreEqual(ECCurve.Secp256k1.GetHashCode(), ECCurve.Secp256k1.GetHashCode());
        }
    }
}
