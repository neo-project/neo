// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Cryptography_Helper.cs file belongs to the neo project and is free
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
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Cryptography_Helper
    {
        [TestMethod]
        public void TestBase58CheckDecode()
        {
            string input = "3vQB7B6MrGQZaxCuFg4oh";
            byte[] result = input.Base58CheckDecode();
            byte[] helloWorld = { 104, 101, 108, 108, 111, 32, 119, 111, 114, 108, 100 };
            result.Should().Equal(helloWorld);

            input = "3v";
            Action action = () => input.Base58CheckDecode();
            action.Should().Throw<FormatException>();

            input = "3vQB7B6MrGQZaxCuFg4og";
            action = () => input.Base58CheckDecode();
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestSha256()
        {
            byte[] value = Encoding.ASCII.GetBytes("hello world");
            byte[] result = value.Sha256(0, value.Length);
            string resultStr = result.ToHexString();
            resultStr.Should().Be("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9");
        }

        [TestMethod]
        public void TestRIPEMD160()
        {
            ReadOnlySpan<byte> value = Encoding.ASCII.GetBytes("hello world");
            byte[] result = value.RIPEMD160();
            string resultStr = result.ToHexString();
            resultStr.Should().Be("98c615784ccb5fe5936fbc0cbe9dfdb408d92f0f");
        }

        [TestMethod]
        public void TestAESEncryptAndDecrypt()
        {
            NEP6Wallet wallet = new NEP6Wallet("", "1", TestProtocolSettings.Default);
            wallet.CreateAccount();
            WalletAccount account = wallet.GetAccounts().ToArray()[0];
            KeyPair key = account.GetKey();
            Random random = new Random();
            byte[] nonce = new byte[12];
            random.NextBytes(nonce);
            var cypher = Neo.Cryptography.Helper.AES256Encrypt(Encoding.UTF8.GetBytes("hello world"), key.PrivateKey, nonce);
            var m = Neo.Cryptography.Helper.AES256Decrypt(cypher, key.PrivateKey);
            var message2 = Encoding.UTF8.GetString(m);
            Assert.AreEqual("hello world", message2);
        }

        [TestMethod]
        public void TestEcdhEncryptAndDecrypt()
        {
            NEP6Wallet wallet = new NEP6Wallet("", "1", ProtocolSettings.Default);
            wallet.CreateAccount();
            wallet.CreateAccount();
            WalletAccount account1 = wallet.GetAccounts().ToArray()[0];
            KeyPair key1 = account1.GetKey();
            WalletAccount account2 = wallet.GetAccounts().ToArray()[1];
            KeyPair key2 = account2.GetKey();
            Console.WriteLine($"Account:{1},privatekey:{key1.PrivateKey.ToHexString()},publicKey:{key1.PublicKey.ToArray().ToHexString()}");
            Console.WriteLine($"Account:{2},privatekey:{key2.PrivateKey.ToHexString()},publicKey:{key2.PublicKey.ToArray().ToHexString()}");
            var secret1 = Neo.Cryptography.Helper.ECDHDeriveKey(key1, key2.PublicKey);
            var secret2 = Neo.Cryptography.Helper.ECDHDeriveKey(key2, key1.PublicKey);
            Assert.AreEqual(secret1.ToHexString(), secret2.ToHexString());
            var message = Encoding.ASCII.GetBytes("hello world");
            Random random = new Random();
            byte[] nonce = new byte[12];
            random.NextBytes(nonce);
            var cypher = message.AES256Encrypt(secret1, nonce);
            cypher.AES256Decrypt(secret2);
            Assert.AreEqual("hello world", Encoding.ASCII.GetString(cypher.AES256Decrypt(secret2)));
        }

        [TestMethod]
        public void TestTest()
        {
            int m = 7, n = 10;
            uint nTweak = 123456;
            BloomFilter filter = new(m, n, nTweak);

            Transaction tx = new()
            {
                Script = TestUtils.GetByteArray(32, 0x42),
                SystemFee = 4200000000,
                Signers = new Signer[] { new Signer() { Account = (Array.Empty<byte>()).ToScriptHash() } },
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = new[]
                {
                    new Witness
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = Array.Empty<byte>()
                    }
                }
            };
            filter.Test(tx).Should().BeFalse();
            filter.Add(tx.Witnesses[0].ScriptHash.ToArray());
            filter.Test(tx).Should().BeTrue();
            filter.Add(tx.Hash.ToArray());
            filter.Test(tx).Should().BeTrue();
        }
    }
}
