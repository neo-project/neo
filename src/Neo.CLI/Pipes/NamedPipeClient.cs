// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipes.PipeOptions;

namespace Neo.CLI.Pipes
{
    internal class NamedPipeClient : IAsyncDisposable
    {
        internal IDuplexPipe Application { get; private set; }
        public IDuplexPipe Transport { get; private set; }

        public NamedPipeEndPoint EndPoint => _endPoint;

        private readonly NamedPipeEndPoint _endPoint;
        private readonly NamedPipeClientStream _client;

        private Task _receivingTask = Task.CompletedTask;
        private Task _sendingTask = Task.CompletedTask;
        private Task _protocolTask = Task.CompletedTask;

        public NamedPipeClient(
            NamedPipeEndPoint endPoint)
        {
            _endPoint = endPoint;
            _client = new(_endPoint.ServerName, _endPoint.PipeName, PipeDirection.InOut, PipeOptions.WriteThrough | PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            var pair = DuplexPipe.CreateConnectionPair();
            Transport = pair.Transport;
            Application = pair.Application;
        }

        public async ValueTask DisposeAsync()
        {
            Transport.Input.Complete();
            Transport.Output.Complete();

            try
            {
                await _receivingTask;
                await _sendingTask;
            }
            catch (Exception)
            {
            }

            await _client.DisposeAsync();
        }

        public async Task ConnectAsync()
        {
            await _client.ConnectAsync();

            try
            {
                _receivingTask = DoReceiveAsync();
                _sendingTask = DoSendAsync();
                _protocolTask = DoProtocolAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Close()
        {
            _client.Close();
        }

        private async Task DoReceiveAsync()
        {
            Exception? error = null;

            try
            {
                var input = Application.Output;

                while (true)
                {
                    var buffer = input.GetMemory(NamedPipeConnection.MinAllocBufferSize);
                    var bytesReceived = await _client.ReadAsync(buffer);

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
                        await _client.WriteAsync(buffer.First);
                    else
                    {
                        foreach (var segment in buffer)
                            await _client.WriteAsync(segment);
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
                _client.Close();

                Application.Input.Complete(unexpectedError);
                Application.Output.CancelPendingFlush();
            }
        }

        private async Task DoProtocolAsync()
        {
            try
            {

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                await DisposeAsync();
            }
        }
    }
}
