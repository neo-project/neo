// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Crypto.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
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
            byte[] message = System.Text.Encoding.Default.GetBytes("HelloWorld");
            byte[] signature = Crypto.Sign(message, key.PrivateKey, key.PublicKey.EncodePoint(false).Skip(1).ToArray());
            Crypto.VerifySignature(message, signature, key.PublicKey).Should().BeTrue();

            byte[] wrongKey = new byte[33];
            wrongKey[0] = 0x02;
            Crypto.VerifySignature(message, signature, wrongKey, Neo.Cryptography.ECC.ECCurve.Secp256r1).Should().BeFalse();

            wrongKey[0] = 0x03;
            for (int i = 1; i < 33; i++) wrongKey[i] = byte.MaxValue;
            Action action = () => Crypto.VerifySignature(message, signature, wrongKey, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            action.Should().Throw<ArgumentException>();

            wrongKey = new byte[36];
            action = () => Crypto.VerifySignature(message, signature, wrongKey, Neo.Cryptography.ECC.ECCurve.Secp256r1);
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestSecp256k1()
        {
            byte[] message = System.Text.Encoding.Default.GetBytes("hello");
            byte[] signature = "5331be791532d157df5b5620620d938bcb622ad02c81cfc184c460efdad18e695480d77440c511e9ad02ea30d773cb54e88f8cbb069644aefa283957085f38b5".FromHexString();
            byte[] pubKey = "03ea01cb94bdaf0cd1c01b159d474f9604f4af35a3e2196f6bdfdb33b2aa4961fa".FromHexString();

            Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1)
                .Should().BeTrue();

            message = System.Text.Encoding.Default.GetBytes("world");
            signature = "b1e6ff4f40536fb7ed706b0f7567903cc227a5241a079fb86f3de51b8321c1e690f37ad0c788848605c1653567935845f0d35a8a1a37174dcbbd235caac8e969".FromHexString();
            pubKey = "03661b86d54eb3a8e7ea2399e0db36ab65753f95fff661da53ae0121278b881ad0".FromHexString();

            Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1)
                .Should().BeTrue();

            message = System.Text.Encoding.Default.GetBytes("中文");
            signature = "b8cba1ff42304d74d083e87706058f59cdd4f755b995926d2cd80a734c5a3c37e4583bfd4339ac762c1c91eee3782660a6baf62cd29e407eccd3da3e9de55a02".FromHexString();
            pubKey = "03661b86d54eb3a8e7ea2399e0db36ab65753f95fff661da53ae0121278b881ad0".FromHexString();

            Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1)
                .Should().BeTrue();
        }
    }
}
