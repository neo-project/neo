using Akka.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_P2PMessage
    {
        [TestMethod]
        public void Serialize_Deserialize()
        {
            var payload = PingPayload.Create(uint.MaxValue);
            var msg = Message.Create(MessageCommand.ping, payload);
            var buffer = msg.ToArray();
            var copy = buffer.AsSerializable<Message>();
            var payloadCopy = copy.GetPayload<PingPayload>();

            copy.Command.Should().Be(MessageCommand.ping);
            copy.Flags.Should().Be(MessageFlags.None);

            payloadCopy.LastBlockIndex.Should().Be(payload.LastBlockIndex);
            payloadCopy.Nonce.Should().Be(payload.Nonce);
            payloadCopy.Timestamp.Should().Be(payload.Timestamp);
        }

        [TestMethod]
        public void Serialize_Deserialize_ByteString()
        {
            var payload = PingPayload.Create(uint.MaxValue);
            var msg = Message.Create(MessageCommand.ping, payload);
            var buffer = ByteString.CopyFrom(msg.ToArray());
            var length = Message.TryDeserialize(buffer, out var copy);

            length.Should().Be(buffer.Count);

            var payloadCopy = copy.GetPayload<PingPayload>();

            copy.Command.Should().Be(MessageCommand.ping);
            copy.Flags.Should().Be(MessageFlags.None);

            payloadCopy.LastBlockIndex.Should().Be(payload.LastBlockIndex);
            payloadCopy.Nonce.Should().Be(payload.Nonce);
            payloadCopy.Timestamp.Should().Be(payload.Timestamp);
        }

        [TestMethod]
        public void Compression()
        {
            var payload = new VersionPayload()
            {
                Relay = true,
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Port = 2,
                Services = VersionServices.NodeNetwork,
                StartHeight = 4,
                Timestamp = 5,
                Version = 6
            };
            var msg = Message.Create(MessageCommand.version, payload);
            var buffer = msg.ToArray();

            buffer.Length.Should().BeLessThan(80);

            var copy = buffer.AsSerializable<Message>();
            var payloadCopy = copy.GetPayload<VersionPayload>();

            copy.Command.Should().Be(MessageCommand.version);
            copy.Flags.Should().Be(MessageFlags.CompressedGzip);

            payloadCopy.Relay.Should().Be(payload.Relay);
            payloadCopy.UserAgent.Should().Be(payload.UserAgent);
            payloadCopy.Nonce.Should().Be(payload.Nonce);
            payloadCopy.Port.Should().Be(payload.Port);
            payloadCopy.Services.Should().Be(payload.Services);
            payloadCopy.StartHeight.Should().Be(payload.StartHeight);
            payloadCopy.Timestamp.Should().Be(payload.Timestamp);
            payloadCopy.Version.Should().Be(payload.Version);
        }
    }
}
