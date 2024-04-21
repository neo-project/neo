// Copyright (C) 2015-2024 The Neo Project.
//
// PromptSystemHostedService.cs file belongs to the neo project and is free
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
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.NamedPipes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.Hosting
{
    internal sealed class PromptSystemHostedService : IHostedService, IDisposable
    {
        public NamedPipeEndPoint EndPoint { get; } = new NamedPipeEndPoint(Path.GetRandomFileName());

        private readonly SemaphoreSlim _bindSemaphore = new(1);
        private readonly ObjectPoolProvider _objectPoolProvider;

        private readonly CancellationTokenSource _stopCts = new();
        private readonly TaskCompletionSource _stoppedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly NamedPipeTransportOptions _transportOptions;
        private readonly NamedPipeTransportFactory _transportFactory;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        private NamedPipeConnectionListener? _connectionListener;

        private bool _hasStarted;
        private int _stopping;

        public PromptSystemHostedService(
            ILoggerFactory? loggerFactory = null,
            IOptions<NamedPipeTransportOptions>? options = null,
            ObjectPoolProvider? objectPoolProvider = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _transportOptions = options?.Value ?? new NamedPipeTransportOptions();
            _objectPoolProvider = objectPoolProvider ?? new DefaultObjectPoolProvider();

            _transportFactory = new(_loggerFactory, Options.Create(_transportOptions), _objectPoolProvider);
            _logger = _loggerFactory.CreateLogger("Remote.Management");
        }

        public void Dispose()
        {
            StopAsync(new CancellationToken(true)).GetAwaiter().GetResult();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_hasStarted)
                    throw new InvalidOperationException($"{nameof(PromptSystemHostedService)} has already been started.");

                _hasStarted = true;
                _logger.LogInformation("PromptSystem started.");

                await BindAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PromptSystem is shutting down...");

            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                await _stoppedTcs.Task.ConfigureAwait(false);
                return;
            }

            _stopCts.Cancel();

#pragma warning disable CA2016 // Don't use cancellationToken when acquiring the semaphore. Dispose calls this with a pre-canceled token.
            await _bindSemaphore.WaitAsync().ConfigureAwait(false);
#pragma warning restore CA2016

            try
            {
                if (_connectionListener is not null)
                    await _connectionListener.UnbindAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _stoppedTcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _stopCts.Dispose();
                _bindSemaphore.Release();
            }

            _stoppedTcs.TrySetResult();
        }

        public async Task BindAsync(CancellationToken cancellationToken)
        {
            await _bindSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_stopping == 1)
                    throw new InvalidOperationException($"{nameof(PromptSystemHostedService)} has already been stopped.");

                _connectionListener = await _transportFactory.BindAsync(EndPoint, cancellationToken);
                _logger.LogInformation("neo-cmd connect {EndPoint}", EndPoint);
            }
            finally
            {
                _bindSemaphore.Release();
            }
        }
    }
}
