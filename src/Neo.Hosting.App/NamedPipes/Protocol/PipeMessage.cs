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
using Neo.Hosting.App.Helpers;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal abstract class PipeMessage : IPipeMessage
    {
        public abstract ulong Magic { get; }

        protected abstract int Initialize(byte[] buffer);
        public abstract byte[] ToArray();

        public virtual async Task CopyFromAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var pipeReader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));

            Exception? error = null;

            try
            {
                var result = await pipeReader.ReadAsync(cancellationToken);

                if (result.IsCanceled)
                    return;

                var buffer = result.Buffer;
                var srcOffset = sizeof(ulong) + sizeof(uint) + 2;
                if (buffer.IsSingleSegment)
                {
                    byte[] src = [.. buffer.FirstSpan];

                    var dst = new byte[src.Length - srcOffset];
                    Buffer.BlockCopy(src, srcOffset, dst, 0, dst.Length);

                    var magic = BinaryUtility.ReadEncodedInteger(src, ulong.MaxValue, 0);
                    var checksum = BinaryUtility.ReadEncodedInteger(src, uint.MaxValue, sizeof(ulong) + 1);

                    if (magic != Magic)
                        throw new FormatException();
                    if (checksum != Crc32.Compute(dst))
                        throw new InvalidDataException();

                    Initialize(dst);
                }
                else
                {
                    byte[] src = [];

                    foreach (var segment in buffer)
                        src = [.. src, .. segment.Span];

                    var dst = new byte[src.Length - srcOffset];
                    Buffer.BlockCopy(src, srcOffset, src, 0, src.Length);

                    var magic = BinaryUtility.ReadEncodedInteger(src, ulong.MaxValue, 0);
                    var checksum = BinaryUtility.ReadEncodedInteger(src, uint.MaxValue, sizeof(ulong) + 1);

                    if (magic != Magic)
                        throw new FormatException();
                    if (checksum != Crc32.Compute(src))
                        throw new InvalidDataException();

                    Initialize(src);
                }

                pipeReader.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                    return;
            }
            catch (Exception ex)
            {
                error = ex;
                throw;
            }
            finally
            {
                pipeReader.Complete(error);
            }
        }

        public virtual async Task CopyToAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var buffer = ToArray();
            var checksum = Crc32.Compute(buffer);
            var pipeWriter = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            Exception? error = null;

            var dstOffset = sizeof(ulong) + sizeof(uint) + 2;
            var tmp = new byte[buffer.Length + dstOffset];

            BinaryUtility.WriteEncodedInteger(Magic, tmp, 0);
            BinaryUtility.WriteEncodedInteger(checksum, tmp, sizeof(ulong) + 1);
            Buffer.BlockCopy(buffer, 0, tmp, dstOffset, buffer.Length);

            try
            {
                await pipeWriter.WriteAsync(tmp, cancellationToken);
            }
            catch (Exception ex)
            {
                error = ex;
                throw;
            }
            finally
            {
                pipeWriter.Complete(error);
            }
        }
    }

    internal interface IPipeMessage
    {
        Task CopyToAsync(Stream stream, CancellationToken cancellationToken);
        Task CopyFromAsync(Stream stream, CancellationToken cancellationToken);
    }
}
