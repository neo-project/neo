using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_TransactionOutput
    {
        TransactionOutput uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new TransactionOutput();
        }

        [TestMethod]
        public void AssetId_Get()
        {
            uut.AssetId.Should().BeNull();
        }

        [TestMethod]
        public void AssetId_Set()
        {
            UInt256 val = new UInt256(TestUtils.GetByteArray(32, 0x42));
            uut.AssetId = val;
            uut.AssetId.Should().Be(val);
        }

        [TestMethod]
        public void Value_Get()
        {
            uut.Value.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void Value_Set()
        {
            Fixed8 val = Fixed8.FromDecimal(42);
            uut.Value = val;
            uut.Value.Should().Be(val);
        }

        [TestMethod]
        public void ScriptHash_Get()
        {
            uut.ScriptHash.Should().BeNull();
        }

        [TestMethod]
        public void ScriptHash_Set()
        {
            UInt160 val = new UInt160(TestUtils.GetByteArray(20, 0x42));
            uut.ScriptHash = val;
            uut.ScriptHash.Should().Be(val);
        }

        [TestMethod]
        public void Size_Get()
        {
            uut.AssetId = new UInt256(TestUtils.GetByteArray(32, 0x42));
            uut.Value = Fixed8.FromDecimal(42);
            uut.ScriptHash = new UInt160(TestUtils.GetByteArray(20, 0x42));

            uut.Size.Should().Be(60); // 32 + 8 + 20
        }

        [TestMethod]
        public void ToJson()
        {
            uut.AssetId = new UInt256(TestUtils.GetByteArray(32, 0x42));
            uut.Value = Fixed8.FromDecimal(42);
            uut.ScriptHash = new UInt160(TestUtils.GetByteArray(20, 0x42));

            JObject jObj = uut.ToJson(36);
            jObj.Should().NotBeNull();
            jObj["n"].AsNumber().Should().Be(36);
            jObj["asset"].AsString().Should().Be("0x2020202020202020202020202020202020202020202020202020202020202042");
            jObj["value"].AsString().Should().Be("42");
            jObj["address"].AsString().Should().Be("AMoWjH3BDwMY7j8FEAovPJdq8XEuyJynwN");
        }

    }
}
