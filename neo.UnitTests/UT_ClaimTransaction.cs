using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ClaimTransaction
    {
        ClaimTransaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new ClaimTransaction();
        }

        [TestMethod]
        public void Claims_Get()
        {
            uut.Claims.Should().BeNull();            
        }

        [TestMethod]
        public void Claims_Set()
        {
            CoinReference val = new CoinReference();
            CoinReference[] refs = new CoinReference[] { val };
            uut.Claims = refs;
            uut.Claims.Length.Should().Be(1);
            uut.Claims[0].Should().Be(val);
        }

        [TestMethod]
        public void NetworkFee_Get()
        {
            uut.NetworkFee.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void Size__Get_0_Claims()
        {
            CoinReference[] refs = new CoinReference[0];
            uut.Claims = refs;

            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.Size.Should().Be(7); // 1, 1, 1, 1, 1, 1 + claims 1
        }

        [TestMethod]
        public void Size__Get_1_Claims()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(null) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.Size.Should().Be(41); // 1, 1, 1, 1, 1, 1 + claims 35
        }

        [TestMethod]
        public void Size__Get_3_Claims()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(null), TestUtils.GetCoinReference(null), TestUtils.GetCoinReference(null) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.Size.Should().Be(109); // 1, 1, 1, 1, 1, 1 + claims 103
        }

        [TestMethod]
        public void GetScriptHashesForVerifying_0_Claims()
        {            
            uut.Claims = new CoinReference[0];
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.GetScriptHashesForVerifying().Length.Should().Be(0);
        }

        [TestMethod]
        public void GetScriptHashesForVerifying_1_Claim()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(new UInt256(TestUtils.GetByteArray(32, 0x42))) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            TestUtils.SetupTestBlockchain(UInt256.Zero);

            UInt160[] res = uut.GetScriptHashesForVerifying();
            res.Length.Should().Be(1);
        }


        [TestMethod]
        public void GetScriptHashesForVerifying_2_Claim()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(new UInt256(TestUtils.GetByteArray(32, 0x42))), TestUtils.GetCoinReference(new UInt256(TestUtils.GetByteArray(32, 0x48))) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            TestUtils.SetupTestBlockchain(UInt256.Zero);

            UInt160[] res = uut.GetScriptHashesForVerifying();
            res.Length.Should().Be(2);
        }

        [TestMethod]
        public void ToJson()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(new UInt256(TestUtils.GetByteArray(32, 0x42))) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["txid"].AsString().Should().Be("0x45a5c537ca95d62add6b331cad0fd742ce29ada02c50ea7e4d709f83563972b9");
            jObj["size"].AsNumber().Should().Be(41);
            jObj["type"].AsString().Should().Be("ClaimTransaction");
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            ((JArray)jObj["vin"]).Count.Should().Be(0);
            ((JArray)jObj["vout"]).Count.Should().Be(0);
            jObj["sys_fee"].AsString().Should().Be("0");
            jObj["net_fee"].AsString().Should().Be("0");
            ((JArray)jObj["scripts"]).Count.Should().Be(0);

            JArray claims = (JArray) jObj["claims"];
            claims.Count.Should().Be(1);
            claims[0]["txid"].AsString().Should().Be("0x2020202020202020202020202020202020202020202020202020202020202042");
            claims[0]["vout"].AsNumber().Should().Be(0);
        }

    }
}
