using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using System;
using ECCurve = Neo.Cryptography.ECC.ECCurve;

namespace Neo.UnitTests.Cryptography.ECC
{
    [TestClass]
    public class UT_ECDSA
    {
        [TestMethod]
        public void GenerateSignature()
        {
            var ecdsa = new ECDsa(ECCurve.Secp256k1.Infinity);
            Assert.ThrowsException<InvalidOperationException>(() => ecdsa.GenerateSignature(UInt256.Zero));

            var pk = new byte[32];
            for (int x = 0; x < pk.Length; x++) pk[x] = (byte)x;

            ecdsa = new ECDsa(pk, ECCurve.Secp256k1);

            var zero = UInt256.Zero;
            var one = UInt256.Parse("0100000000000000000000000000000000000000000000000000000000000000");
            var two = UInt256.Parse("0200000000000000000000000000000000000000000000000000000000000000");
            var sig = ecdsa.GenerateSignature(one);

            Assert.IsTrue(ecdsa.VerifySignature(one, sig[0], sig[1]));
            Assert.IsFalse(ecdsa.VerifySignature(two, sig[0], sig[1]));
            Assert.IsFalse(ecdsa.VerifySignature(one, sig[0] + 1, sig[1]));
            Assert.IsFalse(ecdsa.VerifySignature(one, sig[0], sig[1] + 1));
            Assert.IsFalse(ecdsa.VerifySignature(zero, sig[0], sig[1]));
        }
    }
}
