// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Ed25519.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using System;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Ed25519
    {
        [TestMethod]
        public void TestGenerateKeyPair()
        {
            byte[] keyPair = Ed25519.GenerateKeyPair();
            Assert.IsNotNull(keyPair);
            Assert.AreEqual(32, keyPair.Length);
        }

        [TestMethod]
        public void TestGetPublicKey()
        {
            byte[] privateKey = Ed25519.GenerateKeyPair();
            byte[] publicKey = Ed25519.GetPublicKey(privateKey);
            Assert.IsNotNull(publicKey);
            Assert.AreEqual(Ed25519.PublicKeySize, publicKey.Length);
        }

        [TestMethod]
        public void TestSignAndVerify()
        {
            byte[] privateKey = Ed25519.GenerateKeyPair();
            byte[] publicKey = Ed25519.GetPublicKey(privateKey);
            byte[] message = Encoding.UTF8.GetBytes("Hello, Neo!");

            byte[] signature = Ed25519.Sign(privateKey, message);
            Assert.IsNotNull(signature);
            Assert.AreEqual(Ed25519.SignatureSize, signature.Length);

            bool isValid = Ed25519.Verify(publicKey, message, signature);
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void TestFailedVerify()
        {
            byte[] privateKey = Ed25519.GenerateKeyPair();
            byte[] publicKey = Ed25519.GetPublicKey(privateKey);
            byte[] message = Encoding.UTF8.GetBytes("Hello, Neo!");

            byte[] signature = Ed25519.Sign(privateKey, message);

            // Tamper with the message
            byte[] tamperedMessage = Encoding.UTF8.GetBytes("Hello, Neo?");

            bool isValid = Ed25519.Verify(publicKey, tamperedMessage, signature);
            Assert.IsFalse(isValid);

            // Tamper with the signature
            byte[] tamperedSignature = new byte[signature.Length];
            Array.Copy(signature, tamperedSignature, signature.Length);
            tamperedSignature[0] ^= 0x01; // Flip one bit

            isValid = Ed25519.Verify(publicKey, message, tamperedSignature);
            Assert.IsFalse(isValid);

            // Use wrong public key
            byte[] wrongPrivateKey = Ed25519.GenerateKeyPair();
            byte[] wrongPublicKey = Ed25519.GetPublicKey(wrongPrivateKey);

            isValid = Ed25519.Verify(wrongPublicKey, message, signature);
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void TestInvalidPrivateKeySize()
        {
            byte[] invalidPrivateKey = new byte[31]; // Invalid size
            Action act = () => Ed25519.GetPublicKey(invalidPrivateKey);
            Assert.ThrowsExactly<ArgumentException>(act, "Invalid private key size*");
        }

        [TestMethod]
        public void TestInvalidSignatureSize()
        {
            byte[] message = Encoding.UTF8.GetBytes("Test message");
            byte[] invalidSignature = new byte[63]; // Invalid size
            byte[] publicKey = new byte[Ed25519.PublicKeySize];
            Action act = () => Ed25519.Verify(publicKey, message, invalidSignature);
            Assert.ThrowsExactly<ArgumentException>(act, "Invalid signature size*");
        }

        [TestMethod]
        public void TestInvalidPublicKeySize()
        {
            byte[] message = Encoding.UTF8.GetBytes("Test message");
            byte[] signature = new byte[Ed25519.SignatureSize];
            byte[] invalidPublicKey = new byte[31]; // Invalid size
            Action act = () => Ed25519.Verify(invalidPublicKey, message, signature);
            Assert.ThrowsExactly<ArgumentException>(act, "Invalid public key size*");
        }

        // Test vectors from RFC 8032 (https://datatracker.ietf.org/doc/html/rfc8032)
        // Section 7.1. Test Vectors for Ed25519

        [TestMethod]
        public void TestVectorCase1()
        {
            byte[] privateKey = "9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60".HexToBytes();
            byte[] publicKey = "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a".HexToBytes();
            byte[] message = Array.Empty<byte>();
            byte[] signature = ("e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e06522490155" +
                                "5fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b").HexToBytes();

            CollectionAssert.AreEqual(publicKey, Ed25519.GetPublicKey(privateKey));
            CollectionAssert.AreEqual(signature, Ed25519.Sign(privateKey, message));
        }

        [TestMethod]
        public void TestVectorCase2()
        {
            byte[] privateKey = "4ccd089b28ff96da9db6c346ec114e0f5b8a319f35aba624da8cf6ed4fb8a6fb".HexToBytes();
            byte[] publicKey = "3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c".HexToBytes();
            byte[] message = Encoding.UTF8.GetBytes("r");
            byte[] signature = ("92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da" +
                                "085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c00").HexToBytes();

            CollectionAssert.AreEqual(publicKey, Ed25519.GetPublicKey(privateKey));
            CollectionAssert.AreEqual(signature, Ed25519.Sign(privateKey, message));
        }

        [TestMethod]
        public void TestVectorCase3()
        {
            byte[] privateKey = "c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7".HexToBytes();
            byte[] publicKey = "fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025".HexToBytes();
            byte[] signature = ("6291d657deec24024827e69c3abe01a30ce548a284743a445e3680d7db5ac3ac" +
                                "18ff9b538d16f290ae67f760984dc6594a7c15e9716ed28dc027beceea1ec40a").HexToBytes();
            byte[] message = "af82".HexToBytes();
            CollectionAssert.AreEqual(publicKey, Ed25519.GetPublicKey(privateKey));
            CollectionAssert.AreEqual(signature, Ed25519.Sign(privateKey, message));
        }
    }
}
