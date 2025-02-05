// Copyright (C) 2015-2025 The Neo Project.
//
// UT_KeyPair.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_KeyPair
    {
        [TestMethod]
        public void TestConstructor()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            for (int i = 0; i < privateKey.Length; i++)
                privateKey[i] = (byte)random.Next(256);
            KeyPair keyPair = new KeyPair(privateKey);
            ECPoint publicKey = ECCurve.Secp256r1.G * privateKey;
            CollectionAssert.AreEqual(privateKey, keyPair.PrivateKey);
            Assert.AreEqual(publicKey, keyPair.PublicKey);

            byte[] privateKey96 = new byte[96];
            for (int i = 0; i < privateKey96.Length; i++)
                privateKey96[i] = (byte)random.Next(256);
            keyPair = new KeyPair(privateKey96);
            publicKey = ECPoint.DecodePoint(new byte[] { 0x04 }.Concat(privateKey96.Skip(privateKey96.Length - 96).Take(64)).ToArray(), ECCurve.Secp256r1);
            CollectionAssert.AreEqual(privateKey96.Skip(64).Take(32).ToArray(), keyPair.PrivateKey);
            Assert.AreEqual(publicKey, keyPair.PublicKey);

            byte[] privateKey31 = new byte[31];
            for (int i = 0; i < privateKey31.Length; i++)
                privateKey31[i] = (byte)random.Next(256);
            Action action = () => new KeyPair(privateKey31);
            Assert.ThrowsException<ArgumentException>(action);
        }

        [TestMethod]
        public void TestEquals()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            for (int i = 0; i < privateKey.Length; i++)
                privateKey[i] = (byte)random.Next(256);
            KeyPair keyPair = new KeyPair(privateKey);
            KeyPair keyPair2 = keyPair;
            Assert.IsTrue(keyPair.Equals(keyPair2));

            KeyPair keyPair3 = null;
            Assert.IsFalse(keyPair.Equals(keyPair3));

            byte[] privateKey1 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            byte[] privateKey2 = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02};
            KeyPair keyPair4 = new KeyPair(privateKey1);
            KeyPair keyPair5 = new KeyPair(privateKey2);
            Assert.IsFalse(keyPair4.Equals(keyPair5));
        }

        [TestMethod]
        public void TestEqualsWithObj()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            for (int i = 0; i < privateKey.Length; i++)
                privateKey[i] = (byte)random.Next(256);
            KeyPair keyPair = new KeyPair(privateKey);
            Object keyPair2 = keyPair;
            Assert.IsTrue(keyPair.Equals(keyPair2));
        }

        [TestMethod]
        public void TestExport()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            byte[] data = { 0x80, 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            Assert.AreEqual(Base58.Base58CheckEncode(data), keyPair.Export());
        }

        [TestMethod]
        public void TestGetPublicKeyHash()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            Assert.AreEqual("0x4ab3d6ac3a0609e87af84599c93d57c2d0890406", keyPair.PublicKeyHash.ToString());
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair1 = new KeyPair(privateKey);
            KeyPair keyPair2 = new KeyPair(privateKey);
            Assert.AreEqual(keyPair2.GetHashCode(), keyPair1.GetHashCode());
        }

        [TestMethod]
        public void TestToString()
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            KeyPair keyPair = new KeyPair(privateKey);
            Assert.AreEqual("026ff03b949241ce1dadd43519e6960e0a85b41a69a05c328103aa2bce1594ca16", keyPair.ToString());
        }
    }
}
