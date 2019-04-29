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
        public void Serialize_Deserialize(bool checksum)
        {
            var payload = PingPayload.Create(uint.MaxValue);
            var msg = Message.Create(MessageCommand.Ping, payload, checksum);
            var buffer = msg.ToArray();
            var copy = buffer.AsSerializable<Message>();
            var payloadCopy = copy.GetPayload<PingPayload>();

            copy.CheckSum.Should().Be(msg.CheckSum);
            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().Be(msg.Flags);

            payloadCopy.LastBlockIndex.Should().Be(payload.LastBlockIndex);
            payloadCopy.Nonce.Should().Be(payload.Nonce);
            payloadCopy.Timestamp.Should().Be(payload.Timestamp);

            if (checksum)
            {
                copy.Flags.Should().HaveFlag(MessageFlags.Checksum);
                copy.CheckSum.Should().BeGreaterThan(0);
            }
            else
            {
                copy.Flags.Should().NotHaveFlag(MessageFlags.Checksum);
                copy.CheckSum.Should().Be(0);
            }
        }

        [TestMethod]
        public void Serialize_Deserialize_Checksum() => Serialize_Deserialize(true);

        [TestMethod]
        public void Serialize_Deserialize_WithoutChecksum() => Serialize_Deserialize(false);

        public void Serialize_Deserialize_ByteString(bool checksum)
        {
            var payload = PingPayload.Create(uint.MaxValue);
            var msg = Message.Create(MessageCommand.Ping, payload, checksum);
            var buffer = ByteString.CopyFrom(msg.ToArray());
            var length = Message.TryDeserialize(buffer, out var copy);


            var payloadCopy = copy.GetPayload<PingPayload>();

            copy.CheckSum.Should().Be(msg.CheckSum);
            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().Be(msg.Flags);

            payloadCopy.LastBlockIndex.Should().Be(payload.LastBlockIndex);
            payloadCopy.Nonce.Should().Be(payload.Nonce);
            payloadCopy.Timestamp.Should().Be(payload.Timestamp);

            if (checksum)
            {
                buffer.Count.Should().Be(length + 2);

                copy.Flags.Should().HaveFlag(MessageFlags.Checksum);
                copy.CheckSum.Should().BeGreaterThan(0);
            }
            else
            {
                buffer.Count.Should().Be(length);

                copy.Flags.Should().NotHaveFlag(MessageFlags.Checksum);
                copy.CheckSum.Should().Be(0);
            }
        }

        [TestMethod]
        public void Serialize_Deserialize_ByteString_Checksum() => Serialize_Deserialize_ByteString(true);

        [TestMethod]
        public void Serialize_Deserialize_ByteString_WithoutChecksum() => Serialize_Deserialize_ByteString(false);

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
            var payloadCopy = copy.GetPayload<VersionPayload>();

            copy.CheckSum.Should().Be(msg.CheckSum);
            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().Be(MessageFlags.CompressedGzip);

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