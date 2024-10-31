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
using Microsoft.Extensions.Logging;
using Neo.IO.Pipes;
using Neo.Service.Pipes;
using Neo.Service.Pipes.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Hosting.Services
{
    internal class NamedPipeService(
        NamedPipeEndPoint endPoint,
        NamedPipeListener listener,
        NeoSystem neoSystem,
        ILoggerFactory loggerFactory) : IHostedService, IAsyncDisposable
    {
        public NamedPipeEndPoint LocalEndPoint => _endPoint;

        private readonly NamedPipeEndPoint _endPoint = endPoint;
        private readonly NamedPipeListener _listener = listener;
        private readonly NeoSystem _neoSystem = neoSystem;
        private readonly ILoggerFactory _loggerFactory = loggerFactory;
        private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(NamedPipeService));

        private readonly CancellationTokenSource _stopCts = new();

        private bool _hasStarted = false;
        private Task _connTask = Task.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            try
            {
                _stopCts.Cancel();

                await _connTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_hasStarted)
                throw new InvalidOperationException($"{nameof(NamedPipeService)} has already been started.");
            _hasStarted = true;

            _logger.LogInformation("{ClassName} has started.", nameof(NamedPipeService));
            _listener.Start();
            _connTask = ProcessConnectionsAsync();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("{ClassName} has stopped.", nameof(NamedPipeService));
            await DisposeAsync();
        }

        private async Task ProcessConnectionsAsync()
        {
            _logger.LogInformation("Listening on {Pipe} ...", _endPoint);

            try
            {
                while (_stopCts.IsCancellationRequested == false)
                {
                    var conn = await _listener.AcceptAsync(_stopCts.Token);

                    if (conn is null)
                        break;

                    var protocolThread = new ConsoleMessageProtocol(conn, _neoSystem, _loggerFactory.CreateLogger(nameof(ConsoleMessageProtocol)));
                    ThreadPool.UnsafeQueueUserWorkItem(protocolThread, preferLocal: false);
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                await StopAsync().ConfigureAwait(false);
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
