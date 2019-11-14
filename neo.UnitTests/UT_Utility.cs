using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.Wallets;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_Utility
    {
        private KeyPair keyPair;
        private UInt160 scriptHash;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            scriptHash = Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash();
        }

        [TestMethod]
        public void TestGetKeyPair()
        {
            string nul = null;
            Assert.ThrowsException<ArgumentNullException>(() => Utility.GetKeyPair(nul));

            string wif = "KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p";
            var result = Utility.GetKeyPair(wif);
            Assert.AreEqual(keyPair, result);

            string privateKey = keyPair.PrivateKey.ToHexString();
            result = Utility.GetKeyPair(privateKey);
            Assert.AreEqual(keyPair, result);
        }

        [TestMethod]
        public void TestGetScriptHash()
        {
            string nul = null;
            Assert.ThrowsException<ArgumentNullException>(() => Utility.GetScriptHash(nul));

            string addr = scriptHash.ToAddress();
            var result = Utility.GetScriptHash(addr);
            Assert.AreEqual(scriptHash, result);

            string hash = scriptHash.ToString();
            result = Utility.GetScriptHash(hash);
            Assert.AreEqual(scriptHash, result);

            string publicKey = keyPair.PublicKey.ToString();
            result = Utility.GetScriptHash(publicKey);
            Assert.AreEqual(scriptHash, result);
        }
    }
}
