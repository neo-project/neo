using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC.Models;

namespace Neo.UnitTests.Network.RPC.Models
{
    [TestClass]
    public class UT_RpcVersion
    {
        [TestMethod]
        public void TestToJson()
        {
            var version = new RpcVersion()
            {
                TcpPort = 800,
                WsPort = 900,
                Nonce = 1,
                UserAgent = "agent"
            };
            var json = version.ToJson();
            json["topPort"].AsNumber().Should().Be(800);
            json["wsPort"].AsNumber().Should().Be(900);
            json["nonce"].AsNumber().Should().Be(1);
            json["useragent"].AsString().Should().Be("agent");
        }
    }
}
