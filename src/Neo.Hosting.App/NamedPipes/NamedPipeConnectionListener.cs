// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeConnectionListener.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Neo.Hosting.App.NamedPipes
{
    internal sealed class NamedPipeConnectionListener : IAsyncDisposable
    {
        public EndPoint EndPoint => _endPoint;

        private readonly NamedPipeEndPoint _endPoint;
        private readonly NamedPipeTransportOptions _options;
        private readonly ILogger _logger;
        private readonly Mutex _mutex;
        private readonly ObjectPool<NamedPipeServerStream> _namedPipeServerStreamPool;
        private readonly CancellationTokenSource _listeningTokenSource = new();
        private readonly CancellationToken _listeningToken;
        private readonly Channel<NamedPipeConnection> _acceptedQueue;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly PipeOptions _inputOptions;
        private readonly PipeOptions _outputOptions;
        private readonly NamedPipeServerStreamPoolPolicy _poolPolicy;
        private Task? _completeListeningTask;
        private int _disposed;

        public NamedPipeConnectionListener(
            ILoggerFactory loggerFactory,
            NamedPipeEndPoint endPoint,
            NamedPipeTransportOptions options,
            ObjectPoolProvider objectPoolProvider,
            Mutex mutex)
        {
            _logger = loggerFactory.CreateLogger("NamedPipes");
            _endPoint = endPoint;
            _options = options;
            _memoryPool = options.MemoryPoolFactory();
            _mutex = mutex;
            _listeningToken = _listeningTokenSource.Token;
            _poolPolicy = new NamedPipeServerStreamPoolPolicy(endPoint, options);
            _namedPipeServerStreamPool = objectPoolProvider.Create(_poolPolicy);
            _acceptedQueue = Channel.CreateBounded<NamedPipeConnection>(new BoundedChannelOptions(capacity: 1));

            var maxReadBufferSize = _options.MaxReadBufferSize;
            var maxWriteBufferSize = _options.MaxWriteBufferSize;

            _inputOptions = new(_memoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
            _outputOptions = new(_memoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);
        }

        internal void ReturnStream(NamedPipeServerStream stream)
        {
            Debug.Assert(stream.IsConnected == false, "Stream should have been successfully disconnected to reach this point.");

            _namedPipeServerStreamPool.Return(stream);
        }

        public void Start()
        {
            Debug.Assert(_completeListeningTask == null, "Already started");

            var listeningTasks = new Task[_options.ListenerQueueCount];

            for (var i = 0; i < listeningTasks.Length; i++)
            {
                var initialStream = _namedPipeServerStreamPool.Get();
                _poolPolicy.SetFirstPipeStarted();

                listeningTasks[i] = Task.Run(() => StartAsync(initialStream));
            }

            _completeListeningTask = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(listeningTasks);
                    _acceptedQueue.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Named pipe listener aborted.");
                    _acceptedQueue.Writer.TryComplete(ex);
                }
            });
        }

        private async Task StartAsync(NamedPipeServerStream nextStream)
        {
            while (true)
            {
                try
                {
                    var stream = nextStream;

                    await stream.WaitForConnectionAsync(_listeningToken);

                    var connection = new NamedPipeConnection(this, stream, _endPoint, _logger, _memoryPool, _inputOptions, _outputOptions);
                    connection.Start();

                    nextStream = _namedPipeServerStreamPool.Get();

                    while (_acceptedQueue.Writer.TryWrite(connection) == false)
                    {
                        if (await _acceptedQueue.Writer.WaitToWriteAsync(_listeningToken) == false)
                            throw new InvalidOperationException("Accept queue writer was unexpectedly closed.");
                    }
                }
                catch (IOException ex) when (_listeningToken.IsCancellationRequested == false)
                {
                    _logger.LogDebug(ex, "Named pipe listener received broken pipe while waiting for a connection.");

                    nextStream.Dispose();
                    nextStream = _namedPipeServerStreamPool.Get();
                }
                catch (OperationCanceledException) when (_listeningToken.IsCancellationRequested)
                {
                    break;
                }
            }

            nextStream.Dispose();
        }

        public async ValueTask<NamedPipeConnection?> AcceptAsync(CancellationToken cancellationToken = default)
        {
            while (await _acceptedQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_acceptedQueue.Reader.TryRead(out var connection))
                    return connection;
            }

            return null;
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default) =>
            DisposeAsync();

        #region IAsyncDisposable

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _listeningTokenSource.Cancel();

            _listeningTokenSource.Dispose();
            _mutex.Dispose();

            if (_completeListeningTask is not null)
                await _completeListeningTask;

            (_namedPipeServerStreamPool as IDisposable)?.Dispose();
        }

        #endregion
    }
}
