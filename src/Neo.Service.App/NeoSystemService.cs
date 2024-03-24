// Copyright (C) 2015-2024 The Neo Project.
//
// NeoSystemService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo.Network.P2P;
using Neo.Persistence;
using Neo.Plugins;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.App
{
    internal sealed class NeoSystemService : BackgroundService
    {
        public bool IsRunning { get; private set; }
        public NeoSystem? NeoSystem => _neoSystem;

        private readonly ILogger<NeoSystemService> _logger;
        private readonly ProtocolSettings _protocolSettings;
        private readonly ApplicationSettings _appSettings;

        private NeoSystem? _neoSystem;
        private LocalNode? _localNode;

        public NeoSystemService(
            IConfiguration config,
            ILogger<NeoSystemService> logger)
        {
            _logger = logger;
            _protocolSettings = ProtocolSettings.Load(config.GetRequiredSection("ProtocolConfiguration"));
            _appSettings = ApplicationSettings.Load(config.GetRequiredSection("ApplicationConfiguration"));

            Plugin.LoadPlugins();
        }

        public override void Dispose()
        {
            _logger.LogInformation("NeoSystem is shutting down...");

            _neoSystem?.Dispose();
            IsRunning = false;
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string? storagePath = null;
            if (string.IsNullOrEmpty(_appSettings.Storage.Path) == false)
            {
                storagePath = string.Format(_appSettings.Storage.Path, _protocolSettings.Network);
                if (Directory.Exists(storagePath) == false)
                {
                    if (Path.IsPathFullyQualified(storagePath) == false)
                        storagePath = Path.Combine(AppContext.BaseDirectory, storagePath);
                }
            }

            if (StoreFactory.GetStoreProvider(_appSettings.Storage.Engine) is null)
                throw new DllNotFoundException($"Plugin '{_appSettings.Storage.Engine}.dll' can't be found.");

            _neoSystem ??= new(_protocolSettings, _appSettings.Storage.Engine, storagePath);
            _localNode ??= await _neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance(), stoppingToken);
            IsRunning = true;
            _logger.LogInformation("NeoSystem started.");

            await Task.Delay(-1, stoppingToken);
        }

        public void StartNode()
        {
            if (_neoSystem is null)
                throw new NullReferenceException("NeoSystem");

            _neoSystem.StartNode(new()
            {
                Tcp = new(IPAddress.Parse(_appSettings.P2P.Listen!), _appSettings.P2P.Port),
                MinDesiredConnections = _appSettings.P2P.MinDesiredConnections,
                MaxConnections = _appSettings.P2P.MaxConnections,
                MaxConnectionsPerAddress = _appSettings.P2P.MaxConnectionsPerAddress,
            });

            _logger.LogInformation("Waiting for connections: {ListenIpAddress}:{ListenPort}", _appSettings.P2P.Listen!, _appSettings.P2P.Port);
        }
    }
}
