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
using System.Threading;
using System.Threading.Tasks;

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

        public async Task CopyFromAsync(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            var magic = stream.Read<ulong>();
            if (magic != Magic)
                throw new InvalidDataException();

            _ = stream.Read<byte>();

            var crc = stream.Read<uint>();

            await Payload.CopyFromAsync(stream);
            await Exception.CopyFromAsync(stream);

            byte[] bytes = ToArray();
            if (crc != Crc32.Compute(bytes))
                throw new InvalidDataException();
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            byte[] bytes = ToArray();

            stream.Write(Magic);
            stream.Write(Version);
            stream.Write(Crc32.Compute(bytes));
            stream.Write(bytes);

            return Task.CompletedTask;
        }

        public byte[] ToArray() =>
        [
            .. Payload.ToArray(),
            .. Exception.ToArray()
        ];
    }
}
