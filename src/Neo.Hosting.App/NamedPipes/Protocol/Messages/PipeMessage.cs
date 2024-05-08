// Copyright (C) 2015-2024 The Neo Project.
//
// PipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Hosting.App.Buffers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace Neo.Hosting.App.NamedPipes.Protocol.Messages
{
    internal sealed class PipeMessage : IPipeMessage
    {
        public const ulong Magic = 0x314547415353454dul; // MESSAGE1
        public const byte Version = 0x01;

        private static readonly ConcurrentDictionary<PipeCommand, Type> _commandTypes = new();

        public PipeCommand Command { get; private set; }

        public IPipeMessage Payload { get; private set; }

        public PipeMessage()
        {
            Payload = new PipeEmptyPayload();
            Command = PipeCommand.Empty;
        }

        static PipeMessage()
        {
            foreach (var pipeProtocolField in typeof(PipeCommand).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = pipeProtocolField.GetCustomAttribute<PipeProtocolAttribute>();
                if (attr is null) continue;

                _ = _commandTypes.TryAdd((PipeCommand)pipeProtocolField.GetValue(null)!, attr.Type);
            }
        }

        public int Size =>
            sizeof(ulong) +
            sizeof(byte) +
            sizeof(uint) +
            sizeof(PipeCommand) +
            Payload.Size;

        public static PipeMessage Create(PipeCommand command, IPipeMessage payload) =>
            new()
            {
                Command = command,
                Payload = payload,
            };

        public static IPipeMessage? CreateMessage(PipeCommand command) =>
            _commandTypes.TryGetValue(command, out var t)
                ? (IPipeMessage?)Activator.CreateInstance(t)
                : null;

        public void FromArray(byte[] buffer)
        {
            var wrapper = new ByteArrayBuffer(buffer);

            var magic = wrapper.Read<ulong>();
            if (magic != Magic)
                throw new FormatException($"Magic number is incorrect: {magic}");

            var version = wrapper.Read<byte>();
            if (version != Version)
                throw new FormatException($"Version number is incorrect: {version}");

            var crc32 = wrapper.Read<uint>();
            var command = wrapper.Read<PipeCommand>();
            var payloadBytes = wrapper.ReadArray<byte>();

            if (crc32 != Crc32.Compute(payloadBytes))
                throw new InvalidDataException("CRC32 mismatch");

            Command = command;
            Payload = CreateMessage(command) ?? throw new InvalidDataException($"Unknown command: {command}");
            Payload.FromArray(payloadBytes);
        }

        public byte[] ToArray()
        {
            var wrapper = new ByteArrayBuffer();

            byte[] payloadBytes = Payload.ToArray();

            wrapper.Write(Magic);
            wrapper.Write(Version);
            wrapper.Write(Crc32.Compute(payloadBytes));
            wrapper.Write(Command);
            wrapper.Write(payloadBytes);

            return [.. wrapper];
        }
    }
}