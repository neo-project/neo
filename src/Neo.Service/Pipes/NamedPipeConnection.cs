// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeConnection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Pipes;
using System;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Neo.CLI.Pipes
{
    internal class NamedPipeConnection : IAsyncDisposable
    {
        internal const int MinAllocBufferSize = 4096;

        internal IDuplexPipe Application { get; private set; }
        public IDuplexPipe Transport { get; private set; }

        private readonly NamedPipeListener _namedPipeListener;
        private readonly NamedPipeServerStream _namedPipeStream;

        private readonly IDuplexPipe _originalTransport;

        private Task _receivingTask = Task.CompletedTask;
        private Task _sendingTask = Task.CompletedTask;

        public NamedPipeConnection(
            NamedPipeListener listener,
            NamedPipeServerStream serverStream,
            PipeOptions inputOptions,
            PipeOptions outputOptions)
        {
            _namedPipeListener = listener;
            _namedPipeStream = serverStream;

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            Transport = _originalTransport = pair.Transport;
            Application = pair.Application;
        }

        public async ValueTask DisposeAsync()
        {
            _originalTransport.Input.Complete();
            _originalTransport.Output.Complete();

            try
            {
                await _receivingTask;
                await _sendingTask;
            }
            catch (Exception)
            {
                _namedPipeStream.Dispose();
            }

            if (_namedPipeStream.IsConnected)
                _namedPipeStream.Dispose();
            else
                _namedPipeListener.ReturnStream(_namedPipeStream);
        }

        internal void Start()
        {
            try
            {
                _receivingTask = DoReceiveAsync();
                _sendingTask = DoSendAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task DoReceiveAsync()
        {
            Exception? error = null;

            try
            {
                var input = Application.Output;

                while (true)
                {
                    var buffer = input.GetMemory(MinAllocBufferSize);
                    var bytesReceived = await _namedPipeStream.ReadAsync(buffer);

                    if (bytesReceived == 0)
                        break;

                    input.Advance(bytesReceived);

                    var result = await input.FlushAsync();

                    if (result.IsCompleted || result.IsCanceled)
                        break;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                Application.Output.Complete(error);
            }
        }

        private async Task DoSendAsync()
        {
            Exception? unexpectedError = null;

            try
            {
                while (true)
                {
                    var output = Application.Input;
                    var result = await output.ReadAsync();

                    if (result.IsCanceled)
                        break;

                    var buffer = result.Buffer;
                    if (buffer.IsSingleSegment)
                        await _namedPipeStream.WriteAsync(buffer.First);
                    else
                    {
                        foreach (var segment in buffer)
                            await _namedPipeStream.WriteAsync(segment);
                    }

                    output.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                        break;
                }
            }
            catch (ObjectDisposedException)
            {

            }
            catch (Exception ex)
            {
                unexpectedError = ex;
            }
            finally
            {
                _namedPipeStream.Disconnect();

                Application.Input.Complete(unexpectedError);
                Application.Output.CancelPendingFlush();
            }
        }
    }
}
