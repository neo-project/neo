using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.RPC.Models;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC.Models
{
    [TestClass]
    public class UT_RpcNep5Balance
    {
        private RpcNep5Balance balance;

        [TestInitialize]
        public void Setup()
        {
            balance = new RpcNep5Balance();
        }

        [TestMethod]
        public void TestAssetHash()
        {
            balance.AssetHash = UInt160.Zero;
            balance.AssetHash.Should().Be(UInt160.Zero);
        }

        [TestMethod]
        public void TestAmount()
        {
            balance.Amount = BigInteger.Zero;
            balance.Amount.Should().Be(BigInteger.Zero);
        }

        [TestMethod]
        public void TestLastUpdatedBlock()
        {
            balance.LastUpdatedBlock = 0;
            balance.LastUpdatedBlock.Should().Be(0);
        }

        [TestMethod]
        public void TestToJson()
        {
            balance.AssetHash = UInt160.Zero;
            balance.Amount = BigInteger.Zero;
            balance.LastUpdatedBlock = 0;
            var json = balance.ToJson();
            json["asset_hash"].AsString().Should().Be("0000000000000000000000000000000000000000");
            json["amount"].AsNumber().Should().Be(0);
            json["last_updated_block"].AsNumber().Should().Be(0);
        }

        [TestMethod]
        public void TestFromJson()
        {
            var json = new JObject();
            json["asset_hash"] = "0000000000000000000000000000000000000000";
            json["amount"] = "0";
            json["last_updated_block"] = "0";
            var rpcNep5Balance = RpcNep5Balance.FromJson(json);
            rpcNep5Balance.AssetHash.Should().Be(UInt160.Zero);
            rpcNep5Balance.Amount.Should().Be(BigInteger.Zero);
            rpcNep5Balance.LastUpdatedBlock.Should().Be(0);
        }
    }
}
