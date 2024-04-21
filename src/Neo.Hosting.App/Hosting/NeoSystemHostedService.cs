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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration;
using Neo.Persistence;
using Neo.Plugins;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.Hosting
{
    internal sealed class NeoSystemHostedService(
        ILoggerFactory loggerFactory,
        ProtocolSettings protocolSettings,
        IOptions<SystemOptions> systemOptions) : IHostedService, IDisposable
    {
        public IPEndPoint? EndPoint
        {
            get; [param: DisallowNull]
            private set;
        }

        public NeoSystem NeoSystem => _neoSystem ??
            throw new InvalidOperationException($"{nameof(PromptSystemHostedService)} needs to be started.");

        private readonly ProtocolSettings _protocolSettings = protocolSettings;
        private readonly SystemOptions _systemOptions = systemOptions.Value;

        private readonly CancellationTokenSource _stopCts = new();
        private readonly TaskCompletionSource _stoppedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly SemaphoreSlim _neoSystemStoppedSemaphore = new(1);

        private readonly ILogger<NeoSystem> _logger = loggerFactory.CreateLogger<NeoSystem>();

        private NeoSystem? _neoSystem;

        private bool _hasStarted;
        private int _stopping;

        public void Dispose()
        {
            StopAsync(new CancellationToken(true)).GetAwaiter().GetResult();
        }

        [MemberNotNullWhen(true, nameof(_neoSystem), nameof(NeoSystem))]
        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_hasStarted)
                    throw new InvalidOperationException($"{nameof(PromptSystemHostedService)} has already been started.");

                _hasStarted = true;

                // Force Neo plugins to load
                Plugin.LoadPlugins();
                _logger.LogInformation("Plugin root path: {PluginsDirectory}", Plugin.PluginsDirectory);

                string? storagePath = null;
                if (string.IsNullOrEmpty(_systemOptions.Storage.Path) == false)
                {
                    storagePath = string.Format(_systemOptions.Storage.Path, _protocolSettings.Network);
                    if (Directory.Exists(storagePath) == false)
                    {
                        if (Path.IsPathFullyQualified(storagePath) == false)
                            storagePath = Path.Combine(AppContext.BaseDirectory, storagePath);
                    }
                }

                if (StoreFactory.GetStoreProvider(_systemOptions.Storage.Engine) is null)
                    throw new DllNotFoundException($"Plugin '{Path.Combine(Plugin.PluginsDirectory, $"{_systemOptions.Storage.Engine}.dll")}' can't be found.");

                _neoSystem = new(_protocolSettings, _systemOptions.Storage.Engine, storagePath);
                _logger.LogInformation("NeoSystem started.");

                return Task.CompletedTask;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public bool TryStartNode()
        {
            if (_hasStarted == false) return false;
            if (_neoSystem is null) return false;

            _neoSystem.StartNode(new()
            {
                Tcp = EndPoint = new(IPAddress.Parse(_systemOptions.P2P.Listen), _systemOptions.P2P.Port),
                MinDesiredConnections = _systemOptions.P2P.MinDesiredConnections,
                MaxConnections = _systemOptions.P2P.MaxConnections,
                MaxConnectionsPerAddress = _systemOptions.P2P.MaxConnectionsPerAddress,
            });
            _logger.LogInformation("Listening on remote endpoint: neo://{EndPoint}/#{Network}", EndPoint, _protocolSettings.Network);

            return true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("NeoSystem is shutting down...");

            if (Interlocked.Exchange(ref _stopping, 1) == 1)
            {
                await _stoppedTcs.Task.ConfigureAwait(false);
                return;
            }

            _stopCts.Cancel();

#pragma warning disable CA2016 // Don't use cancellationToken when acquiring the semaphore. Dispose calls this with a pre-canceled token.
            await _neoSystemStoppedSemaphore.WaitAsync().ConfigureAwait(false);
#pragma warning restore CA2016

            try
            {
                _neoSystem?.Dispose();
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
    }
}
