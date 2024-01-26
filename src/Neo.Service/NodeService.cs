// Copyright (C) 2015-2024 The Neo Project.
//
// NodeService.cs file belongs to the neo project and is free
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
using Neo.Service.Pipes;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal sealed partial class NodeService : BackgroundService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<NodeService> _logger;
        private readonly ProtocolSettings _nodeProtocolSettings;
        private readonly ApplicationSettings _appSettings;
        private readonly NamedPipeService _namedPipeService;

        private NeoSystem? _neoSystem;
        private LocalNode? _localNode;
        private Task? _importBlocksTask;
        private CancellationTokenSource? _importBlocksToken;

        public NodeService(
            IConfiguration config,
            ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<NodeService>();
            _nodeProtocolSettings = ProtocolSettings.Load(config.GetRequiredSection("ProtocolConfiguration"));
            _appSettings = ApplicationSettings.Load(config.GetRequiredSection("ApplicationConfiguration"));
            _namedPipeService = new(_nodeProtocolSettings, loggerFactory);
            NamedPipeService.RegisterMethods(this);
        }

        public override void Dispose()
        {
            _importBlocksToken?.Dispose();
            _importBlocksTask?.Dispose();
            _neoSystem?.Dispose();
            base.Dispose();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await CreateNeoSystemAsync(cancellationToken);

            _importBlocksToken ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _importBlocksTask = Task.Run(() => StartImport(_appSettings.Storage.Import.Verify, _importBlocksToken.Token).ConfigureAwait(false), cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopImportBlocksAsync();
            await StopNeoSystemAsync();
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) =>
            await _namedPipeService.StartAsync(_appSettings.NamedPipe.Instances, stoppingToken);

        private async Task CreateNeoSystemAsync(CancellationToken cancellationToken)
        {
            string? storagePath = null;
            if (string.IsNullOrEmpty(_appSettings.Storage.Path) == false)
            {
                storagePath = string.Format(_appSettings.Storage.Path, _nodeProtocolSettings.Network);
                if (Directory.Exists(storagePath) == false)
                {
                    if (Path.IsPathFullyQualified(storagePath) == false)
                        storagePath = Path.Combine(AppContext.BaseDirectory, storagePath);
                }
            }

            _neoSystem ??= new(_nodeProtocolSettings, _appSettings.Storage.Engine, storagePath);
            _logger.LogInformation("Initialized system node.");

            _localNode ??= await _neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance(), cancellationToken);
        }

        private async Task StartNeoSystemAsync(CancellationToken cancellationToken)
        {
            if (_neoSystem is null)
                await CreateNeoSystemAsync(cancellationToken);

            _neoSystem!.StartNode(new()
            {
                Tcp = new(IPAddress.Parse(_appSettings.P2P.Listen!), _appSettings.P2P.Port),
                MinDesiredConnections = _appSettings.P2P.MinDesiredConnections,
                MaxConnections = _appSettings.P2P.MaxConnections,
                MaxConnectionsPerAddress = _appSettings.P2P.MaxConnectionsPerAddress,
            });
            _logger.LogInformation("Started system node.");
        }

        private async Task StopImportBlocksAsync()
        {
            _importBlocksToken?.Cancel();

            if (_importBlocksTask is not null)
                await _importBlocksTask;

            _importBlocksToken?.Dispose();
            _importBlocksToken = null;
            _logger.LogInformation("Stopped importing blocks.");
        }

        private Task StopNeoSystemAsync()
        {
            _neoSystem?.Dispose();
            _neoSystem = null;
            _logger.LogInformation("Stopped system node.");

            return Task.CompletedTask;
        }
    }
}
