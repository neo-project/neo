using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.RPC.Models;

namespace Neo.UnitTests.Network.RPC.Models
{
    [TestClass]
    public class UT_RpcRawMemPool
    {
        [TestMethod]
        public void TestToJson()
        {
            var pool = new RpcRawMemPool
            {
                Height = 1,
                Verified = new string[] {
                "a", "b"
                },
                UnVerified = new string[] {
                "c", "d"
                }
            };
            var json = pool.ToJson();
            json["height"].AsNumber().Should().Be(1);
            json["verified"].AsString().Should().Be("a,b");
            json["unverified"].AsString().Should().Be("c,d");
        }
    }
}
