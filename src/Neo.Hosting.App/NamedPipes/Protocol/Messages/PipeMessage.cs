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
using Neo.Hosting.App.Extensions;
using System;
using System.IO;

namespace Neo.Hosting.App.NamedPipes.Protocol.Messages
{
    internal sealed class PipeMessage<TMessage> : IPipeMessage, IPipeException
        where TMessage : class, IPipeMessage, new()
    {
        public const ulong Magic = 0x314547415353454dul; // MESSAGE1
        public const int HeaderSize = sizeof(ulong) + sizeof(uint) + sizeof(byte);
        public const byte Version = 0x01;

        public TMessage Payload { get; private set; }

        public PipeException Exception { get; private set; }

        public PipeMessage()
        {
            Payload = new TMessage();
            Exception = new();
        }

        public int Size =>
            Payload.Size +
            Exception.Size;

        public static PipeMessage<TMessage> Create(TMessage payload, Exception? exception = null) =>
            new()
            {
                Payload = payload,
                Exception = new()
                {
                    Message = exception?.InnerException?.Message ?? exception?.Message ?? string.Empty,
                    StackTrace = exception?.InnerException?.StackTrace ?? exception?.StackTrace ?? string.Empty,
                },
            };

        public void CopyFrom(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            var magic = stream.Read<ulong>();
            if (magic != Magic)
                throw new InvalidDataException();

            _ = stream.Read<byte>();

            var crc = stream.Read<uint>();

            Payload.CopyFrom(stream);
            Exception.CopyFrom(stream);

            byte[] bytes = ToArray();
            if (crc != Crc32.Compute(bytes))
                throw new InvalidDataException();
        }

        public void CopyTo(Stream stream)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            var bytes = ToArray();

            stream.Write(Magic);
            stream.Write(Version);
            stream.Write(Crc32.Compute(bytes));
            stream.Write(bytes);
            stream.Flush();
        }

        public void CopyTo(byte[] buffer, int start = 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, start, nameof(start));

            var bytes = ToArray();
            var bytesSpan = bytes.AsSpan();
            var bufferSpan = buffer[start..];

            bytesSpan.CopyTo(bufferSpan);
        }

        public void CopyFrom(byte[] buffer, int start = 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, start, nameof(start));


            Payload.CopyFrom(buffer, start);
            Exception.CopyFrom(buffer, start + Payload.Size);
        }

        public byte[] ToArray() =>
        [
            .. Payload.ToArray(),
            .. Exception.ToArray()
        ];
    }
}
