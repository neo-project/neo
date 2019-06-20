using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using Neo.Wallets.NEP6;
using System;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_NEP6Wallet
    {
        NEP6Wallet uut;

        [TestInitialize]
        public void TestSetup()
        {
            JObject wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new System.Version().ToString();
            wallet["scrypt"] = ScryptParameters.Default.ToJson();
            wallet["accounts"] = new JArray();
            //accounts = ((JArray)wallet["accounts"]).Select(p => NEP6Account.FromJson(p, this)).ToDictionary(p => p.ScriptHash);
            wallet["extra"] = new JObject();
            uut = new NEP6Wallet(wallet);
        }

        [TestMethod]
        public void Test_NEP6Wallet()
        {
        }
    }
}