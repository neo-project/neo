using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Capabilities;
using System;
using System.IO;

namespace Neo.UnitTests.Network.P2P.Capabilities
{
    [TestClass]
    public class UT_ServerCapability
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new ServerCapability(NodeCapabilityType.TcpServer) { Port = 1 };
            test.Size.Should().Be(3);

            test = new ServerCapability(NodeCapabilityType.WsServer) { Port = 2 };
            test.Size.Should().Be(3);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new ServerCapability(NodeCapabilityType.WsServer) { Port = 2 };
            var buffer = test.ToArray();

            using var br = new BinaryReader(new MemoryStream(buffer));
            var clone = (ServerCapability)ServerCapability.DeserializeFrom(br);

            Assert.AreEqual(test.Port, clone.Port);
            Assert.AreEqual(test.Type, clone.Type);

            Assert.ThrowsException<ArgumentException>(() => new ServerCapability(NodeCapabilityType.FullNode));
        }
    }
}
