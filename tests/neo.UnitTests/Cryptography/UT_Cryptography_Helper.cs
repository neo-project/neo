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
using System.Security;
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
            NEP6Wallet wallet = new NEP6Wallet("", ProtocolSettings.Default);
            wallet.Unlock("1");
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
            NEP6Wallet wallet = new NEP6Wallet("", ProtocolSettings.Default);
            wallet.Unlock("1");
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

        [TestMethod]
        public void TestStringToAesKey()
        {
            string password = "hello world";
            string string1 = "bc62d4b80d9e36da29c16c5d4d9f11731f36052c72401a76c23c0fb5a9b74423";
            byte[] byteArray = string1.HexToBytes();
            password.ToAesKey().Should().Equal(byteArray);
        }

        [TestMethod]
        public void TestSecureStringToAesKey()
        {
            var password = new SecureString();
            password.AppendChar('h');
            password.AppendChar('e');
            password.AppendChar('l');
            password.AppendChar('l');
            password.AppendChar('o');
            password.AppendChar(' ');
            password.AppendChar('w');
            password.AppendChar('o');
            password.AppendChar('r');
            password.AppendChar('l');
            password.AppendChar('d');
            string string1 = "bc62d4b80d9e36da29c16c5d4d9f11731f36052c72401a76c23c0fb5a9b74423";
            byte[] byteArray = string1.HexToBytes();
            password.ToAesKey().Should().Equal(byteArray);
        }

        [TestMethod]
        public void TestToArray()
        {
            var password = new SecureString();
            password.AppendChar('h');
            password.AppendChar('e');
            password.AppendChar('l');
            password.AppendChar('l');
            password.AppendChar('o');
            password.AppendChar(' ');
            password.AppendChar('w');
            password.AppendChar('o');
            password.AppendChar('r');
            password.AppendChar('l');
            password.AppendChar('d');
            byte[] byteArray = { 0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x77, 0x6F, 0x72, 0x6C, 0x64 };
            password.ToArray().Should().Equal(byteArray);

            SecureString nullString = null;
            Action action = () => nullString.ToArray();
            action.Should().Throw<NullReferenceException>();

            var zeroString = new SecureString();
            var result = zeroString.ToArray();
            byteArray = Array.Empty<byte>();
            result.Should().Equal(byteArray);
        }
    }
}
