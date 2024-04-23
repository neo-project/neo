// Copyright (C) 2015-2024 The Neo Project.
//
// IPipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
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

        protected abstract void Initialize(byte[] buffer);
        public abstract byte[] ToArray();

        public virtual async Task CopyFromAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanRead == false) return;

            var pipeReader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));

            Exception? error = null;

            try
            {
                var result = await pipeReader.ReadAsync(cancellationToken);

                if (result.IsCanceled)
                    return;

                var buffer = result.Buffer;
                if (buffer.IsSingleSegment)
                    Initialize([.. buffer.FirstSpan]);
                else
                {
                    byte[] tmp = [];

                    foreach (var segment in buffer)
                        tmp = [.. tmp, .. segment.Span];

                    Initialize(tmp);
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
            if (stream.CanWrite == false) return;

            var buffer = ToArray();
            var checksum = Crc32.Compute(buffer);
            var pipeWriter = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            Exception? error = null;

            try
            {
                await pipeWriter.WriteAsync(BitConverter.GetBytes(Magic), cancellationToken);
                await pipeWriter.WriteAsync(BitConverter.GetBytes(checksum), cancellationToken);
                await pipeWriter.WriteAsync(buffer, cancellationToken);
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
