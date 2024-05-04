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

using Neo.Hosting.App.Extensions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal sealed class PipeException : IPipeMessage
    {
        public const ulong Magic = 0x4552524f524d5347ul; // ERRORMSG

        public required bool IsEmpty { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }

        public Task CopyFromAsync(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            var magic = stream.Read<ulong>();
            if (magic != Magic)
                throw new InvalidDataException();

            IsEmpty = stream.Read<bool>();

            if (IsEmpty)
                return Task.CompletedTask;

            Message = stream.ReadString();
            StackTrace = stream.ReadString();

            if (string.IsNullOrEmpty(StackTrace))
                StackTrace = null;

            return Task.CompletedTask;
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            CopyToStream(stream);
            return stream.FlushAsync(cancellationToken);
        }

        public byte[] ToArray()
        {
            using var ms = new MemoryStream();
            CopyToStream(ms);
            return ms.ToArray();
        }

        private void CopyToStream(Stream stream)
        {
            stream.Write(Magic);

            if (IsEmpty)
                stream.Write(true);
            else
            {
                stream.Write(false);
                stream.Write(Message!);
                stream.Write(StackTrace ?? string.Empty);
            }
        }
    }
}
