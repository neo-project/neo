using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Json;
using Neo.Network.RPC.Models;

namespace Neo.UnitTests.Network.RPC.Models
{
    [TestClass]
    public class UT_RpcResponse
    {
        [TestMethod]
        public void TestToJson()
        {
            var error = new RpcResponseError()
            {
                Code = 0,
                Message = "msg",
                Data = new JBoolean(true)
            };
            var rep = new RpcResponse()
            {
                Id = 1,
                Jsonrpc = "rpc",
                Error = error,
                Result = new JBoolean(true)
            };
            var json = rep.ToJson();
            json["id"].AsNumber().Should().Be(1);
            json["jsonrpc"].AsString().Should().Be("rpc");
            json["error"].AsString().Should().Be(error.ToJson().AsString());
            json["result"].AsBoolean().Should().BeTrue();
        }
    }
}
