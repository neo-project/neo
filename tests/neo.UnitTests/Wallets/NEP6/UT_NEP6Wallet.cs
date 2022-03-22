using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Contract = Neo.SmartContract.Contract;

namespace Neo.UnitTests.Wallets.NEP6
{
    [TestClass]
    public class UT_NEP6Wallet
    {
        private NEP6Wallet uut;
        private string wPath;
        private static KeyPair keyPair;
        private static string nep2key;
        private static UInt160 testScriptHash;
        private string rootPath;

        public static string GetRandomPath()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            return Path.GetFullPath(string.Format("Wallet_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
        }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            keyPair = new KeyPair(privateKey);
            testScriptHash = Contract.CreateSignatureContract(keyPair.PublicKey).ScriptHash;
            nep2key = keyPair.Export("123", ProtocolSettings.Default.AddressVersion, 2, 1, 1);
        }

        private NEP6Wallet CreateWallet()
        {
            return TestUtils.GenerateTestWallet();
        }

        private string CreateWalletFile()
        {
            rootPath = GetRandomPath();
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
            string path = Path.Combine(rootPath, "wallet.json");
            File.WriteAllText(path, "{\"name\":\"name\",\"version\":\"1.0\",\"scrypt\":{\"n\":2,\"r\":1,\"p\":1},\"accounts\":[],\"extra\":{}}");
            return path;
        }

        [TestInitialize]
        public void TestSetup()
        {
            uut = CreateWallet();
            wPath = CreateWalletFile();
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            if (File.Exists(wPath)) File.Delete(wPath);
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath);
        }

        [TestMethod]
        public void TestCreateAccount()
        {
            var uut = new NEP6Wallet(wPath, ProtocolSettings.Default);
            uut.Unlock("123");
            var acc = uut.CreateAccount("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632549".HexToBytes());
            var tx = new Transaction()
            {
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = new byte[1],
                Signers = new Signer[] { new Signer() { Account = acc.ScriptHash } },
            };
            var ctx = new ContractParametersContext(TestBlockchain.GetTestSnapshot(), tx, ProtocolSettings.Default.Network);
            var sig = uut.Sign(ctx);
            tx.Witnesses = ctx.GetWitnesses();
            Assert.IsTrue(tx.VerifyWitnesses(ProtocolSettings.Default, TestBlockchain.GetTestSnapshot(), long.MaxValue));
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount((byte[])null));
            Assert.ThrowsException<ArgumentException>(() => uut.CreateAccount("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551".HexToBytes()));
        }

        [TestMethod]
        public void TestChangePassword()
        {
            JObject wallet = new();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            File.WriteAllText(wPath, wallet.ToString());
            uut = new NEP6Wallet(wPath, ProtocolSettings.Default);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            uut.ChangePassword("456", "123").Should().BeFalse();
            uut.ChangePassword("123", "456").Should().BeTrue();
            uut.VerifyPassword("456").Should().BeTrue();
            uut.ChangePassword("456", "123").Should().BeTrue();
            uut.Lock();
        }

        [TestMethod]
        public void TestConstructorWithPathAndName()
        {
            NEP6Wallet wallet = new(wPath, ProtocolSettings.Default);
            Assert.AreEqual("name", wallet.Name);
            Assert.AreEqual(new ScryptParameters(2, 1, 1).ToJson().ToString(), wallet.Scrypt.ToJson().ToString());
            Assert.AreEqual(new Version("1.0").ToString(), wallet.Version.ToString());
            wallet = new NEP6Wallet("", ProtocolSettings.Default, "test");
            Assert.AreEqual("test", wallet.Name);
            Assert.AreEqual(ScryptParameters.Default.ToJson().ToString(), wallet.Scrypt.ToJson().ToString());
            Assert.AreEqual(Version.Parse("1.0"), wallet.Version);
        }

        [TestMethod]
        public void TestConstructorWithJObject()
        {
            JObject wallet = new();
            wallet["name"] = "test";
            wallet["version"] = Version.Parse("1.0").ToString();
            wallet["scrypt"] = ScryptParameters.Default.ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            wallet.ToString().Should().Be("{\"name\":\"test\",\"version\":\"1.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[],\"extra\":{}}");
            NEP6Wallet w = new(null, ProtocolSettings.Default, wallet);
            Assert.AreEqual("test", w.Name);
            Assert.AreEqual(Version.Parse("1.0").ToString(), w.Version.ToString());
        }

        [TestMethod]
        public void TestGetName()
        {
            Assert.AreEqual("noname", uut.Name);
        }

        [TestMethod]
        public void TestGetVersion()
        {
            Assert.AreEqual(new Version("1.0").ToString(), uut.Version.ToString());
        }

        [TestMethod]
        public void TestContains()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestAddCount()
        {
            uut.CreateAccount(testScriptHash);
            Assert.IsTrue(uut.Contains(testScriptHash));
            WalletAccount account = uut.GetAccount(testScriptHash);
            Assert.IsTrue(account.WatchOnly);
            Assert.IsFalse(account.HasKey);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            account = uut.GetAccount(testScriptHash);
            Assert.IsFalse(account.WatchOnly);
            Assert.IsTrue(account.HasKey);
            uut.CreateAccount(testScriptHash);
            account = uut.GetAccount(testScriptHash);
            Assert.IsFalse(account.WatchOnly);
            Assert.IsFalse(account.HasKey);
            uut.CreateAccount(keyPair.PrivateKey);
            account = uut.GetAccount(testScriptHash);
            Assert.IsFalse(account.WatchOnly);
            Assert.IsTrue(account.HasKey);
        }

        [TestMethod]
        public void TestCreateAccountWithPrivateKey()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestCreateAccountWithKeyPair()
        {
            Neo.SmartContract.Contract contract = Neo.SmartContract.Contract.CreateSignatureContract(keyPair.PublicKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(contract);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.CreateAccount(contract, keyPair);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestCreateAccountWithScriptHash()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestDecryptKey()
        {
            string nep2key = keyPair.Export("123", ProtocolSettings.Default.AddressVersion, 2, 1, 1);
            uut.Unlock("123");
            KeyPair key1 = uut.DecryptKey(nep2key);
            bool result = key1.Equals(keyPair);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestDeleteAccount()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestGetAccount()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            WalletAccount account = uut.GetAccount(testScriptHash);
            Assert.AreEqual(Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash().ToAddress(ProtocolSettings.Default.AddressVersion), account.Address);
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            Dictionary<UInt160, KeyPair> keys = new();
            uut.Unlock("123");
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new(privateKey);
            keys.Add(Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash(), key);
            keys.Add(Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash(), keyPair);
            uut.CreateAccount(key.PrivateKey);
            uut.CreateAccount(keyPair.PrivateKey);
            foreach (var account in uut.GetAccounts())
            {
                if (!keys.ContainsKey(account.ScriptHash))
                {
                    Assert.Fail();
                }
            }
        }

        public X509Certificate2 NewCertificate()
        {
            ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest request = new(
                "CN=Self-Signed ECDSA",
                key,
                HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: false));
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));
            DateTimeOffset start = DateTimeOffset.UtcNow;
            X509Certificate2 cert = request.CreateSelfSigned(notBefore: start, notAfter: start.AddMonths(3));
            return cert;
        }

        [TestMethod]
        public void TestImportCert()
        {
            X509Certificate2 cert = NewCertificate();
            Assert.IsNotNull(cert);
            Assert.AreEqual(true, cert.HasPrivateKey);
            uut.Unlock("123");
            WalletAccount account = uut.Import(cert);
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestImportWif()
        {
            string wif = keyPair.Export();
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.Import(wif);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestImportNep2()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Import(nep2key, "123", 2, 1, 1);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            JObject wallet = new();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            uut = new NEP6Wallet(null, ProtocolSettings.Default, wallet);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Import(nep2key, "123", 2, 1, 1);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestLock()
        {
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            uut.Lock();
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
        }

        [TestMethod]
        public void TestSave()
        {
            JObject wallet = new();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            File.WriteAllText(wPath, wallet.ToString());
            uut = new NEP6Wallet(wPath, ProtocolSettings.Default);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.Save();
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestUnlock()
        {
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            Assert.ThrowsException<CryptographicException>(() => uut.Unlock("1"));
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            bool result = uut.VerifyPassword("123");
            Assert.AreEqual(true, result);
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            result = uut.VerifyPassword("123");
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            Assert.AreEqual(false, uut.Contains(testScriptHash));
            JObject wallet = new();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            uut = new NEP6Wallet(null, ProtocolSettings.Default, wallet);
            nep2key = keyPair.Export("123", ProtocolSettings.Default.AddressVersion, 2, 1, 1);
            uut.Import(nep2key, "123", 2, 1, 1);
            Assert.IsFalse(uut.VerifyPassword("1"));
            Assert.IsTrue(uut.VerifyPassword("123"));
        }

        [TestMethod]
        public void Test_NEP6Wallet_Json()
        {
            uut.Name.Should().Be("noname");
            uut.Version.Should().Be(new Version("1.0"));
            uut.Scrypt.Should().NotBeNull();
            uut.Scrypt.N.Should().Be(new ScryptParameters(2, 1, 1).N);
        }

        [TestMethod]
        public void TestIsDefault()
        {
            JObject wallet = new();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            var w = new NEP6Wallet(null, ProtocolSettings.Default, wallet);
            using var l = w.Unlock("");
            var ac = w.CreateAccount();
            Assert.AreEqual(ac.Address, w.GetDefaultAccount().Address);
            var ac2 = w.CreateAccount();
            Assert.AreEqual(ac.Address, w.GetDefaultAccount().Address);
            ac2.IsDefault = true;
            Assert.AreEqual(ac2.Address, w.GetDefaultAccount().Address);
        }
    }
}
