using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_FilterAddPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new FilterAddPayload() { Data = new byte[0] };
            test.Size.Should().Be(1);

            test = new FilterAddPayload() { Data = new byte[] { 1, 2, 3 } };
            test.Size.Should().Be(4);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new FilterAddPayload() { Data = new byte[] { 1, 2, 3 } };
            var clone = test.ToArray().AsSerializable<FilterAddPayload>();

            Assert.IsTrue(test.Data.Span.SequenceEqual(clone.Data.Span));
        }
    }
}
