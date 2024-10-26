// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Buffers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Hashing;
using System.Reflection;

namespace Neo.IO.Pipes.Protocols
{
    internal class NamedPipeMessage : INamedPipeMessage
    {
        public const ulong Magic = 0x314547415353454dul; // MESSAGE1
        public const byte Version = 0x01;

        public NamedPipeCommand Command { get; set; }
        public INamedPipeMessage? Payload { get; set; }
        public int PayloadSize => Payload?.Size ?? 0;

        private static readonly Dictionary<NamedPipeCommand, Type> s_commandTypes = new();

        private uint _checksum;
        private int _payloadSize;

        public int Size =>
            sizeof(ulong) +             // Magic
            sizeof(byte) +              // Version
            sizeof(uint) +              // CRC32
            sizeof(NamedPipeCommand) +  // Command
            sizeof(int) +               // Payload size
            (Payload?.Size ?? 0);       // Payload

        static NamedPipeMessage()
        {
            foreach (var pipeProtocolField in typeof(NamedPipeCommand).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attr = pipeProtocolField.GetCustomAttribute<NamedPipeCommandAttribute>();
                if (attr is null) continue;

                _ = s_commandTypes.TryAdd((NamedPipeCommand)pipeProtocolField.GetValue(null)!, attr.Type);
            }
        }

        public void FromStream(Stream stream)
        {
            using var reader = new MemoryBuffer(stream);
            FromMemoryBuffer(reader);
        }

        public void FromMemoryBuffer(MemoryBuffer reader)
        {
            var magic = reader.Read<ulong>();
            if (magic != Magic)
                throw new InvalidDataException("Invalid magic number");

            var version = reader.Read<byte>();
            if (version != Version)
                throw new InvalidDataException("Invalid version number");

            _checksum = reader.Read<uint>();
            Command = reader.Read<NamedPipeCommand>();
            _payloadSize = reader.Read<int>();

            var payload = CreateEmptyPayload(Command) ?? throw new InvalidDataException($"Unknown command: {Command}");
            payload.FromMemoryBuffer(reader);

            var payloadBytes = payload.ToByteArray();
            if (payloadBytes.Length != _payloadSize)
                throw new InvalidDataException("Invalid payload size");

            if (_checksum != Crc32.HashToUInt32(payloadBytes))
                throw new InvalidDataException("Invalid checksum");

            Payload = payload;
        }

        public byte[] ToByteArray()
        {
            if (Payload is null)
                throw new InvalidDataException("Payload is not set");

            using var ms = new MemoryStream();
            using var writer = new MemoryBuffer(ms);
            var bytes = Payload.ToByteArray();

            _checksum = Crc32.HashToUInt32(bytes);

            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(_checksum);
            writer.Write(Command);
            writer.Write(bytes.Length);
            writer.WriteRaw(bytes);

            return ms.ToArray();
        }

        public static bool TryDeserialize(Stream stream, [NotNullWhen(true)] out NamedPipeMessage? message)
        {
            message = null;
            try
            {
                var m = new NamedPipeMessage();
                m.FromStream(stream);
                message = m;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static INamedPipeMessage? CreateEmptyPayload(NamedPipeCommand command) =>
            s_commandTypes.TryGetValue(command, out var t)
                ? Activator.CreateInstance(t) as INamedPipeMessage
                : null;
    }
}
