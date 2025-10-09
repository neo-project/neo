// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.App.Options;
using Neo.Network.P2P;
using Neo.Plugins;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.App.Services
{
    internal sealed class NeoSystemHostedService : IHostedService, IAsyncDisposable
    {
        private readonly ProtocolSettings _protocolSettings;
        private readonly StorageOptions _storageSettings;
        private readonly NetworkOptions _networkSettings;

        private readonly ILogger _logger;
        private bool _hasStarted = false;
        private NeoSystem? _neoSystem;

        public NeoSystemHostedService(
            NeoConfigurationOptions neoOptions,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(NeoSystem));
            _protocolSettings = neoOptions.ProtocolConfiguration.ToObject();
            _storageSettings = neoOptions.StorageConfiguration;
            _networkSettings = neoOptions.NetworkConfiguration;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_hasStarted)
                throw new InvalidOperationException($"{nameof(NeoSystem)} has already been started.");

            _hasStarted = true;

            NeoPlugin.LoadPlugins();

            _neoSystem = new NeoSystem(_protocolSettings, _storageSettings.Engine, _storageSettings.Path);
            _neoSystem.StartNode(new ChannelsConfig()
            {
                Tcp = new IPEndPoint(_networkSettings.Listen, _networkSettings.Port),
                MinDesiredConnections = _networkSettings.MinDesiredConnections,
                MaxConnections = _networkSettings.MaxConnections,
                MaxConnectionsPerAddress = _networkSettings.MaxConnectionsPerAddress,
                MaxKnownHashes = _networkSettings.MaxKnownHashes,
                EnableCompression = _networkSettings.EnableCompression,
            });

            _logger.LogDebug("{NeoSystem} has started.", nameof(NeoSystem));
            _logger.LogInformation("Now listening on: neo://{IPAddress}:{Port}/#{Network}",
                _networkSettings.Listen, _networkSettings.Port, _protocolSettings.Network);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
