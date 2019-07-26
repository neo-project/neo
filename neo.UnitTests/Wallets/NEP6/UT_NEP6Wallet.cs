using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Wallets.NEP6;
using System;

namespace Neo.UnitTests.Wallets.NEP6
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
            // test minimally scryptparameters parsing here
            ScryptParameters.FromJson(wallet["scrypt"]).Should().NotBeNull();
            ScryptParameters.FromJson(wallet["scrypt"]).N.Should().Be(ScryptParameters.Default.N);
            wallet["accounts"] = new JArray();
            //accounts = ((JArray)wallet["accounts"]).Select(p => NEP6Account.FromJson(p, this)).ToDictionary(p => p.ScriptHash);
            wallet["extra"] = new JObject();
            // check string json
            wallet.ToString().Should().Be("{\"name\":\"name\",\"version\":\"0.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[],\"extra\":{}}");
            uut = new NEP6Wallet(wallet);
        }

        [TestMethod]
        public void Test_NEP6Wallet_Json()
        {
            uut.Name.Should().Be("name");
            uut.Version.Should().Be(new Version());
            uut.Scrypt.Should().NotBeNull();
            uut.Scrypt.N.Should().Be(ScryptParameters.Default.N);
        }
    }
}
