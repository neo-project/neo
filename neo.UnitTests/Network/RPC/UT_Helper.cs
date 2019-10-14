using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC
{
    [TestClass]
    public class UT_Helper
    {
        KeyPair keyPair;
        UInt160 scriptHash;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            scriptHash = Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash();
        }

        [TestMethod]
        public void TestToUInt160()
        {
            string nul = null;
            Assert.ThrowsException<ArgumentNullException>(() => nul.ToUInt160());

            string addr = Neo.Wallets.Helper.ToAddress(scriptHash);
            var result = addr.ToUInt160();
            Assert.AreEqual(scriptHash, result);

            string hash = scriptHash.ToString();
            result = hash.ToUInt160();
            Assert.AreEqual(scriptHash, result);

            string publicKey = keyPair.PublicKey.ToString();
            result = publicKey.ToUInt160();
            Assert.AreEqual(scriptHash, result);
        }

        [TestMethod]
        public void TestToKeyPair()
        {
            string nul = null;
            Assert.ThrowsException<ArgumentNullException>(() => nul.ToKeyPair());

            string wif = "KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p";
            var result = wif.ToKeyPair();
            Assert.AreEqual(keyPair, result);

            string privateKey = keyPair.PrivateKey.ToHexString();
            result = privateKey.ToKeyPair();
            Assert.AreEqual(keyPair, result);
        }

        [TestMethod]
        public void TestToBigInteger()
        {
            decimal amount = 1.23456789m;
            uint decimals = 9;
            var result = amount.ToBigInteger(decimals);
            Assert.AreEqual(1234567890, result);

            amount = 1.23456789m;
            decimals = 18;
            result = amount.ToBigInteger(decimals);
            Assert.AreEqual(BigInteger.Parse("1234567890000000000"), result);

            amount = 1.23456789m;
            decimals = 4;
            Assert.ThrowsException<OverflowException>(() => result = amount.ToBigInteger(decimals));
        }
    }
}
