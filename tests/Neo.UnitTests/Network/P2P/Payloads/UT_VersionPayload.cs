using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_VersionPayload
    {
        [TestMethod]
        public void SizeAndEndPoint_Get()
        {
            var test = new VersionPayload() { Capabilities = Array.Empty<NodeCapability>(), UserAgent = "neo3" };
            test.Size.Should().Be(22);

            test = VersionPayload.Create(123, 456, "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
            test.Size.Should().Be(25);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = VersionPayload.Create(123, 456, "neo3", new NodeCapability[] { new ServerCapability(NodeCapabilityType.TcpServer, 22) });
            var clone = test.ToArray().AsSerializable<VersionPayload>();

            CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());
            Assert.AreEqual(test.UserAgent, clone.UserAgent);
            Assert.AreEqual(test.Nonce, clone.Nonce);
            Assert.AreEqual(test.Timestamp, clone.Timestamp);
            CollectionAssert.AreEqual(test.Capabilities.ToByteArray(), clone.Capabilities.ToByteArray());

            Assert.ThrowsException<FormatException>(() => VersionPayload.Create(123, 456, "neo3",
                new NodeCapability[] {
                    new ServerCapability(NodeCapabilityType.TcpServer, 22) ,
                    new ServerCapability(NodeCapabilityType.TcpServer, 22)
                }).ToArray().AsSerializable<VersionPayload>());
        }
    }
}
