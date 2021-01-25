using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_GetBlockDataPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new GetBlockDataPayload() { Count = 5, IndexStart = 5 };
            test.Size.Should().Be(6);

            test = GetBlockDataPayload.Create(1, ushort.MaxValue);
            test.Size.Should().Be(6);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new GetBlockDataPayload() { Count = 1, IndexStart = int.MaxValue };
            var clone = test.ToArray().AsSerializable<GetBlockDataPayload>();

            Assert.AreEqual(test.Count, clone.Count);
            Assert.AreEqual(test.IndexStart, clone.IndexStart);

            test = new GetBlockDataPayload() { Count = 0, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockDataPayload>());

            test = new GetBlockDataPayload() { Count = 501, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockDataPayload>());
        }
    }
}
