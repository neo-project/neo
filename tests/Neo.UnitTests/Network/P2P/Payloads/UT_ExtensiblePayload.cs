using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;

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
                Sender = Array.Empty<byte>().ToScriptHash(),
                Category = "123",
                Data = new byte[] { 1, 2, 3 },
                Witness = new Witness() { InvocationScript = new byte[] { 3, 5, 6 }, VerificationScript = Array.Empty<byte>() }
            };
            test.Size.Should().Be(42);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var test = new ExtensiblePayload()
            {
                Category = "123",
                ValidBlockStart = 456,
                ValidBlockEnd = 789,
                Sender = Array.Empty<byte>().ToScriptHash(),
                Data = new byte[] { 1, 2, 3 },
                Witness = new Witness() { InvocationScript = new byte[] { (byte)OpCode.PUSH1, (byte)OpCode.PUSH2, (byte)OpCode.PUSH3 }, VerificationScript = Array.Empty<byte>() }
            };
            var clone = test.ToArray().AsSerializable<ExtensiblePayload>();

            Assert.AreEqual(test.Sender, clone.Witness.ScriptHash);
            Assert.AreEqual(test.Hash, clone.Hash);
            Assert.AreEqual(test.ValidBlockStart, clone.ValidBlockStart);
            Assert.AreEqual(test.ValidBlockEnd, clone.ValidBlockEnd);
            Assert.AreEqual(test.Category, clone.Category);
        }
    }
}
