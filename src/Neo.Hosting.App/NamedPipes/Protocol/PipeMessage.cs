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

using Neo.Hosting.App.Extensions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal sealed class PipeMessage<TMessage> : IPipeMessage, IPipeException
        where TMessage : class, IPipeMessage, new()
    {
        public const ulong Magic = 0x4d45535341474531ul; // MESSAGE1

        public TMessage Payload { get; private set; }

        public PipeException Exception { get; private set; }

        public PipeMessage()
        {
            Payload = new TMessage();
            Exception = new() { IsEmpty = true };
        }

        public static PipeMessage<TMessage> Create(TMessage payload, Exception? exception = null) =>
            new()
            {
                Payload = payload,
                Exception = new()
                {
                    IsEmpty = exception is null,
                    Message = exception?.InnerException?.Message ?? exception?.Message,
                    StackTrace = exception?.InnerException?.StackTrace ?? exception?.StackTrace,
                },
            };

        public async Task CopyFromAsync(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            var magic = stream.Read<ulong>();
            if (magic != Magic)
                throw new InvalidDataException();

            await Payload.CopyFromAsync(stream);
            await Exception.CopyFromAsync(stream);
        }

        public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            stream.Write(Magic);

            await Payload.CopyToAsync(stream, cancellationToken);
            await Exception.CopyToAsync(stream, cancellationToken);
        }

        public byte[] ToArray()
        {
            using var ms = new MemoryStream();

            ms.Write(Magic);

            var task = Payload.CopyToAsync(ms);
            if (task.IsCompleted == false)
                task.RunSynchronously();

            task = Exception.CopyToAsync(ms);
            if (task.IsCompleted == false)
                task.RunSynchronously();

            return ms.ToArray();
        }
    }
}
