// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Message.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
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

            Assert.AreEqual(msg.Command, copy.Command);
            Assert.AreEqual(msg.Flags, copy.Flags);
            Assert.AreEqual(payload.Size + 3, msg.Size);

            Assert.AreEqual(payload.LastBlockIndex, payloadCopy.LastBlockIndex);
            Assert.AreEqual(payload.Nonce, payloadCopy.Nonce);
            Assert.AreEqual(payload.Timestamp, payloadCopy.Timestamp);
        }

        [TestMethod]
        public void Serialize_Deserialize_WithoutPayload()
        {
            var msg = Message.Create(MessageCommand.GetAddr);
            var buffer = msg.ToArray();
            var copy = buffer.AsSerializable<Message>();

            Assert.AreEqual(msg.Command, copy.Command);
            Assert.AreEqual(msg.Flags, copy.Flags);
            Assert.IsNull(copy.Payload);
        }

        [TestMethod]
        public void ToArray()
        {
            var payload = PingPayload.Create(uint.MaxValue);
            var msg = Message.Create(MessageCommand.Ping, payload);
            _ = msg.ToArray();

            Assert.AreEqual(payload.Size + 3, msg.Size);
        }

        [TestMethod]
        public void Serialize_Deserialize_ByteString()
        {
            var payload = PingPayload.Create(uint.MaxValue);
            var msg = Message.Create(MessageCommand.Ping, payload);
            var buffer = ByteString.CopyFrom(msg.ToArray());
            var length = Message.TryDeserialize(buffer, out var copy);

            var payloadCopy = (PingPayload)copy.Payload;

            Assert.AreEqual(msg.Command, copy.Command);
            Assert.AreEqual(msg.Flags, copy.Flags);

            Assert.AreEqual(payload.LastBlockIndex, payloadCopy.LastBlockIndex);
            Assert.AreEqual(payload.Nonce, payloadCopy.Nonce);
            Assert.AreEqual(payload.Timestamp, payloadCopy.Timestamp);

            Assert.AreEqual(length, buffer.Count);
        }

        [TestMethod]
        public void ToArray_WithoutPayload()
        {
            var msg = Message.Create(MessageCommand.GetAddr);
            _ = msg.ToArray();
        }

        [TestMethod]
        public void Serialize_Deserialize_WithoutPayload_ByteString()
        {
            var msg = Message.Create(MessageCommand.GetAddr);
            var buffer = ByteString.CopyFrom(msg.ToArray());
            var length = Message.TryDeserialize(buffer, out var copy);

            Assert.AreEqual(msg.Command, copy.Command);
            Assert.AreEqual(msg.Flags, copy.Flags);
            Assert.IsNull(copy.Payload);

            Assert.AreEqual(length, buffer.Count);
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
                Witnesses = new Witness[] { new Witness() { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() } },
            };

            var msg = Message.Create(MessageCommand.Transaction, payload);
            var buffer = msg.ToArray();

            Assert.AreEqual(56, buffer.Length);

            byte[] script = new byte[100];
            Array.Fill(script, (byte)OpCode.PUSH2);
            payload.Script = script;
            msg = Message.Create(MessageCommand.Transaction, payload);
            buffer = msg.ToArray();

            Assert.AreEqual(30, buffer.Length);
            Assert.IsTrue(msg.Flags.HasFlag(MessageFlags.Compressed));

            _ = Message.TryDeserialize(ByteString.CopyFrom(msg.ToArray()), out var copy);
            Assert.IsNotNull(copy);

            Assert.IsTrue(copy.Flags.HasFlag(MessageFlags.Compressed));
        }
    }
}
