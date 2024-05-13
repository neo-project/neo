// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipesSystemHostedService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neo.Hosting.App.NamedPipes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.Hosting.Services
{
    internal sealed class NamedPipesSystemHostedService : IHostedService, IDisposable
    {
        public NamedPipeEndPoint LocalEndPoint => _namedPipeListener.LocalEndPoint;

        private readonly SemaphoreSlim _bindSemaphore = new(1);

        private readonly CancellationTokenSource _stopTokenSource = new();
        private readonly TaskCompletionSource _stoppedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private readonly NamedPipeServerListener _namedPipeListener;

        private NamedPipeServerConnection? _connection;

        private bool _hasStarted;
        private int _stopping;

        public NamedPipesSystemHostedService(
            NamedPipeServerListener listener,
            ILoggerFactory? loggerFactory = null)
        {
            _namedPipeListener = listener;
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger(LoggerCategoryDefaults.RemoteManagement);
        }

        public void Dispose()
        {
            StopAsync(new CancellationToken(true)).GetAwaiter().GetResult();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_hasStarted)
                    throw new InvalidOperationException($"{nameof(NamedPipesSystemHostedService)} has already been started.");

                _hasStarted = true;
                _logger.LogInformation("NamedPipeSystem started.");

                _namedPipeListener.Start();

                _ = ProcessClientAsync();
            }
            catch
            {
                Dispose();
                throw;
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("NamedPipeSystem is shutting down...");

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
                await _namedPipeListener.UnbindAsync(new CancellationToken(true)).ConfigureAwait(false);
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

        private async Task ProcessClientAsync()
        {
            var stoppingToken = _stopTokenSource.Token;

            await _bindSemaphore.WaitAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                if (_stopping == 1)
                    throw new InvalidOperationException($"{nameof(NamedPipesSystemHostedService)} has already been stopped.");

                _logger.LogInformation("Now listening on: {EndPoint}", LocalEndPoint);

                while (true)
                {
                    _connection = await _namedPipeListener.AcceptAsync(stoppingToken).ConfigureAwait(false);

                    if (_connection is null)
                        break;
                }
            }
            finally
            {
                _bindSemaphore.Release();

                if (_connection is not null)
                    await _connection.DisposeAsync();
            }
        }
    }
}
