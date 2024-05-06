// Copyright (C) 2015-2024 The Neo Project.
//
// PipeException.cs file belongs to the neo project and is free
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
    internal sealed class PipeException : IPipeMessage
    {
        public const ulong Magic = 0x47534d524f525245ul; // ERRORMSG

        private static readonly int s_isEmptyOffset = 0;
        private static readonly int s_messageOffset = sizeof(bool);

        private int StackTraceOffset => sizeof(bool) + Message.GetSize();

        private byte[] _bytes = [sizeof(bool)];

        public required bool IsEmpty
        {
            get => string.IsNullOrEmpty(Message) && string.IsNullOrEmpty(StackTrace);
            set => _bytes.AsSpan().Write(value, s_isEmptyOffset);
        }

        public string Message
        {
            get => _bytes.TryCatch(t => t.AsSpan().ReadString(s_messageOffset), string.Empty);
            set
            {
                var stackTrace = StackTrace;
                Array.Resize(ref _bytes, sizeof(bool) + value.GetSize() + stackTrace.GetSize());
                var span = _bytes.AsSpan();
                span.Write(true, s_isEmptyOffset);
                span.Write(value, s_messageOffset);
                span.Write(stackTrace, StackTraceOffset);
            }
        }

        public string StackTrace
        {
            get => _bytes.TryCatch(t => t.AsSpan().ReadString(StackTraceOffset), string.Empty);
            set
            {
                if (string.IsNullOrEmpty(Message))
                    throw new InvalidOperationException($"Set {nameof(Message)} first.");

                Array.Resize(ref _bytes, sizeof(bool) + value.GetSize() + Message.GetSize());
                _bytes.AsSpan().Write(value, StackTraceOffset);
            }
        }

        public int Size =>
            sizeof(bool) +
            Message.GetSize() +
            StackTrace.GetSize();

        public PipeException()
        {
            _bytes = new byte[sizeof(int) * 2 + sizeof(bool)];
        }

        public Task CopyFromAsync(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            var magic = stream.Read<ulong>();
            if (magic != Magic)
                throw new InvalidDataException();

            var crc32 = stream.Read<uint>();

            var tmp = new byte[stream.Length - sizeof(ulong) - sizeof(uint)];
            stream.Read(tmp, 0, tmp.Length);

            if (crc32 != Crc32.Compute(tmp))
                throw new InvalidDataException();

            _bytes = tmp;

            return Task.CompletedTask;
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            var crc32 = Crc32.Compute(_bytes);

            stream.Write(Magic);
            stream.Write(crc32);
            stream.Write(_bytes[..].AsSpan());

            return stream.FlushAsync(cancellationToken);
        }

        public byte[] ToArray() =>
            _bytes[..];
    }
}
