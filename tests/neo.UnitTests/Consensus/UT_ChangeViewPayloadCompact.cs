using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Consensus;
using Neo.IO;

namespace Neo.UnitTests.Consensus
{
    [TestClass]
    public class UT_ChangeViewPayloadCompact
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new RecoveryMessage.ChangeViewPayloadCompact() { Timestamp = 1, ValidatorIndex = 1, InvocationScript = new byte[0], OriginalViewNumber = 1 };
            ((ISerializable)test).Size.Should().Be(11);

            test = new RecoveryMessage.ChangeViewPayloadCompact() { Timestamp = 1, ValidatorIndex = 1, InvocationScript = new byte[1024], OriginalViewNumber = 1 };
            ((ISerializable)test).Size.Should().Be(1037);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new RecoveryMessage.ChangeViewPayloadCompact() { Timestamp = 1, ValidatorIndex = 2, InvocationScript = new byte[] { 1, 2, 3 }, OriginalViewNumber = 3 };
            var clone = test.ToArray().AsSerializable<RecoveryMessage.ChangeViewPayloadCompact>();

            Assert.AreEqual(test.Timestamp, clone.Timestamp);
            Assert.AreEqual(test.ValidatorIndex, clone.ValidatorIndex);
            Assert.AreEqual(test.OriginalViewNumber, clone.OriginalViewNumber);
            CollectionAssert.AreEqual(test.InvocationScript, clone.InvocationScript);
        }
    }
}
