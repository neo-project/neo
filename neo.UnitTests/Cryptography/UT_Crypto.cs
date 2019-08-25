using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Wallets;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Crypto
    {
        private KeyPair key = null;

        public static KeyPair generateKey(int privateKeyLength)
        {
            byte[] privateKey = new byte[privateKeyLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            return new KeyPair(privateKey);
        }

        public static KeyPair generateCertainKey(int privateKeyLength)
        {
            byte[] privateKey = new byte[privateKeyLength];
            for (int i = 0; i < privateKeyLength; i++)
            {
                privateKey[i] = (byte)((byte)i % byte.MaxValue);
            }
            return new KeyPair(privateKey);
        }

        [TestInitialize]
        public void TestSetup()
        {
            key = generateKey(32);
        }

        [TestMethod]
        public void TestVerifySignature()
        {
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            byte[] signature = Crypto.Default.Sign(message, key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray());
            Crypto.Default.VerifySignature(message, signature, key.PublicKey.EncodePoint(false)).Should().BeTrue();
            Crypto.Default.VerifySignature(message, signature, key.PublicKey.EncodePoint(false).Skip(1).ToArray()).Should().BeTrue();
            Crypto.Default.VerifySignature(message, signature, key.PublicKey.EncodePoint(false).Skip(1).ToArray()).Should().BeTrue();

            byte[] wrongKey = new byte[33];
            wrongKey[0] = 0x02;
            Crypto.Default.VerifySignature(message, signature, wrongKey).Should().BeFalse();

            wrongKey[0] = 0x03;
            for (int i = 1; i < 33; i++) wrongKey[i] = byte.MaxValue;
            Crypto.Default.VerifySignature(message, signature, wrongKey).Should().BeFalse();

            wrongKey = new byte[36];
            Action action = () => Crypto.Default.VerifySignature(message, signature, wrongKey).Should().BeFalse();
            action.ShouldThrow<ArgumentException>();
        }
    }
}