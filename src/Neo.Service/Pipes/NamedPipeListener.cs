// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeListener.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.ObjectPool;
using Neo.IO.Pipes;
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

namespace Neo.CLI.Pipes
{
    internal class NamedPipeListener : IAsyncDisposable
    {
        private static readonly char[] s_invalidChars = Path.GetInvalidFileNameChars();

        private readonly Channel<NamedPipeConnection> _connections;
        private readonly NamedPipeStreamPoolPolicy _namedPipeStreamPoolPolicy;
        private readonly ObjectPool<NamedPipeServerStream> _namedPipeServerStreamPool;
        private readonly NamedPipeTransportOptions _namedPipeTransportOptions;

        private readonly MemoryPool<byte> _memoryPool;
        private readonly PipeOptions _inputOptions;
        private readonly PipeOptions _outputOptions;

        private readonly CancellationTokenSource _listeningTokenSource = new();
        private readonly CancellationToken _listeningToken;
        private readonly Mutex _mutex;

        private Task? _completeListeningTask;
        private int _disposed;

        public NamedPipeListener(
            NamedPipeEndPoint endPoint,
            NamedPipeTransportOptions? options = null)
        {
            var pipeName = endPoint.PipeName;
            if (endPoint.PipeName.IndexOfAny(s_invalidChars) > -1)
            {
                foreach (var c in s_invalidChars)
                    pipeName = pipeName.Replace(c, '-');
            }

            _mutex = new Mutex(false, $"NamedPipe-{pipeName}", out var createdNew);
            if (!createdNew)
            {
                _mutex.Dispose();
                throw new ApplicationException($"Named pipe '{endPoint.PipeName}' is already in use.");
            }

            _namedPipeTransportOptions = options ?? new();
            _namedPipeStreamPoolPolicy = new(endPoint, _namedPipeTransportOptions);
            _listeningToken = _listeningTokenSource.Token;
            _connections = Channel.CreateBounded<NamedPipeConnection>(capacity: 1);
            _memoryPool = _namedPipeTransportOptions.MemoryPoolFactory();

            var objectPoolProvider = new DefaultObjectPoolProvider();
            _namedPipeServerStreamPool = objectPoolProvider.Create(_namedPipeStreamPoolPolicy);

            var maxReadBufferSize = _namedPipeTransportOptions.MaxReadBufferSize;
            var maxWriteBufferSize = _namedPipeTransportOptions.MaxWriteBufferSize;

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
            var listeningTasks = new Task[_namedPipeTransportOptions.ListenerQueueCount];

            for (var i = 0; i < listeningTasks.Length; i++)
            {
                var initialPipeStream = _namedPipeServerStreamPool.Get();
                _namedPipeStreamPoolPolicy.SetFirstPipeStarted();

                listeningTasks[i] = Task.Run(() => StartAsync(initialPipeStream));
            }

            _completeListeningTask = Task.Run(async () =>
            {
                try
                {
                    await Task.WhenAll(listeningTasks);
                    _connections.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    _connections.Writer.TryComplete(ex);
                }
            });
        }

        public async ValueTask<NamedPipeConnection?> AcceptAsync(CancellationToken cancellationToken = default)
        {
            while (await _connections.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_connections.Reader.TryRead(out var connection))
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

                    var connection = new NamedPipeConnection(this, stream, _inputOptions, _outputOptions);
                    connection.Start();

                    nextStream = _namedPipeServerStreamPool.Get();

                    while (_connections.Writer.TryWrite(connection) == false)
                    {
                        if (await _connections.Writer.WaitToWriteAsync(_listeningToken) == false)
                            throw new InvalidOperationException("Accept queue writer was unexpectedly closed.");
                    }
                }
                catch (IOException) when (_listeningToken.IsCancellationRequested)
                {
                    nextStream.Dispose();
                    nextStream = _namedPipeServerStreamPool.Get();
                }
                catch (OperationCanceledException) when (_listeningToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
