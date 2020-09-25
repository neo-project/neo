using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System;
using System.Security;
using System.Text;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_Cryptography_Helper
    {
        [TestMethod]
        public void TestAES256Encrypt()
        {
            byte[] block = Encoding.ASCII.GetBytes("00000000000000000000000000000000");
            byte[] key = Encoding.ASCII.GetBytes("1234567812345678");
            byte[] result = block.AES256Encrypt(key);
            string encryptString = result.ToHexString();
            encryptString.Should().Be("f69e0923d8247eef417d6a78944a4b39f69e0923d8247eef417d6a78944a4b39");
        }

        [TestMethod]
        public void TestAES256Decrypt()
        {
            byte[] block = new byte[32];
            byte[] key = Encoding.ASCII.GetBytes("1234567812345678");
            string decryptString = "f69e0923d8247eef417d6a78944a4b39f69e0923d8247eef417d6a78944a4b399ae8fd02b340288a0e7bbff0f0ba54d6";
            for (int i = 0; i < 32; i++)
                block[i] = Convert.ToByte(decryptString.Substring(i * 2, 2), 16);
            string str = System.Text.Encoding.Default.GetString(block.AES256Decrypt(key));
            str.Should().Be("00000000000000000000000000000000");
        }

        [TestMethod]
        public void TestAesEncrypt()
        {
            byte[] data = Encoding.ASCII.GetBytes("00000000000000000000000000000000");
            byte[] key = Encoding.ASCII.GetBytes("12345678123456781234567812345678");
            byte[] iv = Encoding.ASCII.GetBytes("1234567812345678");
            byte[] result = data.AesEncrypt(key, iv);

            string encryptString = result.ToHexString();
            encryptString.Should().Be("07c748cf7d326782f82e60ebe60e2fac289e84e9ce91c1bc41565d14ecb53640");

            byte[] nullData = null;
            Action action = () => nullData.AesEncrypt(key, iv);
            action.Should().Throw<ArgumentNullException>();

            byte[] nullKey = null;
            action = () => data.AesEncrypt(nullKey, iv);
            action.Should().Throw<ArgumentNullException>();

            byte[] nullIv = null;
            action = () => data.AesEncrypt(key, nullIv);
            action.Should().Throw<ArgumentNullException>();

            byte[] wrongData = Encoding.ASCII.GetBytes("000000000000000000000000000000001"); ;
            action = () => wrongData.AesEncrypt(key, iv);
            action.Should().Throw<ArgumentException>();

            byte[] wrongKey = Encoding.ASCII.GetBytes("123456781234567812345678123456780"); ;
            action = () => data.AesEncrypt(wrongKey, iv);
            action.Should().Throw<ArgumentException>();

            byte[] wrongIv = Encoding.ASCII.GetBytes("12345678123456780"); ;
            action = () => data.AesEncrypt(key, wrongIv);
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestAesDecrypt()
        {
            byte[] data = new byte[32];
            byte[] key = Encoding.ASCII.GetBytes("12345678123456781234567812345678");
            byte[] iv = Encoding.ASCII.GetBytes("1234567812345678");
            string decryptString = "07c748cf7d326782f82e60ebe60e2fac289e84e9ce91c1bc41565d14ecb5364073f28c9aa7bd6b069e44d8a97beb2b58";
            for (int i = 0; i < 32; i++)
                data[i] = Convert.ToByte(decryptString.Substring(i * 2, 2), 16);
            string str = System.Text.Encoding.Default.GetString(data.AesDecrypt(key, iv));
            str.Should().Be("00000000000000000000000000000000");

            byte[] nullData = null;
            Action action = () => nullData.AesDecrypt(key, iv);
            action.Should().Throw<ArgumentNullException>();

            byte[] nullKey = null;
            action = () => data.AesDecrypt(nullKey, iv);
            action.Should().Throw<ArgumentNullException>();

            byte[] nullIv = null;
            action = () => data.AesDecrypt(key, nullIv);
            action.Should().Throw<ArgumentNullException>();

            byte[] wrongData = Encoding.ASCII.GetBytes("00000000000000001"); ;
            action = () => wrongData.AesDecrypt(key, iv);
            action.Should().Throw<ArgumentException>();

            byte[] wrongKey = Encoding.ASCII.GetBytes("123456781234567812345678123456780"); ;
            action = () => data.AesDecrypt(wrongKey, iv);
            action.Should().Throw<ArgumentException>();

            byte[] wrongIv = Encoding.ASCII.GetBytes("12345678123456780"); ;
            action = () => data.AesDecrypt(key, wrongIv);
            action.Should().Throw<ArgumentException>();
        }

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
        public void TestTest()
        {
            int m = 7, n = 10;
            uint nTweak = 123456;
            BloomFilter filter = new BloomFilter(m, n, nTweak);

            Transaction tx = new Transaction(ProtocolSettings.Default.Magic)
            {
                Script = TestUtils.GetByteArray(32, 0x42),
                SystemFee = 4200000000,
                Signers = new Signer[] { new Signer() { Account = (new byte[0]).ToScriptHash() } },
                Attributes = Array.Empty<TransactionAttribute>(),
                Witnesses = new[]
                {
                    new Witness
                    {
                        InvocationScript = new byte[0],
                        VerificationScript = new byte[0]
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
            byte[] byteArray = new byte[string1.Length / 2];
            byteArray = string1.HexToBytes();
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
            byte[] byteArray = new byte[string1.Length / 2];
            byteArray = string1.HexToBytes();
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
            byteArray = new byte[0];
            result.Should().Equal(byteArray);
        }
    }
}
