using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using System;

namespace Neo.UnitTests.Network.RPC.Models
{
    [TestClass]
    public class UT_RpcBlockHeader
    {
        [TestMethod]
        public void TestToJson()
        {
            var rpcBlockHeader = new RpcBlockHeader();
            var header = new Header();
            TestUtils.SetupHeaderWithValues(header, UInt256.Zero, out UInt256 _, out UInt160 _, out ulong _, out uint _, out Witness _);
            rpcBlockHeader.Header = header;
            var json = rpcBlockHeader.ToJson();
            json["previousblockhash"].AsString().Should().Be("0x0000000000000000000000000000000000000000000000000000000000000000");
            json.Should().NotBeNull();

            rpcBlockHeader.Confirmations = 1;
            rpcBlockHeader.NextBlockHash = UInt256.Zero;
            json = rpcBlockHeader.ToJson();
            json["confirmations"].AsNumber().Should().Be(1);
        }
    }
}
