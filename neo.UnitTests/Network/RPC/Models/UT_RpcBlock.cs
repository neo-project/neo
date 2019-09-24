using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using System;

namespace Neo.UnitTests.Network.RPC.Models
{
    [TestClass]
    public class UT_RpcBlock
    {
        [TestMethod]
        public void TestToJson()
        {
            var rpcBlock = new RpcBlock();
            var block = new Block();
            TestUtils.SetupBlockWithValues(block, UInt256.Zero, out UInt256 merkRootVal, out UInt160 val160, out ulong timestampVal, out uint indexVal, out Witness scriptVal, out Transaction[] transactionsVal, 1);
            rpcBlock.Block = block;
            var json = rpcBlock.ToJson();
            json["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            Console.WriteLine(json.AsString());
            json.Should().NotBeNull();

            rpcBlock.Confirmations = 1;
            rpcBlock.NextBlockHash = UInt256.Zero;
            json = rpcBlock.ToJson();
            json["confirmations"].AsNumber().Should().Be(1);
        }
    }
}
