// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeTextWriter.cs file belongs to the neo project and is free
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
    internal class NamedPipeTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public new string NewLine { get; } = Environment.NewLine;

        private readonly string _pipeName;
        private readonly Task _listenForConnectionsTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private NamedPipeServerStream? _textWriterPipeStream;

        public NamedPipeTextWriter(
            string pipeName)
        {
            if (string.IsNullOrEmpty(pipeName)) throw new ArgumentNullException(nameof(pipeName));
            _pipeName = pipeName;
            _listenForConnectionsTask = InternalListenAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            if (_listenForConnectionsTask.IsCompleted == false)
            {
                _cancellationTokenSource.Cancel();
                await _listenForConnectionsTask;
            }
            _cancellationTokenSource.Dispose();
            _listenForConnectionsTask.Dispose();
            _textWriterPipeStream?.Dispose();
            await base.DisposeAsync();
        }

        public new void Dispose()
        {
            if (_listenForConnectionsTask.IsCompleted == false)
            {
                _cancellationTokenSource.Cancel();
                _listenForConnectionsTask.Wait();
            }
            _cancellationTokenSource.Dispose();
            _listenForConnectionsTask.Dispose();
            _textWriterPipeStream?.Dispose();
            base.Dispose();
        }

        public override void Flush() =>
            _textWriterPipeStream?.Flush();

        public override Task FlushAsync() =>
            _textWriterPipeStream?.FlushAsync() ?? Task.CompletedTask;

        public override void Close() =>
            _textWriterPipeStream?.Close();

        public override void Write(bool value) =>
            InternalWrite($"{value}");

        public override void Write(char value) =>
            InternalWrite($"{value}");

        public override void Write(char[] buffer) =>
            InternalWrite($"{buffer}");

        public override void Write(char[] buffer, int index, int count) =>
            InternalWrite($"{buffer[index..count]}");

        public override void Write(decimal value) =>
            InternalWrite($"{value}");

        public override void Write(double value) =>
            InternalWrite($"{value}");

        public override void Write(int value) =>
            InternalWrite($"{value}");

        public override void Write(long value) =>
            InternalWrite($"{value}");

        public override void Write(object value) =>
            InternalWrite($"{value}");

        public override void Write(ReadOnlySpan<char> buffer) =>
            InternalWrite(new string(buffer));

        public override void Write(float value) =>
            InternalWrite($"{value}");

        public override void Write(string value) =>
            InternalWrite($"{value}");

        public override void Write(string format, object arg0) =>
            InternalWrite(string.Format(FormatProvider, $"{format}", arg0));

        public override void Write(string format, object arg0, object arg1) =>
            InternalWrite(string.Format(FormatProvider, $"{format}", arg0, arg1));

        public override void Write(string format, object arg0, object arg1, object arg2) =>
            InternalWrite(string.Format(FormatProvider, $"{format}", arg0, arg1, arg2));

        public override void Write(string format, params object[] args) =>
            InternalWrite(string.Format(FormatProvider, $"{format}", args));

        public override void Write(uint value) =>
            InternalWrite($"{value}");

        public override void Write(ulong value) =>
            InternalWrite($"{value}");

        public override Task WriteAsync(char value) =>
            InternalWriteAsync($"{value}");

        public override Task WriteAsync(char[] buffer, int index, int count) =>
            InternalWriteAsync($"{buffer[index..count]}");

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default) =>
            InternalWriteAsync($"{buffer}", cancellationToken);

        public override Task WriteAsync(string value) =>
            InternalWriteAsync(value);

        public override void WriteLine() =>
            InternalWrite($"{NewLine}");

        public override void WriteLine(bool value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(char value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(char[] buffer) =>
            InternalWrite($"{buffer}{NewLine}");

        public override void WriteLine(char[] buffer, int index, int count) =>
            InternalWrite($"{buffer[index..count]}{NewLine}");

        public override void WriteLine(decimal value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(double value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(int value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(long value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(object value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(ReadOnlySpan<char> buffer) =>
            InternalWrite($"{new string(buffer)}{NewLine}");

        public override void WriteLine(float value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(string value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(string format, object arg0) =>
            InternalWrite(string.Format(FormatProvider, $"{format}{NewLine}", arg0));

        public override void WriteLine(string format, object arg0, object arg1) =>
            InternalWrite(string.Format(FormatProvider, $"{format}{NewLine}", arg0, arg1));

        public override void WriteLine(string format, object arg0, object arg1, object arg2) =>
            InternalWrite(string.Format(FormatProvider, $"{format}{NewLine}", arg0, arg1, arg2));

        public override void WriteLine(string format, params object[] args) =>
            InternalWrite(string.Format(FormatProvider, $"{format}{NewLine}", args));

        public override void WriteLine(uint value) =>
            InternalWrite($"{value}{NewLine}");

        public override void WriteLine(ulong value) =>
            InternalWrite($"{value}{NewLine}");

        public override Task WriteLineAsync() =>
            InternalWriteAsync($"{NewLine}");

        public override Task WriteLineAsync(char value) =>
            InternalWriteAsync($"{value}{NewLine}");

        public override Task WriteLineAsync(char[] buffer, int index, int count) =>
            InternalWriteAsync($"{buffer[index..count]}{NewLine}");

        public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default) =>
            InternalWriteAsync($"{buffer}{NewLine}", cancellationToken);

        public override Task WriteLineAsync(string value) =>
            InternalWriteAsync($"{value}{NewLine}");

        private Task InternalWriteAsync(string value, CancellationToken cancellationToken = default) =>
            _textWriterPipeStream?.IsConnected == true ?
            _textWriterPipeStream.WriteAsync(Encoding.GetBytes(value), cancellationToken).AsTask() :
            Task.CompletedTask;

        private void InternalWrite(string value)
        {
            if (_textWriterPipeStream?.IsConnected == true)
                _textWriterPipeStream.Write(Encoding.GetBytes(value));
        }

        private async Task InternalListenAsync()
        {
            _textWriterPipeStream ??= new(
                _pipeName, PipeDirection.Out,
                NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte,
                PipeOptions.CurrentUserOnly | PipeOptions.WriteThrough | PipeOptions.Asynchronous);

            await _textWriterPipeStream.WaitForConnectionAsync(_cancellationTokenSource.Token);

            while (_cancellationTokenSource.IsCancellationRequested == false && _textWriterPipeStream?.IsConnected == true)
                await Task.Delay(100);

            // Client disconnected; Restart
            if (_cancellationTokenSource.IsCancellationRequested == false)
                await InternalListenAsync();
        }
    }
}
