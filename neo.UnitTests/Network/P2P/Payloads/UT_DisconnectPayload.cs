using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_DisconnectPayload
    {
        [TestMethod]
        public void Size_Get()
        {
            var payload = DisconnectPayload.Create(DisconnectReason.DuplicateNonce, new byte[] { 0x01, 0x02 });
            payload.Size.Should().Be(4);
        }

        [TestMethod]
        public void Deserialize()
        {
            var payload = new DisconnectPayload();
            var hex = "03020102";
            using (MemoryStream ms = new MemoryStream(hex.HexToBytes(), false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    payload.Deserialize(reader);
                }
            }
            Assert.AreEqual(DisconnectReason.DuplicateNonce, payload.Reason);
            Assert.AreEqual(2, payload.Data.Length);
            Assert.AreEqual(0x01, payload.Data[0]);
            Assert.AreEqual(0x02, payload.Data[1]);
        }

        [TestMethod]
        public void Serialize()
        {
            var payload = DisconnectPayload.Create(DisconnectReason.DuplicateNonce, new byte[] { 0x01, 0x02 });
            payload.ToArray().ToHexString().Should().Be("03020102");
        }
    }
}
