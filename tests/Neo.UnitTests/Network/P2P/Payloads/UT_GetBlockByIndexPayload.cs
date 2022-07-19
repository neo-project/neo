using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_GetBlockByIndexPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new GetBlockByIndexPayload() { Count = 5, IndexStart = 5 };
            test.Size.Should().Be(6);

            test = GetBlockByIndexPayload.Create(1, short.MaxValue);
            test.Size.Should().Be(6);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new GetBlockByIndexPayload() { Count = -1, IndexStart = int.MaxValue };
            var clone = test.ToArray().AsSerializable<GetBlockByIndexPayload>();

            Assert.AreEqual(test.Count, clone.Count);
            Assert.AreEqual(test.IndexStart, clone.IndexStart);

            test = new GetBlockByIndexPayload() { Count = -2, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockByIndexPayload>());

            test = new GetBlockByIndexPayload() { Count = 0, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockByIndexPayload>());

            test = new GetBlockByIndexPayload() { Count = HeadersPayload.MaxHeadersCount + 1, IndexStart = int.MaxValue };
            Assert.ThrowsException<FormatException>(() => test.ToArray().AsSerializable<GetBlockByIndexPayload>());
        }
    }
}
