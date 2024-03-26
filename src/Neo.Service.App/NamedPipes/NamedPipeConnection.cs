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

using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Neo.Service.App.NamedPipes
{
    internal sealed class NamedPipeConnection : ConnectionContext
    {
        private static readonly int s_minAllocBufferSize = 4096;

        public override IDuplexPipe? Transport { get; set; } = default;
        public IDuplexPipe? Application { get; set; } = default;
        public MemoryPool<byte>? MemoryPool { get; } = default;
        public CancellationToken ConnectionClosed { get; set; }

        public PipeWriter Input => Application?.Output ?? throw new NullReferenceException(nameof(Application));
        public PipeReader Output => Application?.Input ?? throw new NullReferenceException(nameof(Application));
        public NamedPipeServerStream NamedPipe => _stream;
        public NamedPipeEndPoint LocalEndPoint => _localEndPoint;

        private readonly NamedPipeConnectionListener _listener;
        private readonly NamedPipeServerStream _stream;
        private readonly NamedPipeEndPoint _localEndPoint;
        private readonly IDuplexPipe _originalTransport;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new();
        private readonly object _shutdownLock = new();
        private readonly ILogger _logger;

        private bool _connectionClosed;
        private bool _connectionShutdown;
        private bool _streamDisconnected;
        private Exception? _shutdownReason;
        private Task _receivingTask = Task.CompletedTask;
        private Task _sendingTask = Task.CompletedTask;

        public NamedPipeConnection(
            NamedPipeConnectionListener listener,
            NamedPipeServerStream stream,
            NamedPipeEndPoint endPoint,
            ILogger logger,
            MemoryPool<byte> memoryPool,
            PipeOptions inputOptions,
            PipeOptions outputOptions)
        {
            _listener = listener;
            _stream = stream;
            _localEndPoint = endPoint;
            _logger = logger;
            MemoryPool = memoryPool;

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            Transport = _originalTransport = pair.Transport;
            Application = pair.Application;
        }

        public override async ValueTask DisposeAsync()
        {
            _originalTransport.Input.Complete();
            _originalTransport.Output.Complete();

            try
            {
                await _receivingTask;
                await _sendingTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeConnection)}.{nameof(Start)}.");
                _stream.Dispose();
            }

            if (_streamDisconnected == false)
                _stream.Dispose();
        }

        public void Start()
        {
            try
            {
                _receivingTask = DoReceiveAsync();
                _sendingTask = DoSendAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeConnection)}.{nameof(Start)}.");
            }
        }

        public override void Abort(Exception abortReason)
        {
            Shutdown(abortReason);

            Output.CancelPendingRead();
        }

        private async Task DoReceiveAsync()
        {
            Exception? error = null;

            try
            {
                var input = Input;
                while (true)
                {
                    var buffer = input.GetMemory(s_minAllocBufferSize);
                    var bytesReceived = await _stream.ReadAsync(buffer);

                    if (bytesReceived == 0)
                        break;

                    input.Advance(bytesReceived);

                    var result = await input.FlushAsync();

                    if (result.IsCompleted || result.IsCanceled)
                        break;
                }
            }
            catch (ObjectDisposedException ex)
            {
                error = ex;
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
                        await _stream.WriteAsync(buffer.First);
                    else
                    {
                        foreach (var segment in buffer)
                            await _stream.WriteAsync(segment);
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
                    _stream.Disconnect();
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
                _logger.LogError(0, ex, $"Unexpected exception in {nameof(NamedPipeConnection)}.{nameof(CancelConnectionClosedToken)}.");
            }
        }
    }
}
