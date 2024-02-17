// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeTextReader.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.IO.Pipes
{
    internal class NamedPipeTextReader : TextReader
    {
        public string NewLine { get; } = Environment.NewLine;

        private const int ReadBufferSize = 1024;

        private readonly string _pipeName;
        private readonly Task _listenForConnectionsTask;
        private readonly CancellationTokenSource _streamReadTokenSource;
        private readonly CancellationTokenSource _listenForConnectionsTokenSource = new();

        private NamedPipeServerStream? _textReaderPipeStream;

        public NamedPipeTextReader(
            string pipeName)
        {
            if (string.IsNullOrEmpty(pipeName)) throw new ArgumentNullException(nameof(pipeName));
            _pipeName = pipeName;
            _listenForConnectionsTask = InternalListenAsync();
            _streamReadTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_listenForConnectionsTokenSource.Token);
        }

        public override void Close() =>
            _textReaderPipeStream?.Close();

        public override int Peek() =>
            throw new InvalidOperationException();

        public override int Read()
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var value = _textReaderPipeStream.ReadByte();
                return Convert.ToChar(value);
            }
            return -1;
        }

        public override int Read(char[] buffer, int index, int count) =>
            ReadBlock(buffer, index, count);

        public override int Read(Span<char> buffer) =>
            ReadBlock(buffer);

        public override Task<int> ReadAsync(char[] buffer, int index, int count) =>
            ReadBlockAsync(buffer, index, count);

        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default) =>
            ReadBlockAsync(buffer, cancellationToken);

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var readBufferBytes = new byte[count];
                var bytesReadCount = _textReaderPipeStream.Read(readBufferBytes.AsSpan());
                Encoding.UTF8.GetChars(readBufferBytes, 0, bytesReadCount);
                return bytesReadCount;
            }
            return 0;
        }

        public override int ReadBlock(Span<char> buffer)
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var readBufferBytes = new byte[buffer.Length];
                var bytesReadCount = _textReaderPipeStream.Read(readBufferBytes.AsSpan());
                Encoding.UTF8.GetChars(readBufferBytes, 0, bytesReadCount)
                    .AsSpan()
                    .CopyTo(buffer);
                return bytesReadCount;
            }
            return 0;
        }

        public override async Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var readBufferBytes = new byte[count];
                var bytesReadCount = await _textReaderPipeStream.ReadAsync(readBufferBytes.AsMemory(), _streamReadTokenSource.Token);
                Encoding.UTF8.GetChars(readBufferBytes, 0, bytesReadCount)
                    .AsMemory()
                    .CopyTo(buffer);
                return bytesReadCount;
            }
            return 0;
        }

        public override async ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var cancelToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _streamReadTokenSource.Token);
                var readBufferBytes = new byte[buffer.Length];
                var bytesReadCount = await _textReaderPipeStream.ReadAsync(readBufferBytes.AsMemory(), cancelToken.Token);
                Encoding.UTF8.GetChars(readBufferBytes, 0, bytesReadCount)
                    .AsMemory()
                    .CopyTo(buffer);
                return bytesReadCount;
            }
            return 0;
        }

        public override string ReadLine()
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var readBufferBytes = new byte[NewLine.Length];
                var output = string.Empty;
                int bytesReadCount;
                do
                {
                    bytesReadCount = _textReaderPipeStream.Read(readBufferBytes.AsSpan());
                    output += Encoding.UTF8.GetString(readBufferBytes, 0, bytesReadCount);
                    if (output.EndsWith(NewLine)) break;
                } while (_streamReadTokenSource.IsCancellationRequested == false && bytesReadCount != 0);
                return output;
            }
            return string.Empty;
        }

        public override async Task<string?> ReadLineAsync()
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var readBufferBytes = new byte[NewLine.Length];
                var output = string.Empty;
                int bytesReadCount;
                do
                {
                    bytesReadCount = await _textReaderPipeStream.ReadAsync(readBufferBytes.AsMemory());
                    output += Encoding.UTF8.GetString(readBufferBytes, 0, bytesReadCount);
                    if (output.EndsWith(NewLine)) break;
                } while (_streamReadTokenSource.IsCancellationRequested == false && bytesReadCount != 0);
                return output;
            }
            return string.Empty;
        }

        public override string ReadToEnd()
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var readBufferBytes = new byte[ReadBufferSize];
                var output = string.Empty;
                int bytesReadCount;
                do
                {
                    bytesReadCount = _textReaderPipeStream.Read(readBufferBytes.AsSpan());
                    output += Encoding.UTF8.GetString(readBufferBytes, 0, bytesReadCount);
                } while (_streamReadTokenSource.IsCancellationRequested == false && bytesReadCount != 0);
                return output;
            }
            return string.Empty;
        }

        public override async Task<string> ReadToEndAsync()
        {
            if (_textReaderPipeStream?.IsConnected == true)
            {
                var readBufferBytes = new byte[ReadBufferSize];
                var output = string.Empty;
                int bytesReadCount;
                do
                {
                    bytesReadCount = await _textReaderPipeStream.ReadAsync(readBufferBytes.AsMemory(), _streamReadTokenSource.Token);
                    output += Encoding.UTF8.GetString(readBufferBytes, 0, bytesReadCount);
                } while (_streamReadTokenSource.IsCancellationRequested == false && bytesReadCount != 0);
                return output;
            }
            return string.Empty;
        }

        private async Task InternalListenAsync()
        {
            _textReaderPipeStream ??= new(
                _pipeName, PipeDirection.In,
                NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
                PipeOptions.CurrentUserOnly | PipeOptions.WriteThrough | PipeOptions.Asynchronous);

            await _textReaderPipeStream.WaitForConnectionAsync(_listenForConnectionsTokenSource.Token);

            while (_listenForConnectionsTokenSource.IsCancellationRequested == false && _textReaderPipeStream?.IsConnected == true)
                await Task.Delay(100);

            // Client disconnected; Restart
            if (_listenForConnectionsTokenSource.IsCancellationRequested == false)
                await InternalListenAsync();
        }
    }
}
