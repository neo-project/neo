// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Numerics;

namespace Neo.Network.RPC.Tests
{
    [TestClass]
    public class UT_Utility
    {
        private KeyPair keyPair;
        private UInt160 scriptHash;
        private ProtocolSettings protocolSettings;

        [TestInitialize]
        public void TestSetup()
        {
            keyPair = new KeyPair(Wallet.GetPrivateKeyFromWIF("KyXwTh1hB76RRMquSvnxZrJzQx7h9nQP2PCRL38v6VDb5ip3nf1p"));
            scriptHash = Contract.CreateSignatureRedeemScript(keyPair.PublicKey).ToScriptHash();
            protocolSettings = ProtocolSettings.Load("protocol.json");
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
            Assert.ThrowsException<ArgumentNullException>(() => Utility.GetScriptHash(nul, protocolSettings));

            string addr = scriptHash.ToAddress(protocolSettings.AddressVersion);
            var result = Utility.GetScriptHash(addr, protocolSettings);
            Assert.AreEqual(scriptHash, result);

            string hash = scriptHash.ToString();
            result = Utility.GetScriptHash(hash, protocolSettings);
            Assert.AreEqual(scriptHash, result);

            string publicKey = keyPair.PublicKey.ToString();
            result = Utility.GetScriptHash(publicKey, protocolSettings);
            Assert.AreEqual(scriptHash, result);
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
            Assert.ThrowsException<ArgumentException>(() => result = amount.ToBigInteger(decimals));
        }
    }
}
