using Akka.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using System;
using System.Linq;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_Message
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
            msg.Size.Should().Be(payload.Size + 3);

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
        public void MultipleSizes()
        {
            var msg = Message.Create(MessageCommand.GetAddr);
            var buffer = msg.ToArray();

            var length = Message.TryDeserialize(ByteString.Empty, out var copy);
            Assert.AreEqual(0, length);
            Assert.IsNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer), out copy);
            Assert.AreEqual(buffer.Length, length);
            Assert.IsNotNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFD }).ToArray()), out copy);
            Assert.AreEqual(0, length);
            Assert.IsNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFD, buffer[2], 0x00 }).Concat(buffer.Skip(3)).ToArray()), out copy);
            Assert.AreEqual(buffer.Length + 2, length);
            Assert.IsNotNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFD, 0x01, 0x00 }).Concat(buffer.Skip(3)).ToArray()), out copy);
            Assert.AreEqual(0, length);
            Assert.IsNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFE }).Concat(buffer.Skip(3)).ToArray()), out copy);
            Assert.AreEqual(0, length);
            Assert.IsNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFE, buffer[2], 0x00, 0x00, 0x00 }).Concat(buffer.Skip(3)).ToArray()), out copy);
            Assert.AreEqual(buffer.Length + 4, length);
            Assert.IsNotNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFF }).Concat(buffer.Skip(3)).ToArray()), out copy);
            Assert.AreEqual(0, length);
            Assert.IsNull(copy);

            length = Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFF, buffer[2], 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }).Concat(buffer.Skip(3)).ToArray()), out copy);
            Assert.AreEqual(buffer.Length + 8, length);
            Assert.IsNotNull(copy);

            // Big message

            Assert.ThrowsException<FormatException>(() => Message.TryDeserialize(ByteString.CopyFrom(buffer.Take(2).Concat(new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }).Concat(buffer.Skip(3)).ToArray()), out copy));
        }

        [TestMethod]
        public void Compression()
        {
            var payload = new Transaction()
            {
                Nonce = 1,
                Version = 0,
                Attributes = Array.Empty<TransactionAttribute>(),
                Script = new byte[] { (byte)OpCode.PUSH1 },
                Signers = new Signer[] { new Signer() { Account = UInt160.Zero } },
                Witnesses = new Witness[] { new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = new byte[0] } },
            };

            var msg = Message.Create(MessageCommand.Transaction, payload);
            var buffer = msg.ToArray();

            buffer.Length.Should().Be(56);

            payload.Script = new byte[100];
            for (int i = 0; i < payload.Script.Length; i++) payload.Script[i] = (byte)OpCode.PUSH2;
            msg = Message.Create(MessageCommand.Transaction, payload);
            buffer = msg.ToArray();

            buffer.Length.Should().Be(30);

            var copy = buffer.AsSerializable<Message>();
            var payloadCopy = (Transaction)copy.Payload;

            copy.Command.Should().Be(msg.Command);
            copy.Flags.Should().HaveFlag(MessageFlags.Compressed);

            payloadCopy.ToArray().ToHexString().Should().Be(payload.ToArray().ToHexString());
        }
    }
}
