using System;
using System.Numerics;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Wallets;
using ECDsa = Neo.Cryptography.ECC.ECDsa;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.UnitTests.Cryptography.ECC
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
        public void TestCalculateE()
        {
            ECDsa sa = new ECDsa(key.PublicKey);
            BigInteger n = key.PublicKey.Curve.N;
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            MethodInfo dynMethod = typeof(ECDsa).GetMethod("CalculateE", BindingFlags.NonPublic | BindingFlags.Instance);
            ((BigInteger)dynMethod.Invoke(sa, new object[] { n, message})).Should().Be(BigInteger.Parse("341881320659934023674980"));

            n = new BigInteger(10000000);
            ((BigInteger)dynMethod.Invoke(sa, new object[] { n, message})).Should().Be(BigInteger.Parse("4744556"));
        }

        [TestMethod]
        public void TestECDsaConstructor()
        {
            Action action = () => new ECDsa(key.PublicKey);
            action.ShouldNotThrow();
            action = () => new ECDsa(key.PrivateKey, key.PublicKey.Curve);
            action.ShouldNotThrow();
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
        }

        [TestMethod]
        public void TestSumOfTwoMultiplies()
        {
            ECDsa sa = new ECDsa(key.PublicKey);
            ECPoint P = key.PublicKey.Curve.G;
            BigInteger k = new BigInteger(100);
            ECPoint Q = key.PublicKey;
            BigInteger l = new BigInteger(200);
            MethodInfo dynMethod = typeof(ECDsa).GetMethod("SumOfTwoMultiplies", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            ((ECPoint)dynMethod.Invoke(sa, new object[] { P, k, Q, l })).Should().Be(
                new ECPoint(new ECFieldElement(BigInteger.Parse("46605035452732818385437365557543869880820646072199061780142149056306396117981"), key.PublicKey.Curve),
                new ECFieldElement(BigInteger.Parse("17025013696357888503335513636231796588249781560475785186604887550268987721186"), key.PublicKey.Curve), key.PublicKey.Curve));
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
