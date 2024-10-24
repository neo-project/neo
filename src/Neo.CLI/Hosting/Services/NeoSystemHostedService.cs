// Copyright (C) 2015-2024 The Neo Project.
//
// NeoSystemHostedService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Neo.CLI.Configuration;
using Neo.Network.P2P;
using System;
using System.CommandLine;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Hosting.Services
{
    internal partial class NeoSystemHostedService : IHostedService, IAsyncDisposable
    {
        public static NeoSystem? NeoSystem { get; private set; }
        public static NeoOptions? Options { get; private set; }
        public static LocalNode? LocalNode { get; set; }

        private readonly NeoOptions _neoOptions;

        private readonly ProtocolSettings _protocolSettings;

        private readonly CancellationTokenSource _stopCts = new();
        private readonly TaskCompletionSource _stoppedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly SemaphoreSlim _neoSystemStoppedSemaphore = new(1);

        private readonly IConsole _console;

        private NeoSystem? _neoSystem;
        private LocalNode? _localNode;

        private bool _hasStarted = false;
        private int _stopping;

        public NeoSystemHostedService(
            IOptions<NeoOptions> neoOptions,
            IConsole console)
        {
            _neoOptions = neoOptions.Value;
            Options = _neoOptions;
            _console = console;
            _protocolSettings = ProtocolSettings.Load("config.json");
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                await _stoppedTcs.Task.ConfigureAwait(false);
                return;
            }

            _stopCts.Cancel();
            await _neoSystemStoppedSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                await StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _stoppedTcs.TrySetException(ex);
                throw;
            }
            finally
            {
                _stopCts.Dispose();
                _neoSystemStoppedSemaphore.Release();
            }

            _stoppedTcs.TrySetResult();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_hasStarted)
                    throw new InvalidOperationException($"{nameof(NeoSystemHostedService)} has already been started.");
                _hasStarted = true;

                _neoSystem = new(_protocolSettings, _neoOptions.Storage.Engine, string.Format(_neoOptions.Storage.Path, _protocolSettings.Network.ToString("X8")));
                NeoSystem = _neoSystem;
                _localNode = await _neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance(), cancellationToken);
                LocalNode = _localNode;
                _neoSystem.StartNode(new()
                {
                    Tcp = new(IPAddress.Parse(_neoOptions.P2P.Listen), _neoOptions.P2P.Port),
                    MaxConnections = _neoOptions.P2P.MaxConnections,
                    MaxConnectionsPerAddress = _neoOptions.P2P.MaxConnectionsPerAddress,
                    MinDesiredConnections = _neoOptions.P2P.MinDesiredConnections,
                });
            }
            catch
            {
                await StopAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_hasStarted == false)
                return Task.CompletedTask;

            _hasStarted = false;

            _neoSystem?.Dispose();

            _neoSystem = null;

            return Task.CompletedTask;
        }
    }
}
