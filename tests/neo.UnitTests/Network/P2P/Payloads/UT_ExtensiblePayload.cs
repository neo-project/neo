using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_ExtensiblePayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var test = new ExtensiblePayload()
            {
                Receiver = "123",
                Data = new byte[] { 1, 2, 3 },
                Witness = new Witness() { InvocationScript = new byte[] { 3, 5, 6 }, VerificationScript = new byte[0] }
            };
            test.Size.Should().Be(23);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new ExtensiblePayload()
            {
                Receiver = "123",
                MessageType = 123,
                ValidBlockEnd = 456,
                ValidBlockStart = 789,
                Data = new byte[] { 1, 2, 3 },
                Witness = new Witness() { InvocationScript = new byte[] { 3, 5, 6 }, VerificationScript = new byte[0] }
            };
            var clone = test.ToArray().AsSerializable<ExtensiblePayload>();

            Assert.AreEqual(test.Hash, clone.Hash);
            Assert.AreEqual(test.ValidBlockStart, clone.ValidBlockStart);
            Assert.AreEqual(test.ValidBlockEnd, clone.ValidBlockEnd);
            Assert.AreEqual(test.Receiver, clone.Receiver);
            Assert.AreEqual(test.MessageType, clone.MessageType);
        }
    }
}
