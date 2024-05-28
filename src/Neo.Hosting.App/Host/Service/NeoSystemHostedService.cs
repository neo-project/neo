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

namespace Neo.Hosting.App.Host.Service
{
    internal sealed partial class NeoSystemHostedService(
        ILoggerFactory loggerFactory,
        ProtocolSettings protocolSettings,
        IOptions<NeoOptions> neoOptions) : IHostedService, IAsyncDisposable
    {
        public IPEndPoint? EndPoint
        {
            get; [param: DisallowNull]
            private set;
        }

        public NeoSystem NeoSystem => _neoSystem ??
            throw new InvalidOperationException($"{nameof(NeoSystemHostedService)} needs to be started.");

        private readonly ProtocolSettings _protocolSettings = protocolSettings;
        private readonly NeoOptions _neoOptions = neoOptions.Value;

        private readonly CancellationTokenSource _stopCts = new();
        private readonly TaskCompletionSource _stoppedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly SemaphoreSlim _neoSystemStoppedSemaphore = new(1);

        private readonly ILogger _logger = loggerFactory.CreateLogger(LoggerCategoryDefaults.NeoSystem);

        private NeoSystem? _neoSystem;
        private DataCache? _store;

        private bool _hasStarted;
        private int _stopping;

        public async ValueTask DisposeAsync()
        {
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

                // Force Neo plugins to load
                Plugin.LoadPlugins();
                _logger.LogInformation("Plugin root path: {PluginsDirectory}", Plugin.PluginsDirectory);

                foreach (var plugins in Plugin.Plugins)
                    _logger.LogInformation("Loaded plugin: {Plugin}", plugins.Name);

                string? storagePath = null;
                if (string.IsNullOrEmpty(_neoOptions.Storage.Path) == false)
                {
                    storagePath = string.Format(_neoOptions.Storage.Path, _protocolSettings.Network);
                    if (Directory.Exists(storagePath) == false)
                    {
                        if (Path.IsPathFullyQualified(storagePath) == false)
                            storagePath = Path.Combine(AppContext.BaseDirectory, storagePath);
                    }
                }

                if (StoreFactory.GetStoreProvider(_neoOptions.Storage.Engine) is null)
                    throw new DllNotFoundException($"Plugin '{Path.Combine(Plugin.PluginsDirectory, $"{_neoOptions.Storage.Engine}.dll")}' can't be found.");

                _neoSystem = new(_protocolSettings, _neoOptions.Storage.Engine, storagePath);
                _logger.LogInformation("{NeoSystem} started.", LoggerCategoryDefaults.NeoSystem);

                _store = _neoSystem.StoreView;
            }
            catch
            {
                await StopAsync(cancellationToken);
                throw;
            }
        }

        public bool TryStartNode()
        {
            if (_hasStarted == false) return false;
            if (_neoSystem is null) return false;

            _neoSystem.StartNode(new()
            {
                Tcp = EndPoint = new(IPAddress.Parse(_neoOptions.P2P.Listen), _neoOptions.P2P.Port),
                MinDesiredConnections = _neoOptions.P2P.MinDesiredConnections,
                MaxConnections = _neoOptions.P2P.MaxConnections,
                MaxConnectionsPerAddress = _neoOptions.P2P.MaxConnectionsPerAddress,
            });
            _logger.LogInformation("Now listening on: neo://{EndPoint}/#{Network}", EndPoint, _protocolSettings.Network);

            return true;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_hasStarted == false)
                return Task.CompletedTask;

            _logger.LogInformation("{NeoSystem} is shutting down...", LoggerCategoryDefaults.NeoSystem);

            _neoSystem?.Dispose();

            _neoSystem = null;
            _store = null;

            _hasStarted = false;

            return Task.CompletedTask;
        }
    }
}