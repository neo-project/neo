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
            Assert.ThrowsException<InvalidOperationException>(() => ecdsa.GenerateSignature(new byte[0]));

            var pk = new byte[32];
            for (int x = 0; x < pk.Length; x++) pk[x] = (byte)x;

            ecdsa = new ECDsa(pk, ECCurve.Secp256k1);
            var sig = ecdsa.GenerateSignature(new byte[] { 1 });

            Assert.IsTrue(ecdsa.VerifySignature(new byte[] { 1 }, sig[0], sig[1]));
            Assert.IsFalse(ecdsa.VerifySignature(new byte[] { 2 }, sig[0], sig[1]));
            Assert.IsFalse(ecdsa.VerifySignature(new byte[] { 1 }, sig[0] + 1, sig[1]));
            Assert.IsFalse(ecdsa.VerifySignature(new byte[] { 1 }, sig[0], sig[1] + 1));
        }
    }
}
