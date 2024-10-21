// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Neo.CLI.Pipes;
using Neo.CLI.Pipes.Protocols;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Hosting.Services
{
    internal class NamedPipeService(
        NamedPipeEndPoint endPoint) : IHostedService, IAsyncDisposable
    {
        public NamedPipeEndPoint LocalEndPoint => _localEndPoint;

        private readonly NamedPipeEndPoint _localEndPoint = endPoint;
        private readonly NamedPipeListener _listener = new(endPoint);

        private readonly SemaphoreSlim _bindSemaphore = new(1);
        private readonly CancellationTokenSource _stopTokenSource = new();
        private readonly TaskCompletionSource _stoppedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private bool _hasStarted = false;
        private int _stopping;

        public async ValueTask DisposeAsync()
        {
            await StopAsync(new CancellationToken(true)).ConfigureAwait(false);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_hasStarted)
                throw new InvalidOperationException($"{nameof(NamedPipeService)} has already been started.");

            _hasStarted = true;
            _listener.Start();

            _ = ProcessClientsAsync();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                await _stoppedCompletionSource.Task.ConfigureAwait(false);
                return;
            }

            _stopTokenSource.Cancel();

#pragma warning disable CA2016 // Don't use cancellationToken when acquiring the semaphore. Dispose calls this with a pre-canceled token.
            await _bindSemaphore.WaitAsync().ConfigureAwait(false);
#pragma warning restore CA2016

            try
            {
                await _listener!.UnbindAsync(new CancellationToken(true)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _stoppedCompletionSource.TrySetException(ex);
                throw;
            }
            finally
            {
                _stopTokenSource.Dispose();
                _bindSemaphore.Release();
            }

            _stoppedCompletionSource.TrySetResult();
        }

        private async Task ProcessClientsAsync()
        {
            var stoppingToken = _stopTokenSource.Token;
            await _bindSemaphore.WaitAsync().ConfigureAwait(false);

            if (_stopping == 1)
                throw new InvalidOperationException($"{nameof(NamedPipeService)} has been stopped.");

            try
            {
                while (true)
                {
                    var connection = await _listener.AcceptAsync(stoppingToken).ConfigureAwait(false);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    if (connection is null)
                        continue;

                    var threadPoolItem = new NamedPipeMessageProtocol(connection);
                    ThreadPool.UnsafeQueueUserWorkItem(threadPoolItem, preferLocal: false);
                }
            }
            finally
            {
                _bindSemaphore.Release();
            }
        }
    }
}
