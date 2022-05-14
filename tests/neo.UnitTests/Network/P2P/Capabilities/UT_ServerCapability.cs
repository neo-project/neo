using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Capabilities;
using System;

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

            var br = new MemoryReader(buffer);
            var clone = (ServerCapability)NodeCapability.DeserializeFrom(ref br);

            Assert.AreEqual(test.Port, clone.Port);
            Assert.AreEqual(test.Type, clone.Type);

            clone = new ServerCapability(NodeCapabilityType.WsServer, 123);
            br = new MemoryReader(buffer);
            ((ISerializable)clone).Deserialize(ref br);

            Assert.AreEqual(test.Port, clone.Port);
            Assert.AreEqual(test.Type, clone.Type);

            clone = new ServerCapability(NodeCapabilityType.TcpServer, 123);

            br = new MemoryReader(buffer);
            try
            {
                ((ISerializable)clone).Deserialize(ref br);
                Assert.Fail();
            }
            catch (FormatException) { }
            try
            {
                _ = new ServerCapability(NodeCapabilityType.FullNode);
                Assert.Fail();
            }
            catch (ArgumentException) { }

            // Wrog type
            buffer[0] = 0xFF;
            br = new MemoryReader(buffer);
            try
            {
                NodeCapability.DeserializeFrom(ref br);
                Assert.Fail();
            }
            catch (FormatException) { }
        }
    }
}
