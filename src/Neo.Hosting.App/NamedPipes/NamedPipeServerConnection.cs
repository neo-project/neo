// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeServerConnection.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Hosting.App.NamedPipes.Protocol.Messages;
using System;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Neo.Hosting.App.NamedPipes
{
    internal sealed class NamedPipeServerConnection : ITransport
    {
        internal const int MinAllocBufferSize = 4096;
        internal const int MaxMessageCapacity = 3;

        private readonly CancellationTokenSource _connectionClosedTokenSource = new();
        private readonly CancellationToken _connectionClosedToken = default;

        private readonly NamedPipeServerStream _serverStream;
        private readonly Channel<PipeMessage> _messageQueue;
        private readonly NamedPipeServerListener _listener;
        private readonly NamedPipeEndPoint _endPoint;

        private readonly IDuplexPipe _originalTransport;
        private readonly object _shutdownLock = new();
        private readonly ILogger _logger;

        private Task _receivingTask = Task.CompletedTask;
        private Task _sendingTask = Task.CompletedTask;
        private Task _processMessageTask = Task.CompletedTask;

        private Exception? _shutdownReason;

        private bool _connectionClosed;
        private bool _connectionShutdown;
        private bool _streamDisconnected;

        internal PipeWriter Input => Application.Output;

        internal PipeReader Output => Application.Input;

        internal PipeWriter Writer => Transport.Output;

        internal PipeReader Reader => Transport.Input;

        internal IDuplexPipe Application { get; private set; }

        public IDuplexPipe Transport { get; private set; }

        public EndPoint LocalEndPoint => _endPoint;

        public Exception? ShutdownReason => _shutdownReason;

        public int MessageQueueCount => _messageQueue.Reader.Count;

        internal NamedPipeServerConnection(
            NamedPipeServerListener listener,
            NamedPipeEndPoint endPoint,
            NamedPipeServerStream serverStream,
            PipeOptions inputOptions,
            PipeOptions outputOptions,
            ILogger logger)
        {
            _listener = listener;
            _endPoint = endPoint;
            _serverStream = serverStream;
            _logger = logger;

            _connectionClosedToken = _connectionClosedTokenSource.Token;
            _messageQueue = Channel.CreateBounded<PipeMessage>(
                new BoundedChannelOptions(MaxMessageCapacity)
                {
                    SingleReader = true,
                    FullMode = BoundedChannelFullMode.Wait,
                });

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
                await _processMessageTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeServerConnection)}.{nameof(DisposeAsync)}.");
                _serverStream.Dispose();
            }

            if (_streamDisconnected == false)
                _serverStream.Dispose();
            else
                _listener.ReturnStream(_serverStream);
        }

        public void Abort(Exception abortReason)
        {
            Shutdown(abortReason);

            Output.CancelPendingRead();
            Reader.CancelPendingRead();
        }

        public async ValueTask<PipeMessage?> ReadAsync(CancellationToken cancellationToken = default)
        {
            while (await _messageQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_messageQueue.Reader.TryRead(out var message))
                    return message;
            }

            return null;
        }

        internal void Start()
        {
            try
            {
                _receivingTask = DoReceiveAsync();
                _sendingTask = DoSendAsync();
                _processMessageTask = ProcessMessagesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeServerConnection)}.{nameof(Start)}.");
            }
        }

        private async Task DoReceiveAsync()
        {
            Exception? error = null;

            try
            {
                var input = Input;

                while (true)
                {
                    var buffer = input.GetMemory(MinAllocBufferSize);
                    var bytesReceived = await _serverStream.ReadAsync(buffer);

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
                Input.Complete(_shutdownReason ?? error);
                FireConnectionClosed();
            }
        }

        private async Task DoSendAsync()
        {
            Exception? shutdownReason = null;
            Exception? unexpectedError = null;

            try
            {
                while (true)
                {
                    var result = await Output.ReadAsync();

                    if (result.IsCanceled)
                        break;

                    var buffer = result.Buffer;
                    if (buffer.IsSingleSegment)
                        await _serverStream.WriteAsync(buffer.First);
                    else
                    {
                        foreach (var segment in buffer)
                            await _serverStream.WriteAsync(segment);
                    }

                    Output.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                        break;
                }
            }
            catch (ObjectDisposedException ex)
            {
                shutdownReason = ex;
            }
            catch (Exception ex)
            {
                shutdownReason = ex;
                unexpectedError = ex;
            }
            finally
            {
                Shutdown(shutdownReason);

                Output.Complete(unexpectedError);
                Input.CancelPendingFlush();
            }
        }

        private async Task ProcessMessagesAsync()
        {
            Exception? unexpectedError = null;

            try
            {
                while (true)
                {
                    var result = await Reader.ReadAsync();

                    if (result.IsCanceled)
                        break;

                    var buffer = result.Buffer;
                    if (buffer.IsSingleSegment)
                        await QueueMessageAsync(buffer.First);
                    else
                    {
                        foreach (var segment in buffer)
                            await QueueMessageAsync(segment);
                    }

                    Reader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                        break;
                }
            }
            catch (Exception ex)
            {
                unexpectedError = ex;

                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeServerConnection)}.{nameof(ProcessMessagesAsync)}.");
            }
            finally
            {
                Shutdown(unexpectedError);

                Reader.Complete(unexpectedError);
                Output.CancelPendingRead();

                _messageQueue.Writer.Complete(unexpectedError);
            }
        }

        private async Task QueueMessageAsync(ReadOnlyMemory<byte> buffer)
        {
            try
            {
                if (buffer.IsEmpty)
                    return;

                var message = PipeMessage.Create(buffer);

                if (message is null)
                    return;

                if (_messageQueue.Writer.TryWrite(message) == false)
                {
                    if (await _messageQueue.Writer.WaitToWriteAsync(_connectionClosedToken) == false)
                        throw new InvalidOperationException("Message queue writer was unexpectedly closed.");
                }
            }
            catch (IndexOutOfRangeException) // NULL message or Empty message
            {
                _logger.LogTrace("Received a corrupted message.");
            }
            catch (FormatException ex) // Normally invalid or corrupt message
            {
                _logger.LogTrace("{Exception}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeServerConnection)}.{nameof(QueueMessageAsync)}.");
            }
        }

        private void Shutdown(Exception? shutdownReason)
        {
            lock (_shutdownLock)
            {
                if (_connectionShutdown)
                    return;

                _connectionShutdown = true;

                _shutdownReason = shutdownReason;

                try
                {
                    _serverStream.Disconnect();
                    _streamDisconnected = true;
                }
                catch
                {
                }
            }
        }

        private void FireConnectionClosed()
        {
            lock (_shutdownLock)
            {
                if (_connectionClosed)
                    return;

                _connectionClosed = true;
            }

            CancelConnectionClosedToken();
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeServerConnection)}.{nameof(CancelConnectionClosedToken)}.");
            }
        }
    }
}
