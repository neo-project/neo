using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;
using Neo.Wallets;
using Neo.VM;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_MinerTransaction
    {
        MinerTransaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new MinerTransaction();
        }

        [TestMethod]
        public void Nonce_Get()
        {
            uut.Nonce.Should().Be(0u);
        }

        [TestMethod]
        public void Nonce_Set()
        {
            uint val = 42;
            uut.Nonce = val;
            uut.Nonce.Should().Be(val);
        }

        [TestMethod]
        public void NetworkFee_Get()
        {
            uut.NetworkFee.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.Size.Should().Be(10); // 1, 1, 1, 1, 1, 1 + 4
        }

        [TestMethod]
        public void ToJson()
        {
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];
            uut.Nonce = 42;

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["txid"].AsString().Should().Be("0xe42ca5744eda6de2e1a2bdc2ed98fa7b967b13cd3aa2605c95fff37261f07ef6");
            jObj["size"].AsNumber().Should().Be(10);
            jObj["type"].AsString().Should().Be("MinerTransaction");
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            ((JArray)jObj["vin"]).Count.Should().Be(0);
            ((JArray)jObj["vout"]).Count.Should().Be(0);
            jObj["sys_fee"].AsNumber().Should().Be(0);
            jObj["net_fee"].AsNumber().Should().Be(0);
            ((JArray)jObj["scripts"]).Count.Should().Be(0);

            jObj["nonce"].AsNumber().Should().Be(42);            
        }

    }
}
