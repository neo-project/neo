using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.SmartContract.Manifest;
using Neo.Wallets;
using System;
using System.Linq;

namespace Neo.UnitTests.SmartContract.Manifest
{
    [TestClass]
    public class UT_ContractGroup
    {
        [TestMethod]
        public void TestClone()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            random.NextBytes(privateKey);
            KeyPair keyPair = new KeyPair(privateKey);
            ContractGroup contractGroup = new ContractGroup
            {
                PubKey = keyPair.PublicKey,
                Signature = new byte[20]
            };

            var clone = contractGroup.Clone();
            Assert.AreEqual(clone.ToJson().ToString(), contractGroup.ToJson().ToString());
        }

        [TestMethod]
        public void TestIsValid()
        {
            Random random = new Random();
            byte[] privateKey = new byte[32];
            random.NextBytes(privateKey);
            KeyPair keyPair = new KeyPair(privateKey);
            ContractGroup contractGroup = new ContractGroup
            {
                PubKey = keyPair.PublicKey,
                Signature = new byte[20]
            };
            Assert.AreEqual(false, contractGroup.IsValid(UInt160.Zero));


            byte[] message = new byte[] {  0x01,0x01,0x01,0x01,0x01,
                                           0x01,0x01,0x01,0x01,0x01,
                                           0x01,0x01,0x01,0x01,0x01,
                                           0x01,0x01,0x01,0x01,0x01 };
            byte[] signature = Crypto.Sign(message, keyPair.PrivateKey, keyPair.PublicKey.EncodePoint(false).Skip(1).ToArray());
            contractGroup = new ContractGroup
            {
                PubKey = keyPair.PublicKey,
                Signature = signature
            };
            Assert.AreEqual(true, contractGroup.IsValid(new UInt160(message)));
        }
    }
}
