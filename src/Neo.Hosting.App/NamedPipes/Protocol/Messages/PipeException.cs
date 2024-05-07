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
using System;
using System.IO;

namespace Neo.Hosting.App.NamedPipes.Protocol.Messages
{
    internal sealed class PipeException : IPipeMessage
    {
        private byte[] _bytes = [];

        public string Message
        {
            get => _bytes.TryCatch(t => t.AsSpan().ReadString(), string.Empty);
            set
            {
                var stackTrace = StackTrace;
                Array.Resize(ref _bytes, value.GetStructSize() + stackTrace.GetStructSize());

                var span = _bytes.AsSpan();
                span.Write(value);
                span.Write(stackTrace, value.GetStructSize());
            }
        }

        public string StackTrace
        {
            get => _bytes.TryCatch(t => t.AsSpan().ReadString(Message.GetStructSize()), string.Empty);
            set
            {
                var offset = Message.GetStructSize();

                Array.Resize(ref _bytes, value.GetStructSize() + offset);
                _bytes.AsSpan().Write(value, offset);
            }
        }

        public PipeException()
        {
            Message = string.Empty;
            StackTrace = string.Empty;
        }

        public bool IsEmpty =>
            string.IsNullOrEmpty(Message) &&
            string.IsNullOrEmpty(StackTrace);

        public int Size =>
            Message.GetStructSize() +
            StackTrace.GetStructSize();

        public void CopyFrom(Stream stream)
        {
            if (stream.CanRead == false)
                throw new IOException();

            Message = stream.ReadString();
            StackTrace = stream.ReadString();
        }

        public void CopyTo(Stream stream)
        {
            if (stream.CanWrite == false)
                throw new IOException();

            var bytes = ToArray();
            stream.Write(bytes);

            stream.Flush();
        }

        public void CopyTo(byte[] buffer, int start = 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, start, nameof(start));

            var bytes = ToArray();
            var bytesSpan = bytes.AsSpan();
            var bufferSpan = buffer.AsSpan(start);

            bytesSpan.CopyTo(bufferSpan);
        }

        public void CopyFrom(byte[] buffer, int start = 0)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, start, nameof(start));

            var bufferSpan = buffer.AsSpan(start);

            Message = bufferSpan.ReadString();
            StackTrace = bufferSpan.ReadString(Message.GetStructSize());
        }

        public byte[] ToArray() =>
            _bytes[..];
    }
}
