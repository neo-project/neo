// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Crypto.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Wallets;
using System;
using System.Security.Cryptography;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Crypto
    {
        private KeyPair key = null;
        private readonly byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
        private readonly byte[] wrongKey = new byte[33];
        private readonly byte[] wrongKey2 = new byte[36];

        public static KeyPair GenerateKey(int privateKeyLength)
        {
            byte[] privateKey = new byte[privateKeyLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            return new KeyPair(privateKey);
        }

        public static KeyPair GenerateCertainKey(int privateKeyLength)
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
            key = GenerateKey(32);
        }

        [TestMethod]
        public void TestVerifySignature()
        {
            byte[] signature = Crypto.Sign(message, key.PrivateKey);
            Assert.IsTrue(Crypto.VerifySignature(message, signature, key.PublicKey));

            wrongKey[0] = 0x02;
            Assert.IsFalse(Crypto.VerifySignature(message, signature, wrongKey, Neo.Cryptography.ECC.ECCurve.Secp256r1));

            wrongKey[0] = 0x03;
            for (int i = 1; i < 33; i++) wrongKey[i] = byte.MaxValue;
            Assert.ThrowsException<ArgumentException>(() => Crypto.VerifySignature(message, signature, wrongKey, Neo.Cryptography.ECC.ECCurve.Secp256r1));

            Assert.ThrowsException<FormatException>(() => Crypto.VerifySignature(message, signature, wrongKey2, Neo.Cryptography.ECC.ECCurve.Secp256r1));
        }

        [TestMethod]
        public void TestSecp256k1()
        {
            byte[] privkey = "7177f0d04c79fa0b8c91fe90c1cf1d44772d1fba6e5eb9b281a22cd3aafb51fe".HexToBytes();
            byte[] message = "2d46a712699bae19a634563d74d04cc2da497b841456da270dccb75ac2f7c4e7".HexToBytes();
            var signature = Crypto.Sign(message, privkey, Neo.Cryptography.ECC.ECCurve.Secp256k1);

            byte[] pubKey = "04fd0a8c1ce5ae5570fdd46e7599c16b175bf0ebdfe9c178f1ab848fb16dac74a5d301b0534c7bcf1b3760881f0c420d17084907edd771e1c9c8e941bbf6ff9108".HexToBytes();
            Assert.IsTrue(Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1));

            message = System.Text.Encoding.Default.GetBytes("world");
            signature = Crypto.Sign(message, privkey, Neo.Cryptography.ECC.ECCurve.Secp256k1);

            Assert.IsTrue(Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1));

            message = System.Text.Encoding.Default.GetBytes("中文");
            signature = "b8cba1ff42304d74d083e87706058f59cdd4f755b995926d2cd80a734c5a3c37e4583bfd4339ac762c1c91eee3782660a6baf62cd29e407eccd3da3e9de55a02".HexToBytes();
            pubKey = "03661b86d54eb3a8e7ea2399e0db36ab65753f95fff661da53ae0121278b881ad0".HexToBytes();

            Assert.IsTrue(Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1));
        }
    }
}
