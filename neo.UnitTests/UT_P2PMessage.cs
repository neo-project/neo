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
            var msg = Message.Create(MessageCommand.Ping, payload);
            var buffer = msg.ToArray();
            var copy = buffer.AsSerializable<Message>();
            var payloadCopy = (PingPayload)copy.Payload;

            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().Be(msg.Flags);

            payloadCopy.LastBlockIndex.Should().Be(payload.LastBlockIndex);
            payloadCopy.Nonce.Should().Be(payload.Nonce);
            payloadCopy.Timestamp.Should().Be(payload.Timestamp);
        }

        [TestMethod]
        public void Serialize_Deserialize_ByteString()
        {
            var payload = PingPayload.Create(uint.MaxValue);
            var msg = Message.Create(MessageCommand.Ping, payload);
            var buffer = ByteString.CopyFrom(msg.ToArray());
            var length = Message.TryDeserialize(buffer, out var copy);

            var payloadCopy = (PingPayload)copy.Payload;

            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().Be(msg.Flags);

            payloadCopy.LastBlockIndex.Should().Be(payload.LastBlockIndex);
            payloadCopy.Nonce.Should().Be(payload.Nonce);
            payloadCopy.Timestamp.Should().Be(payload.Timestamp);

            buffer.Count.Should().Be(length);
        }

        [TestMethod]
        public void Serialize_Deserialize_WithoutPayload()
        {
            var msg = Message.Create(MessageCommand.GetAddr);
            var buffer = msg.ToArray();
            var copy = buffer.AsSerializable<Message>();

            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().Be(msg.Flags);
            copy.Payload.Should().Be(null);
        }

        [TestMethod]
        public void Serialize_Deserialize_WithoutPayload_ByteString()
        {
            var msg = Message.Create(MessageCommand.GetAddr);
            var buffer = ByteString.CopyFrom(msg.ToArray());
            var length = Message.TryDeserialize(buffer, out var copy);

            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().Be(msg.Flags);
            copy.Payload.Should().Be(null);

            buffer.Count.Should().Be(length);
        }


        [TestMethod]
        public void Compression()
        {
            var payload = new VersionPayload()
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Port = 2,
                Services = VersionServices.FullNode,
                StartHeight = 4,
                Timestamp = 5,
                Version = 6
            };
            var msg = Message.Create(MessageCommand.Version, payload);
            var buffer = msg.ToArray();

            buffer.Length.Should().BeLessThan(80);

            var copy = buffer.AsSerializable<Message>();
            var payloadCopy = (VersionPayload)copy.Payload;

            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().HaveFlag(MessageFlags.Compressed);

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