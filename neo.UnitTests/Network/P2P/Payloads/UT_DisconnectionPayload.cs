using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_DisconnectionPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var payload = DisconnectionPayload.Create(DisconnectionReason.DuplicateConnection, "test message", new byte[] { 0x01, 0x02 });
            payload.Size.Should().Be(17);
        }

        [TestMethod]
        public void Deserialize()
        {
            var payload = new DisconnectionPayload();
            var hex = "030c74657374206d657373616765020102";
            using (MemoryStream ms = new MemoryStream(hex.HexToBytes(), false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    payload.Deserialize(reader);
                }
            }
            Assert.AreEqual(DisconnectionReason.DuplicateConnection, payload.Reason);
            Assert.AreEqual("test message", payload.Message);
            Assert.AreEqual(2, payload.Data.Length);
            Assert.AreEqual(0x01, payload.Data[0]);
            Assert.AreEqual(0x02, payload.Data[1]);
        }

        [TestMethod]
        public void Serialize()
        {
            var payload = DisconnectionPayload.Create(DisconnectionReason.DuplicateConnection, "test message", new byte[] { 0x01, 0x02 });
            payload.ToArray().ToHexString().Should().Be("030c74657374206d657373616765020102");
        }
    }
}
