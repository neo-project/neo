// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeServerListener.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.ObjectPool;
using NamedPipeService;
using Neo.Plugins.Configuration;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PipeOptions = System.IO.Pipelines.PipeOptions;

namespace Neo.Plugins
{
    internal sealed class NamedPipeServerListener
    {
        private readonly NamedPipeServerTransportOptions _options;
        private readonly Channel<NamedPipeServerConnection> _acceptedQueue;
        private readonly NamedPipeServerStreamPoolPolicy _poolPolicy;
        private readonly ObjectPool<NamedPipeServerStream> _namedPipeServerStreamPool;
        private readonly MemoryPool<byte> _memoryPool;

        private readonly CancellationTokenSource _listeningTokenSource = new();
        private readonly CancellationToken _listeningToken;

        private readonly Mutex _mutex;

        private readonly PipeOptions _inputOptions;
        private readonly PipeOptions _outputOptions;

        private Task? _completeListeningTask;
        private int _disposed;

        public NamedPipeEndPoint LocalEndPoint { get; }

        public NamedPipeServerListener(
            NamedPipeEndPoint endPoint,
            NamedPipeServerTransportOptions? options)
        {
            _mutex = new Mutex(false, $"NamedPipe-{endPoint.PipeName}", out var createdNew);
            if (!createdNew)
            {
                _mutex.Dispose();
                throw new ApplicationException($"Named pipe '{endPoint.PipeName}' is already in use.");
            }

            LocalEndPoint = endPoint;
            _options = options ?? new();
            _poolPolicy = new NamedPipeServerStreamPoolPolicy(LocalEndPoint, _options);
            _memoryPool = _options.MemoryPoolFactory();
            _listeningToken = _listeningTokenSource.Token;

            var objectPoolProvider = new DefaultObjectPoolProvider();
            _namedPipeServerStreamPool = objectPoolProvider.Create(_poolPolicy);

            _acceptedQueue = Channel.CreateBounded<NamedPipeServerConnection>(capacity: 1);

            var maxReadBufferSize = _options.MaxReadBufferSize;
            var maxWriteBufferSize = _options.MaxWriteBufferSize;

            _inputOptions = new PipeOptions(_memoryPool, PipeScheduler.ThreadPool, PipeScheduler.Inline, maxReadBufferSize, maxReadBufferSize / 2, useSynchronizationContext: false);
            _outputOptions = new PipeOptions(_memoryPool, PipeScheduler.Inline, PipeScheduler.ThreadPool, maxWriteBufferSize, maxWriteBufferSize / 2, useSynchronizationContext: false);
        }

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
                    Utility.Log(nameof(NamedPipeServerListener), LogLevel.Error, "Named pipe listener aborted.");
                    _acceptedQueue.Writer.TryComplete(ex);
                }
            });
        }

        public async ValueTask<NamedPipeServerConnection?> AcceptAsync(CancellationToken cancellationToken = default)
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

        private async Task StartAsync(NamedPipeServerStream nextStream)
        {
            while (true)
            {
                try
                {
                    var stream = nextStream;

                    await stream.WaitForConnectionAsync(_listeningToken);

                    var connection = new NamedPipeServerConnection(this, LocalEndPoint, stream, _inputOptions, _outputOptions);
                    connection.Start();

                    nextStream = _namedPipeServerStreamPool.Get();

                    while (_acceptedQueue.Writer.TryWrite(connection) == false)
                    {
                        if (await _acceptedQueue.Writer.WaitToWriteAsync(_listeningToken) == false)
                            throw new InvalidOperationException("Accept queue writer was unexpectedly closed.");
                    }
                }
                catch (IOException) when (_listeningToken.IsCancellationRequested == false)
                {
                    Utility.Log(nameof(NamedPipeServerListener), LogLevel.Error, "Named pipe listener received broken pipe while waiting for a connection.");

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
    }
}
