using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Wallets;
using System;
using System.Numerics;
using ECDsa = Neo.Cryptography.ECC.ECDsa;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_ECDsa
    {
        private KeyPair key = null;

        [TestInitialize]
        public void TestSetup()
        {
            key = UT_Crypto.generateCertainKey(32);
        }

        [TestMethod]
        public void TestECDsaConstructor()
        {
            Action action = () => new ECDsa(key.PublicKey);
            action.Should().NotThrow();
            action = () => new ECDsa(key.PrivateKey, key.PublicKey.Curve);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void TestGenerateSignature()
        {
            ECDsa sa = new ECDsa(key.PrivateKey, key.PublicKey.Curve);
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            for (int i = 0; i < 10; i++)
            {
                BigInteger[] result = sa.GenerateSignature(message);
                result.Length.Should().Be(2);
            }
            sa = new ECDsa(key.PublicKey);
            Action action = () => sa.GenerateSignature(message);
            action.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void TestVerifySignature()
        {
            ECDsa sa = new ECDsa(key.PrivateKey, key.PublicKey.Curve);
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            BigInteger[] result = sa.GenerateSignature(message);
            sa.VerifySignature(message, result[0], result[1]).Should().BeTrue();
            sa.VerifySignature(message, new BigInteger(-100), result[1]).Should().BeFalse();
        }
    }
}
