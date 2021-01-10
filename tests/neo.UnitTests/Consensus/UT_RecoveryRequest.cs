using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Consensus;
using Neo.IO;

namespace Neo.UnitTests.Consensus
{
    [TestClass]
    public class UT_RecoveryRequest
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new RecoveryRequest() { Timestamp = 1, ViewNumber = 1 };
            test.Size.Should().Be(15);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new RecoveryRequest() { ViewNumber = 1, Timestamp = 123 };
            var clone = test.ToArray().AsSerializable<RecoveryRequest>();

            Assert.AreEqual(test.Timestamp, clone.Timestamp);
            Assert.AreEqual(test.Type, clone.Type);
            Assert.AreEqual(test.ViewNumber, clone.ViewNumber);
        }
    }
}
