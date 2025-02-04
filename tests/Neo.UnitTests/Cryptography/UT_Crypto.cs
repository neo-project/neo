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
using Neo.Extensions;
using Neo.Wallets;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Crypto
    {
        private KeyPair _key = null;
        private readonly byte[] _message = Encoding.Default.GetBytes("HelloWorld");

        public static KeyPair GenerateKey(int privateKeyLength)
        {
            var privateKey = new byte[privateKeyLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            return new KeyPair(privateKey);
        }

        public static KeyPair GenerateCertainKey(int privateKeyLength)
        {
            var privateKey = new byte[privateKeyLength];
            for (var i = 0; i < privateKeyLength; i++)
            {
                privateKey[i] = (byte)((byte)i % byte.MaxValue);
            }
            return new KeyPair(privateKey);
        }

        [TestInitialize]
        public void TestSetup()
        {
            _key = GenerateKey(32);
        }

        [TestMethod]
        public void TestVerifySignature()
        {
            var message = Encoding.Default.GetBytes("HelloWorld");
            var signature = Crypto.Sign(message, _key.PrivateKey);
            Assert.IsTrue(Crypto.VerifySignature(message, signature, _key.PublicKey));

            var wrongKey2 = new byte[36];
            var wrongKey = new byte[33];
            wrongKey[0] = 0x02;
            Assert.IsFalse(Crypto.VerifySignature(message, signature, wrongKey, Neo.Cryptography.ECC.ECCurve.Secp256r1));

            wrongKey[0] = 0x03;
            for (var i = 1; i < 33; i++) wrongKey[i] = byte.MaxValue;
            Assert.ThrowsException<ArgumentException>(() => Crypto.VerifySignature(message, signature, wrongKey, Neo.Cryptography.ECC.ECCurve.Secp256r1));

            Assert.ThrowsException<FormatException>(() => Crypto.VerifySignature(message, signature, wrongKey2, Neo.Cryptography.ECC.ECCurve.Secp256r1));
        }

        [TestMethod]
        public void TestSecp256k1()
        {
            var privkey = "7177f0d04c79fa0b8c91fe90c1cf1d44772d1fba6e5eb9b281a22cd3aafb51fe".HexToBytes();
            var message = "2d46a712699bae19a634563d74d04cc2da497b841456da270dccb75ac2f7c4e7".HexToBytes();
            var signature = Crypto.Sign(message, privkey, Neo.Cryptography.ECC.ECCurve.Secp256k1);

            var pubKey = "04fd0a8c1ce5ae5570fdd46e7599c16b175bf0ebdfe9c178f1ab848fb16dac74a5d301b0534c7bcf1b3760881f0c420d17084907edd771e1c9c8e941bbf6ff9108".HexToBytes();
            Assert.IsTrue(Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1));

            message = Encoding.Default.GetBytes("world");
            signature = Crypto.Sign(message, privkey, Neo.Cryptography.ECC.ECCurve.Secp256k1);

            Assert.IsTrue(Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1));

            message = Encoding.Default.GetBytes("中文");
            signature = "b8cba1ff42304d74d083e87706058f59cdd4f755b995926d2cd80a734c5a3c37e4583bfd4339ac762c1c91eee3782660a6baf62cd29e407eccd3da3e9de55a02".HexToBytes();
            pubKey = "03661b86d54eb3a8e7ea2399e0db36ab65753f95fff661da53ae0121278b881ad0".HexToBytes();

            Assert.IsTrue(Crypto.VerifySignature(message, signature, pubKey, Neo.Cryptography.ECC.ECCurve.Secp256k1));

            var messageHash = message.Sha256();
            // append v to signature
            signature = [.. signature, .. new byte[] { 27 }];
            var recover = Crypto.ECRecover(signature, messageHash);

            CollectionAssert.AreEqual(pubKey, recover.ToArray());
        }

        [TestMethod]
        public void TestECRecover()
        {
            // Test case 1
            var message1 = "5c868fedb8026979ebd26f1ba07c27eedf4ff6d10443505a96ecaf21ba8c4f0937b3cd23ffdc3dd429d4cd1905fb8dbcceeff1350020e18b58d2ba70887baa3a9b783ad30d3fbf210331cdd7df8d77defa398cdacdfc2e359c7ba4cae46bb74401deb417f8b912a1aa966aeeba9c39c7dd22479ae2b30719dca2f2206c5eb4b7".HexToBytes();
            var messageHash1 = "5ae8317d34d1e595e3fa7247db80c0af4320cce1116de187f8f7e2e099c0d8d0".HexToBytes();
            var signature1 = "45c0b7f8c09a9e1f1cea0c25785594427b6bf8f9f878a8af0b1abbb48e16d0920d8becd0c220f67c51217eecfd7184ef0732481c843857e6bc7fc095c4f6b78801".HexToBytes();
            var expectedPubKey1 = "034a071e8a6e10aada2b8cf39fa3b5fb3400b04e99ea8ae64ceea1a977dbeaf5d5".HexToBytes();

            var recoveredKey1 = Crypto.ECRecover(signature1, messageHash1);
            CollectionAssert.AreEqual(expectedPubKey1, recoveredKey1.EncodePoint(true));

            // Test case 2
            var message2 = "17cd4a74d724d55355b6fb2b0759ca095298e3fd1856b87ca1cb2df5409058022736d21be071d820b16dfc441be97fbcea5df787edc886e759475469e2128b22f26b82ca993be6695ab190e673285d561d3b6d42fcc1edd6d12db12dcda0823e9d6079e7bc5ff54cd452dad308d52a15ce9c7edd6ef3dad6a27becd8e001e80f".HexToBytes();
            var messageHash2 = "586052916fb6f746e1d417766cceffbe1baf95579bab67ad49addaaa6e798862".HexToBytes();
            var signature2 = "4e0ea79d4a476276e4b067facdec7460d2c98c8a65326a6e5c998fd7c65061140e45aea5034af973410e65cf97651b3f2b976e3fc79c6a93065ed7cb69a2ab5a01".HexToBytes();
            var expectedPubKey2 = "02dbf1f4092deb3cfd4246b2011f7b24840bc5dbedae02f28471ce5b3bfbf06e71".HexToBytes();

            var recoveredKey2 = Crypto.ECRecover(signature2, messageHash2);
            CollectionAssert.AreEqual(expectedPubKey2, recoveredKey2.EncodePoint(true));

            // Test case 3 - recovery param 0
            var message3 = "db0d31717b04802adbbae1997487da8773440923c09b869e12a57c36dda34af11b8897f266cd81c02a762c6b74ea6aaf45aaa3c52867eb8f270f5092a36b498f88b65b2ebda24afe675da6f25379d1e194d093e7a2f66e450568dbdffebff97c4597a00c96a5be9ba26deefcca8761c1354429622c8db269d6a0ec0cc7a8585c".HexToBytes();
            var messageHash3 = "c36d0ecf4bfd178835c97aae7585f6a87de7dfa23cc927944f99a8d60feff68b".HexToBytes();
            var signature3 = "f25b86e1d8a11d72475b3ed273b0781c7d7f6f9e1dae0dd5d3ee9b84f3fab89163d9c4e1391de077244583e9a6e3d8e8e1f236a3bf5963735353b93b1a3ba93500".HexToBytes();
            var expectedPubKey3 = "03414549fd05bfb7803ae507ff86b99becd36f8d66037a7f5ba612792841d42eb9".HexToBytes();

            var recoveredKey3 = Crypto.ECRecover(signature3, messageHash3);
            CollectionAssert.AreEqual(expectedPubKey3, recoveredKey3.EncodePoint(true));

            // Test invalid cases
            var invalidSignature = new byte[65];
            Assert.ThrowsException<ArgumentException>(() => Crypto.ECRecover(invalidSignature, messageHash1));

            // Test with invalid recovery value
            var invalidRecoverySignature = signature1.ToArray();
            invalidRecoverySignature[64] = 29; // Invalid recovery value
            Assert.ThrowsException<ArgumentException>(() => Crypto.ECRecover(invalidRecoverySignature, messageHash1));

            // Test with wrong message hash
            var recoveredWrongHash = Crypto.ECRecover(signature1, messageHash2);
            CollectionAssert.AreNotEquivalent(expectedPubKey1, recoveredWrongHash.EncodePoint(true));
        }

        [TestMethod]
        public void TestERC2098()
        {
            // Test from https://eips.ethereum.org/EIPS/eip-2098

            // Private Key: 0x1234567890123456789012345678901234567890123456789012345678901234
            // Message: "Hello World"
            // Signature:
            // r:  0x68a020a209d3d56c46f38cc50a33f704f4a9a10a59377f8dd762ac66910e9b90
            // s:  0x7e865ad05c4035ab5792787d4a0297a43617ae897930a6fe4d822b8faea52064
            // v:  27

            var privateKey = "1234567890123456789012345678901234567890123456789012345678901234".HexToBytes();
            var expectedPubKey1 = (Neo.Cryptography.ECC.ECCurve.Secp256k1.G * privateKey).ToArray();

            Console.WriteLine($"Expected PubKey: {expectedPubKey1.ToHexString()}");
            var message1 = Encoding.UTF8.GetBytes("Hello World");
            var messageHash1 = new byte[] { 0x19 }.Concat(Encoding.UTF8.GetBytes($"Ethereum Signed Message:\n{message1.Count()}")).Concat(message1).ToArray().Keccak256();
            Console.WriteLine($"Message Hash: {Convert.ToHexString(messageHash1)}");

            // Signature values from EIP-2098 test case
            var r = "68a020a209d3d56c46f38cc50a33f704f4a9a10a59377f8dd762ac66910e9b90".HexToBytes();
            var s = "7e865ad05c4035ab5792787d4a0297a43617ae897930a6fe4d822b8faea52064".HexToBytes();
            var signature1 = new byte[65];
            Array.Copy(r, 0, signature1, 0, 32);
            Array.Copy(s, 0, signature1, 32, 32);
            signature1[64] = 27;

            Console.WriteLine($"r: {Convert.ToHexString(signature1.Take(32).ToArray())}");
            Console.WriteLine($"s: {Convert.ToHexString(signature1.Skip(32).Take(32).ToArray())}");
            Console.WriteLine($"yParity: {(signature1[32] & 0x80) != 0}");

            var recoveredKey1 = Crypto.ECRecover(signature1, messageHash1);

            // Private Key: 0x1234567890123456789012345678901234567890123456789012345678901234
            // Message: "It's a small(er) world"
            // Signature:
            // r:  0x9328da16089fcba9bececa81663203989f2df5fe1faa6291a45381c81bd17f76
            // s:  0x139c6d6b623b42da56557e5e734a43dc83345ddfadec52cbe24d0cc64f550793
            // v:  28

            var sig = "68a020a209d3d56c46f38cc50a33f704f4a9a10a59377f8dd762ac66910e9b90".HexToBytes()
              .Concat("7e865ad05c4035ab5792787d4a0297a43617ae897930a6fe4d822b8faea52064".HexToBytes())
              .Concat(new byte[] { 0x1B })
              .ToArray();

            var pubKey = Crypto.ECRecover(sig, messageHash1);

            Console.WriteLine($"Recovered PubKey: {pubKey.EncodePoint(true).ToHexString()}");
            Console.WriteLine($"Recovered PubKey: {recoveredKey1.EncodePoint(true).ToHexString()}");
            Assert.AreEqual(recoveredKey1, recoveredKey1);

            var message2Body = Encoding.UTF8.GetBytes("It's a small(er) world");
            var message2 = new byte[] { 0x19 }.Concat(Encoding.UTF8.GetBytes($"Ethereum Signed Message:\n{message2Body.Count()}")).Concat(message2Body).ToArray();
            var messageHash2 = message2.Keccak256();
            Console.WriteLine($"\nMessage Hash 2: {Convert.ToHexString(messageHash2)}");

            // Second test case from EIP-2098
            var r2 = "9328da16089fcba9bececa81663203989f2df5fe1faa6291a45381c81bd17f76".HexToBytes();
            var s2 = "939c6d6b623b42da56557e5e734a43dc83345ddfadec52cbe24d0cc64f550793".HexToBytes();
            var signature2 = new byte[64];
            Array.Copy(r2, 0, signature2, 0, 32);
            Array.Copy(s2, 0, signature2, 32, 32);

            Console.WriteLine($"r: {Convert.ToHexString(signature2.Take(32).ToArray())}");
            Console.WriteLine($"s: {Convert.ToHexString(signature2.Skip(32).Take(32).ToArray())}");
            Console.WriteLine($"yParity: {(signature2[32] & 0x80) != 0}");

            var recoveredKey2 = Crypto.ECRecover(signature2, messageHash2);
            CollectionAssert.AreEqual(expectedPubKey1, recoveredKey2.EncodePoint(true));
            Assert.IsTrue(Crypto.VerifySignature(message2, signature2, recoveredKey2, Neo.Cryptography.HashAlgorithm.Keccak256));
        }
    }
}
