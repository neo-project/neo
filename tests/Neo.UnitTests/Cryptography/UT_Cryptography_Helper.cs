// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Cryptography_Helper.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using System.Text;
using Helper = Neo.Cryptography.Helper;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Cryptography_Helper
    {
        [TestMethod]
        public void TestBase58CheckDecode()
        {
            string input = "3vQB7B6MrGQZaxCuFg4oh";
            var result = input.Base58CheckDecode();
            byte[] helloWorld = { 104, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100 };
            CollectionAssert.AreEqual(helloWorld, result);

            input = "3v";
            var action = () => input.Base58CheckDecode();
            Assert.ThrowsExactly<FormatException>(action);

            input = "3vQB7B6MrGQZaxCuFg4og";
            action = () => input.Base58CheckDecode();
            Assert.ThrowsExactly<FormatException>(action);
            Assert.ThrowsExactly<FormatException>(() => _ = string.Empty.Base58CheckDecode());
        }

        [TestMethod]
        public void TestMurmurReadOnlySpan()
        {
            ReadOnlySpan<byte> input = "Hello, world!"u8;
            byte[] input2 = input.ToArray();
            Assert.AreEqual(input2.Murmur32(0), input.Murmur32(0));
            CollectionAssert.AreEqual(input2.Murmur128(0), input.Murmur128(0));
        }

        [TestMethod]
        public void TestSha256()
        {
            var value = Encoding.ASCII.GetBytes("hello world");
            var result = value.Sha256(0, value.Length);
            Assert.AreEqual("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", result.ToHexString());
            CollectionAssert.AreEqual(result, value.Sha256());
            CollectionAssert.AreEqual(result, ((Span<byte>)value).Sha256());
            CollectionAssert.AreEqual(result, ((ReadOnlySpan<byte>)value).Sha256());
        }

        [TestMethod]
        public void TestSha512()
        {
            var value = Encoding.ASCII.GetBytes("hello world");
            var result = value.Sha512(0, value.Length);
            Assert.AreEqual("309ecc489c12d6eb4cc40f50c902f2b4d0ed77ee511a7c7a9bcd3ca86d4cd86f989dd35bc5ff499670da34255b45b0cfd830e81f605dcf7dc5542e93ae9cd76f", result.ToHexString());
            CollectionAssert.AreEqual(result, value.Sha512());
            CollectionAssert.AreEqual(result, ((Span<byte>)value).Sha512());
            CollectionAssert.AreEqual(result, ((ReadOnlySpan<byte>)value).Sha512());
        }

        [TestMethod]
        public void TestKeccak256()
        {
            var input = "Hello, world!"u8.ToArray();
            var result = input.Keccak256();
            Assert.AreEqual("b6e16d27ac5ab427a7f68900ac5559ce272dc6c37c82b3e052246c82244c50e4", result.ToHexString());
            CollectionAssert.AreEqual(result, ((Span<byte>)input).Keccak256());
            CollectionAssert.AreEqual(result, ((ReadOnlySpan<byte>)input).Keccak256());
        }

        [TestMethod]
        public void TestRIPEMD160()
        {
            ReadOnlySpan<byte> value = Encoding.ASCII.GetBytes("hello world");
            var result = value.RIPEMD160();
            Assert.AreEqual("98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f", result.ToHexString());
        }

        [TestMethod]
        public void TestAESEncryptAndDecrypt()
        {
            var wallet = new NEP6Wallet("", "1", TestProtocolSettings.Default);
            wallet.CreateAccount();

            var account = wallet.GetAccounts().ToArray()[0];
            var key = account.GetKey();
            var random = new Random();
            var nonce = new byte[12];
            random.NextBytes(nonce);

            var cypher = Helper.AES256Encrypt(Encoding.UTF8.GetBytes("hello world"), key.PrivateKey, nonce);
            var m = Helper.AES256Decrypt(cypher, key.PrivateKey);
            var message2 = Encoding.UTF8.GetString(m);
            Assert.AreEqual("hello world", message2);
        }

        [TestMethod]
        public void TestEcdhEncryptAndDecrypt()
        {
            var wallet = new NEP6Wallet("", "1", ProtocolSettings.Default);
            wallet.CreateAccount();
            wallet.CreateAccount();

            var account1 = wallet.GetAccounts().ToArray()[0];
            var key1 = account1.GetKey();
            var account2 = wallet.GetAccounts().ToArray()[1];
            var key2 = account2.GetKey();
            Console.WriteLine($"Account:{1},privatekey:{key1.PrivateKey.ToHexString()},publicKey:{key1.PublicKey.ToArray().ToHexString()}");
            Console.WriteLine($"Account:{2},privatekey:{key2.PrivateKey.ToHexString()},publicKey:{key2.PublicKey.ToArray().ToHexString()}");

            var secret1 = Helper.ECDHDeriveKey(key1, key2.PublicKey);
            var secret2 = Helper.ECDHDeriveKey(key2, key1.PublicKey);
            Assert.AreEqual(secret1.ToHexString(), secret2.ToHexString());

            var message = Encoding.ASCII.GetBytes("hello world");
            var random = new Random();
            var nonce = new byte[12];
            random.NextBytes(nonce);

            var cypher = message.AES256Encrypt(secret1, nonce);
            cypher.AES256Decrypt(secret2);
            Assert.AreEqual("hello world", Encoding.ASCII.GetString(cypher.AES256Decrypt(secret2)));

            Assert.ThrowsExactly<ArgumentException>(() => Helper.AES256Decrypt(new byte[11], key1.PrivateKey));
            Assert.ThrowsExactly<ArgumentException>(() => Helper.AES256Decrypt(new byte[11 + 16], key1.PrivateKey));
        }

        [TestMethod]
        public void TestTest()
        {
            int m = 7, n = 10;
            uint nTweak = 123456;
            var filter = new BloomFilter(m, n, nTweak);
            var tx = new Transaction()
            {
                Script = TestUtils.GetByteArray(32, 0x42),
                SystemFee = 4200000000,
                Signers = [new() { Account = Array.Empty<byte>().ToScriptHash() }],
                Attributes = [],
                Witnesses = [Witness.Empty]
            };
            Assert.IsFalse(filter.Test(tx));
            filter.Add(tx.Witnesses[0].ScriptHash.ToArray());
            Assert.IsTrue(filter.Test(tx));
            filter.Add(tx.Hash.ToArray());
            Assert.IsTrue(filter.Test(tx));
        }

        [TestMethod]
        public void TestSha3_512()
        {
            var data = "hello world"u8;
            var hash = data.Sha3_512();
            var expected = "840006653e9ac9e95117a15c915caab81662918e925de9e004f774ff82d7079a40d4d27b1b372657c61d46d470304c88c788b3a4527ad074d1dccbee5dbaa99a";
            Assert.AreEqual(expected, hash.ToHexString());

            hash = data.ToArray().Sha3_512();
            Assert.AreEqual(expected, hash.ToHexString());
        }

        [TestMethod]
        public void TestSha3_256()
        {
            var data = "hello world"u8;
            var hash = data.Sha3_256();
            var expected = "644bcc7e564373040999aac89e7622f3ca71fba1d972fd94a31c3bfbf24e3938";
            Assert.AreEqual(expected, hash.ToHexString());

            hash = data.ToArray().Sha3_256();
            Assert.AreEqual(expected, hash.ToHexString());
        }

        [TestMethod]
        public void TestBlake2b_512()
        {
            var data = "hello world"u8;
            var hash = data.Blake2b_512();
            var expected = "021ced8799296ceca557832ab941a50b4a11f83478cf141f51f933f653ab9fbcc05a037cddbed06e309bf334942c4e58cdf1a46e237911ccd7fcf9787cbc7fd0";
            Assert.AreEqual(expected, hash.ToHexString());

            hash = data.ToArray().Blake2b_512();
            Assert.AreEqual(expected, hash.ToHexString());

            var salt = "0123456789abcdef"u8;
            expected = "d986f099932b14a65ebc5a6fb1b8bff8d05b6924a4ff74d4972949b880c1f74b5ab263357f332726d98fac3cabeacf415099f1a2a9b97b66cd989ca865539640";
            hash = data.Blake2b_512(salt.ToArray());
            Assert.AreEqual(expected, hash.ToHexString());
        }

        [TestMethod]
        public void TestBlake2b_256()
        {
            var data = "hello world"u8;
            var hash = data.Blake2b_256();
            var expected = "256c83b297114d201b30179f3f0ef0cace9783622da5974326b436178aeef610";
            Assert.AreEqual(expected, hash.ToHexString());

            hash = data.ToArray().Blake2b_256();
            Assert.AreEqual(expected, hash.ToHexString());

            var salt = "0123456789abcdef"u8;
            hash = data.Blake2b_256(salt.ToArray());
            Assert.AreEqual("779c5f2194a9c2c03e73e3ffcf3e1508dd83cb85cd861029415ab961a755cc4e", hash.ToHexString());
        }
    }
}
