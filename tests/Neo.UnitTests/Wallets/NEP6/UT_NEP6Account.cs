// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NEP6Account.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Json;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;

namespace Neo.UnitTests.Wallets.NEP6
{
    [TestClass]
    public class UT_NEP6Account
    {
        NEP6Account _account;
        UInt160 _hash;
        NEP6Wallet _wallet;
        private static string _nep2;
        private static KeyPair _keyPair;

        [ClassInitialize]
        public static void ClassSetup(TestContext ctx)
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            _keyPair = new KeyPair(privateKey);
            _nep2 = _keyPair.Export("Satoshi", TestProtocolSettings.Default.AddressVersion, 2, 1, 1);
        }

        [TestInitialize]
        public void TestSetup()
        {
            _wallet = TestUtils.GenerateTestWallet("Satoshi");
            byte[] array1 = { 0x01 };
            _hash = new UInt160(Crypto.Hash160(array1));
            _account = new NEP6Account(_wallet, _hash);
        }

        [TestMethod]
        public void TestChangePassword()
        {
            _account = new NEP6Account(_wallet, _hash, _nep2);
            Assert.IsTrue(_account.ChangePasswordPrepare("b", "Satoshi"));
            _account.ChangePasswordCommit();
            _account.Contract = new Contract();
            Assert.IsFalse(_account.ChangePasswordPrepare("b", "Satoshi"));
            Assert.IsTrue(_account.ChangePasswordPrepare("Satoshi", "b"));
            _account.ChangePasswordCommit();
            Assert.IsTrue(_account.VerifyPassword("b"));
            Assert.IsTrue(_account.ChangePasswordPrepare("b", "Satoshi"));
            _account.ChangePasswordCommit();
            Assert.IsTrue(_account.ChangePasswordPrepare("Satoshi", "b"));
            _account.ChangePasswordRollback();
            Assert.IsTrue(_account.VerifyPassword("Satoshi"));
        }

        [TestMethod]
        public void TestConstructorWithNep2Key()
        {
            Assert.AreEqual(_hash, _account.ScriptHash);
            Assert.IsTrue(_account.Decrypted);
            Assert.IsFalse(_account.HasKey);
        }

        [TestMethod]
        public void TestConstructorWithKeyPair()
        {
            string password = "hello world";
            var wallet = TestUtils.GenerateTestWallet(password);
            byte[] array1 = { 0x01 };
            var hash = new UInt160(Crypto.Hash160(array1));
            NEP6Account account = new(wallet, hash, _keyPair, password);
            Assert.AreEqual(hash, account.ScriptHash);
            Assert.IsTrue(account.Decrypted);
            Assert.IsTrue(account.HasKey);
        }

        [TestMethod]
        public void TestFromJson()
        {
            JObject json = new();
            json["address"] = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            json["key"] = null;
            json["label"] = null;
            json["isDefault"] = true;
            json["lock"] = false;
            json["contract"] = null;
            json["extra"] = null;
            NEP6Account account = NEP6Account.FromJson(json, _wallet);
            Assert.AreEqual("NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf".ToScriptHash(TestProtocolSettings.Default.AddressVersion), account.ScriptHash);
            Assert.IsNull(account.Label);
            Assert.IsTrue(account.IsDefault);
            Assert.IsFalse(account.Lock);
            Assert.IsNull(account.Contract);
            Assert.IsNull(account.Extra);
            Assert.IsNull(account.GetKey());

            json["key"] = "6PYRjVE1gAbCRyv81FTiFz62cxuPGw91vMjN4yPa68bnoqJtioreTznezn";
            json["label"] = "label";
            account = NEP6Account.FromJson(json, _wallet);
            Assert.AreEqual("label", account.Label);
            Assert.IsTrue(account.HasKey);
        }

        [TestMethod]
        public void TestGetKey()
        {
            Assert.IsNull(_account.GetKey());
            _account = new NEP6Account(_wallet, _hash, _nep2);
            Assert.AreEqual(_keyPair, _account.GetKey());
        }

        [TestMethod]
        public void TestGetKeyWithString()
        {
            Assert.IsNull(_account.GetKey("Satoshi"));
            _account = new NEP6Account(_wallet, _hash, _nep2);
            Assert.AreEqual(_keyPair, _account.GetKey("Satoshi"));
        }

        [TestMethod]
        public void TestToJson()
        {
            JObject nep6contract = new();
            nep6contract["script"] = "IQNgPziA63rqCtRQCJOSXkpC/qSKRO5viYoQs8fOBdKiZ6w=";
            JObject parameters = new();
            parameters["type"] = 0x00;
            parameters["name"] = "Sig";
            JArray array = new()
            {
                parameters
            };
            nep6contract["parameters"] = array;
            nep6contract["deployed"] = false;
            _account.Contract = NEP6Contract.FromJson(nep6contract);
            JObject json = _account.ToJson();
            Assert.AreEqual("NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf", json["address"].AsString());
            Assert.IsNull(json["label"]);
            Assert.AreEqual("false", json["isDefault"].ToString());
            Assert.AreEqual("false", json["lock"].ToString());
            Assert.IsNull(json["key"]);
            Assert.AreEqual(@"""IQNgPziA63rqCtRQCJOSXkpC/qSKRO5viYoQs8fOBdKiZ6w=""", json["contract"]["script"].ToString());
            Assert.IsNull(json["extra"]);

            _account.Contract = null;
            json = _account.ToJson();
            Assert.IsNull(json["contract"]);
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            _account = new NEP6Account(_wallet, _hash, _nep2);
            Assert.IsTrue(_account.VerifyPassword("Satoshi"));
            Assert.IsFalse(_account.VerifyPassword("b"));
        }
    }
}
