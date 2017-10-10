using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_InvocationTransaction
    {
        InvocationTransaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new InvocationTransaction();
        }

        [TestMethod]
        public void Script_Get()
        {
            uut.Script.Should().BeNull();
        }

        [TestMethod]
        public void Script_Set()
        {
            byte[] val = TestUtils.GetByteArray(32, 0x42);
            uut.Script = val;
            uut.Script.Length.Should().Be(32);
            for (int i = 0; i < val.Length; i++)
            {
                uut.Script[i].Should().Be(val[i]);
            }
        }

        [TestMethod]
        public void Gas_Get()
        {
            uut.Gas.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void Gas_Set()
        {
            Fixed8 val = Fixed8.FromDecimal(42);
            uut.Gas = val;
            uut.Gas.Should().Be(val);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            byte[] val = TestUtils.GetByteArray(32, 0x42);
            uut.Script = val;
            uut.Size.Should().Be(39); // 1, 1, 1, 1, 1, 1 + script 33
        }

        [TestMethod]
        public void SystemFee_Get()
        {
            uut.SystemFee.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void SystemFee_Get_FromGas()
        {
            Fixed8 val = Fixed8.FromDecimal(42);
            uut.Gas = val;
            uut.SystemFee.Should().Be(val);
        }

        [TestMethod]
        public void SystemFee_Set()
        {
            Fixed8 val = Fixed8.FromDecimal(42);
            uut.Gas = val;
            uut.SystemFee.Should().Be(val);
        }

        [TestMethod]
        public void ToJson()
        {
            byte[] scriptVal = TestUtils.GetByteArray(32, 0x42);
            uut.Script = scriptVal;
            Fixed8 gasVal = Fixed8.FromDecimal(42);
            uut.Gas = gasVal;

            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["txid"].AsString().Should().Be("0x8258b950487299376f89ad2d09598b7acbc5cde89b161b3dd73c256f9e2a94b1");
            jObj["size"].AsNumber().Should().Be(39);
            jObj["type"].AsString().Should().Be("InvocationTransaction");
            jObj["version"].AsNumber().Should().Be(0);
            ((JArray)jObj["attributes"]).Count.Should().Be(0);
            ((JArray)jObj["vin"]).Count.Should().Be(0);
            ((JArray)jObj["vout"]).Count.Should().Be(0);
            jObj["sys_fee"].AsString().Should().Be("42");
            jObj["net_fee"].AsString().Should().Be("-42");
            ((JArray)jObj["scripts"]).Count.Should().Be(0);

            jObj["script"].AsString().Should().Be("4220202020202020202020202020202020202020202020202020202020202020");
            jObj["gas"].AsNumber().Should().Be(42);
        }
    }
}
