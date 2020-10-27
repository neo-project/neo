using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.IO.Json;
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
        public static void ClassSetup(TestContext context)
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            _keyPair = new KeyPair(privateKey);
            _nep2 = _keyPair.Export("Satoshi", 2, 1, 1);
        }

        [TestInitialize]
        public void TestSetup()
        {
            _wallet = TestUtils.GenerateTestWallet();
            byte[] array1 = { 0x01 };
            _hash = new UInt160(Crypto.Hash160(array1));
            _account = new NEP6Account(_wallet, _hash);
        }

        [TestMethod]
        public void TestChangePassword()
        {
            _account = new NEP6Account(_wallet, _hash, _nep2);
            _account.ChangePasswordPrepare("b", "Satoshi").Should().BeTrue();
            _account.ChangePasswordCommit();
            _account.Contract = new Contract();
            _wallet.Unlock("Satoshi");
            _account.ChangePasswordPrepare("b", "Satoshi").Should().BeFalse();
            _account.ChangePasswordPrepare("Satoshi", "b").Should().BeTrue();
            _account.ChangePasswordCommit();
            _account.VerifyPassword("b").Should().BeTrue();
            _account.ChangePasswordPrepare("b", "Satoshi").Should().BeTrue();
            _account.ChangePasswordCommit();
            _account.ChangePasswordPrepare("Satoshi", "b").Should().BeTrue();
            _account.ChangePasswordRoolback();
            _account.VerifyPassword("Satoshi").Should().BeTrue();
            _wallet.Lock();
        }

        [TestMethod]
        public void TestConstructorWithNep2Key()
        {
            _account.ScriptHash.Should().Be(_hash);
            _account.Decrypted.Should().BeTrue();
            _account.HasKey.Should().BeFalse();
        }

        [TestMethod]
        public void TestConstructorWithKeyPair()
        {
            var wallet = TestUtils.GenerateTestWallet();
            byte[] array1 = { 0x01 };
            var hash = new UInt160(Crypto.Hash160(array1));
            string password = "hello world";
            NEP6Account account = new NEP6Account(wallet, hash, _keyPair, password);
            account.ScriptHash.Should().Be(hash);
            account.Decrypted.Should().BeTrue();
            account.HasKey.Should().BeTrue();
        }

        [TestMethod]
        public void TestFromJson()
        {
            JObject json = new JObject();
            json["address"] = "NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf";
            json["key"] = null;
            json["label"] = null;
            json["isdefault"] = true;
            json["lock"] = false;
            json["contract"] = null;
            json["extra"] = null;
            NEP6Account account = NEP6Account.FromJson(json, _wallet);
            account.ScriptHash.Should().Be("NdtB8RXRmJ7Nhw1FPTm7E6HoDZGnDw37nf".ToScriptHash());
            account.Label.Should().BeNull();
            account.IsDefault.Should().BeTrue();
            account.Lock.Should().BeFalse();
            account.Contract.Should().BeNull();
            account.Extra.Should().BeNull();
            account.GetKey().Should().BeNull();

            json["key"] = "6PYRjVE1gAbCRyv81FTiFz62cxuPGw91vMjN4yPa68bnoqJtioreTznezn";
            json["label"] = "label";
            account = NEP6Account.FromJson(json, _wallet);
            account.Label.Should().Be("label");
            account.HasKey.Should().BeTrue();
        }

        [TestMethod]
        public void TestGetKey()
        {
            _account.GetKey().Should().BeNull();
            _wallet.Unlock("Satoshi");
            _account = new NEP6Account(_wallet, _hash, _nep2);
            _account.GetKey().Should().Be(_keyPair);
        }

        [TestMethod]
        public void TestGetKeyWithString()
        {
            _account.GetKey("Satoshi").Should().BeNull();
            _account = new NEP6Account(_wallet, _hash, _nep2);
            _account.GetKey("Satoshi").Should().Be(_keyPair);
        }

        [TestMethod]
        public void TestToJson()
        {
            JObject nep6contract = new JObject();
            nep6contract["script"] = "IQNgPziA63rqCtRQCJOSXkpC/qSKRO5viYoQs8fOBdKiZ6w=";
            JObject parameters = new JObject();
            parameters["type"] = 0x00;
            parameters["name"] = "Sig";
            JArray array = new JArray
            {
                parameters
            };
            nep6contract["parameters"] = array;
            nep6contract["deployed"] = false;
            _account.Contract = NEP6Contract.FromJson(nep6contract);
            JObject json = _account.ToJson();
            json["address"].Should().Equals("AZk5bAanTtD6AvpeesmYgL8CLRYUt5JQsX");
            json["label"].Should().BeNull();
            json["isdefault"].ToString().Should().Be("false");
            json["lock"].ToString().Should().Be("false");
            json["key"].Should().BeNull();
            json["contract"]["script"].ToString().Should().Be(@"""IQNgPziA63rqCtRQCJOSXkpC/qSKRO5viYoQs8fOBdKiZ6w=""");
            json["extra"].Should().BeNull();

            _account.Contract = null;
            json = _account.ToJson();
            json["contract"].Should().BeNull();
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            _account = new NEP6Account(_wallet, _hash, _nep2);
            _account.VerifyPassword("Satoshi").Should().BeTrue();
            _account.VerifyPassword("b").Should().BeFalse();
        }
    }
}
