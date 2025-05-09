// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NEP6Wallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

        public static string GetRandomPath(string ext = null)
        {
            var rnd = new Random().Next(1, 1000000);
            var threadName = Environment.CurrentManagedThreadId.ToString();
            return Path.GetFullPath($"Wallet_{rnd:X8}{threadName}{ext}");
        }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var privateKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            keyPair = new KeyPair(privateKey);
            testScriptHash = Contract.CreateSignatureContract(keyPair.PublicKey).ScriptHash;
            nep2key = keyPair.Export("123", TestProtocolSettings.Default.AddressVersion, 2, 1, 1);
        }

        private string CreateWalletFile()
        {
            rootPath = GetRandomPath();
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

            var path = Path.Combine(rootPath, "wallet.json");
            File.WriteAllText(path, "{\"name\":\"name\",\"version\":\"1.0\",\"scrypt\":{\"n\":2,\"r\":1,\"p\":1},\"accounts\":[],\"extra\":{}}");
            return path;
        }

        [TestInitialize]
        public void TestSetup()
        {
            uut = TestUtils.GenerateTestWallet("123");
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
            var acc = uut.CreateAccount("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632549".HexToBytes());
            var tx = new Transaction()
            {
                Attributes = [],
                Script = new byte[1],
                Signers = [new Signer() { Account = acc.ScriptHash }],
            };
            var ctx = new ContractParametersContext(TestBlockchain.GetTestSnapshotCache(), tx, TestProtocolSettings.Default.Network);
            Assert.IsTrue(uut.Sign(ctx));
            tx.Witnesses = ctx.GetWitnesses();
            Assert.IsTrue(tx.VerifyWitnesses(TestProtocolSettings.Default, TestBlockchain.GetTestSnapshotCache(), long.MaxValue));
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = uut.CreateAccount(null));
            Assert.ThrowsExactly<ArgumentException>(() => _ = uut.CreateAccount("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551".HexToBytes()));
        }

        [TestMethod]
        public void TestChangePassword()
        {
            var wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            File.WriteAllText(wPath, wallet.ToString());

            uut = new NEP6Wallet(wPath, "123", TestProtocolSettings.Default);
            uut.CreateAccount(keyPair.PrivateKey);
            Assert.IsFalse(uut.ChangePassword("456", "123"));
            Assert.IsTrue(uut.ChangePassword("123", "456"));
            Assert.IsTrue(uut.VerifyPassword("456"));
            Assert.IsTrue(uut.ChangePassword("456", "123"));
        }

        [TestMethod]
        public void TestConstructorWithPathAndName()
        {
            var wallet = new NEP6Wallet(wPath, "123", TestProtocolSettings.Default);
            Assert.AreEqual("name", wallet.Name);
            Assert.AreEqual(new ScryptParameters(2, 1, 1).ToJson().ToString(), wallet.Scrypt.ToJson().ToString());
            Assert.AreEqual(new Version("1.0").ToString(), wallet.Version.ToString());

            wallet = new NEP6Wallet("", "123", TestProtocolSettings.Default, "test");
            Assert.AreEqual("test", wallet.Name);
            Assert.AreEqual(ScryptParameters.Default.ToJson().ToString(), wallet.Scrypt.ToJson().ToString());
            Assert.AreEqual(Version.Parse("1.0"), wallet.Version);

            wallet = new NEP6Wallet("wallet.json", "123", TestProtocolSettings.Default, "");
            Assert.AreEqual("wallet", wallet.Name);
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
            Assert.AreEqual(
                "{\"name\":\"test\",\"version\":\"1.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[],\"extra\":{}}",
                wallet.ToString());

            var w = new NEP6Wallet(null, "123", TestProtocolSettings.Default, wallet);
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
            var result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);

            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestAddCount()
        {
            uut.CreateAccount(testScriptHash);
            Assert.IsTrue(uut.Contains(testScriptHash));

            var account = uut.GetAccount(testScriptHash);
            Assert.IsTrue(account.WatchOnly);
            Assert.IsFalse(account.HasKey);

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
            var result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCreateAccountWithKeyPair()
        {
            var contract = Contract.CreateSignatureContract(keyPair.PublicKey);
            var result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);
            uut.CreateAccount(contract);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);
            uut.CreateAccount(contract, keyPair);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCreateAccountWithScriptHash()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestDecryptKey()
        {
            var nep2key = keyPair.Export("123", ProtocolSettings.Default.AddressVersion, 2, 1, 1);
            var key1 = uut.DecryptKey(nep2key);
            var result = key1.Equals(keyPair);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestDeleteAccount()
        {
            var result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestGetAccount()
        {
            var result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
            var account = uut.GetAccount(testScriptHash);
            var address = Contract.CreateSignatureRedeemScript(keyPair.PublicKey)
                .ToScriptHash()
                .ToAddress(ProtocolSettings.Default.AddressVersion);
            Assert.AreEqual(address, account.Address);
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            var keys = new Dictionary<UInt160, KeyPair>();
            var privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }

            var key = new KeyPair(privateKey);
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
            var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest(
                new X500DistinguishedName("CN=Self-Signed ECDSA"),
                key,
                HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: false));
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));
            var start = DateTimeOffset.UtcNow;
            var cert = request.CreateSelfSigned(notBefore: start, notAfter: start.AddMonths(3));
            return cert;
        }

        [TestMethod]
        public void TestImportCert()
        {
            var cert = NewCertificate();
            Assert.IsNotNull(cert);
            Assert.IsTrue(cert.HasPrivateKey);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.ThrowsExactly<PlatformNotSupportedException>(() => _ = uut.Import(cert));
                return;
            }
            var account = uut.Import(cert);
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestImportWif()
        {
            var wif = keyPair.Export();
            var result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);

            uut.Import(wif);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestImportNep2()
        {
            var result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);

            uut.Import(nep2key, "123", 2, 1, 1);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);

            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);

            var wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();

            uut = new NEP6Wallet(null, "123", ProtocolSettings.Default, wallet);
            result = uut.Contains(testScriptHash);
            Assert.IsFalse(result);

            uut.Import(nep2key, "123", 2, 1, 1);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestMigrate()
        {
            var path = GetRandomPath(".json");
            var uw = Wallet.Create(null, path, "123", ProtocolSettings.Default);
            uw.CreateAccount(keyPair.PrivateKey);
            uw.Save();
            var npath = GetRandomPath(".json");
            var nw = Wallet.Migrate(npath, path, "123", ProtocolSettings.Default);
            var result = nw.Contains(testScriptHash);
            Assert.IsTrue(result);
            uw.Delete();
            nw.Delete();
        }

        [TestMethod]
        public void TestSave()
        {
            var wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            File.WriteAllText(wPath, wallet.ToString());

            uut = new NEP6Wallet(wPath, "123", ProtocolSettings.Default);
            uut.CreateAccount(keyPair.PrivateKey);

            var result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
            uut.Save();
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestToJson()
        {
            Assert.AreEqual(
                "{\"name\":\"noname\",\"version\":\"1.0\",\"scrypt\":{\"n\":2,\"r\":1,\"p\":1},\"accounts\":[],\"extra\":null}",
                uut.ToJson().ToString());
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            var result = uut.VerifyPassword("123");
            Assert.IsTrue(result);

            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.IsTrue(result);

            result = uut.VerifyPassword("123");
            Assert.IsTrue(result);

            uut.DeleteAccount(testScriptHash);
            Assert.IsFalse(uut.Contains(testScriptHash));

            var wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();

            uut = new NEP6Wallet(null, "123", ProtocolSettings.Default, wallet);
            nep2key = keyPair.Export("123", ProtocolSettings.Default.AddressVersion, 2, 1, 1);
            uut.Import(nep2key, "123", 2, 1, 1);
            Assert.IsFalse(uut.VerifyPassword("1"));
            Assert.IsTrue(uut.VerifyPassword("123"));
        }

        [TestMethod]
        public void Test_NEP6Wallet_Json()
        {
            Assert.AreEqual("noname", uut.Name);
            Assert.AreEqual(new Version("1.0"), uut.Version);
            Assert.IsNotNull(uut.Scrypt);
            Assert.AreEqual(new ScryptParameters(2, 1, 1).N, uut.Scrypt.N);
        }

        [TestMethod]
        public void TestIsDefault()
        {
            var wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new Version("1.0").ToString();
            wallet["scrypt"] = new ScryptParameters(2, 1, 1).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();

            var w = new NEP6Wallet(null, "", ProtocolSettings.Default, wallet);
            var ac = w.CreateAccount();
            Assert.AreEqual(ac.Address, w.GetDefaultAccount().Address);

            var ac2 = w.CreateAccount();
            Assert.AreEqual(ac.Address, w.GetDefaultAccount().Address);
            ac2.IsDefault = true;
            Assert.AreEqual(ac2.Address, w.GetDefaultAccount().Address);
        }
    }
}
