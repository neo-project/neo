using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.RPC.Models;
using System.Numerics;

namespace Neo.UnitTests.Network.RPC.Models
{
    [TestClass]
    public class UT_RpcNep5Balances
    {
        private RpcNep5Balances balances;

        [TestInitialize]
        public void Setup()
        {
            balances = new RpcNep5Balances()
            {
                Address = "abc",
                Balances = new RpcNep5Balance[] {
                    new RpcNep5Balance()
                    {
                        AssetHash = UInt160.Zero,
                        Amount = BigInteger.Zero,
                        LastUpdatedBlock = 0
                    },
                    new RpcNep5Balance()
                    {
                        AssetHash = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                        Amount = new BigInteger(1),
                        LastUpdatedBlock = 1
                    }
                }
            };
        }

        [TestMethod]
        public void TestAddress()
        {
            balances.Address.Should().Be("abc");
        }

        [TestMethod]
        public void TestBalances()
        {
            balances.Balances.Length.Should().Be(2);
        }

        [TestMethod]
        public void TestToJson()
        {
            var json = balances.ToJson();
            json["address"].AsString().Should().Be("abc");
            ((JArray)json["balance"]).Count.Should().Be(2);
        }

        [TestMethod]
        public void TestFromJson()
        {
            var json = balances.ToJson();
            var rpcNep5Balances = RpcNep5Balances.FromJson(json);
            rpcNep5Balances.Address.Should().Be("abc");
            rpcNep5Balances.Balances.Length.Should().Be(2);
        }
    }
}
