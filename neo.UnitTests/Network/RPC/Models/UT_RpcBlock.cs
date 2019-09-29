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
            TestUtils.SetupBlockWithValues(block, UInt256.Zero, out UInt256 _, out UInt160 _, out ulong _, out uint _, out Witness _, out Transaction[] _, 1);
            rpcBlock.Block = block;
            var json = rpcBlock.ToJson();
            json["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            json.Should().NotBeNull();

            rpcBlock.Confirmations = 1;
            rpcBlock.NextBlockHash = UInt256.Zero;
            json = rpcBlock.ToJson();
            json["confirmations"].AsNumber().Should().Be(1);
        }
    }
}
