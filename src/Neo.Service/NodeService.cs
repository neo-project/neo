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
        private readonly NodeSettings _nodeSettings;

        private NeoSystem? _neoSystem;
        private LocalNode? _localNode;
        private CancellationTokenSource? _importBlocksTask;
        private CancellationTokenSource? _namedPipesToken; // DO NOT cancel this token or else the communication is lost.

        public NodeService(
            IConfiguration config,
            ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<NodeService>();
            _nodeProtocolSettings = ProtocolSettings.Load(config.GetRequiredSection("ProtocolConfiguration"));
            _nodeSettings = NodeSettings.Load(config.GetRequiredSection("ApplicationConfiguration"));
            PipeCommand.RegisterMethods(this);
        }

        public override void Dispose()
        {
            _importBlocksTask?.Dispose();
            _namedPipesToken?.Dispose();
            _neoSystem?.Dispose();
            base.Dispose();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await CreateNeoSystemAsync(cancellationToken);

            _importBlocksTask ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = Task.Run(() => StartImport(_nodeSettings.Storage.ImportVerify, _importBlocksTask.Token).ConfigureAwait(false), cancellationToken);

            await base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _importBlocksTask?.Dispose();
            _importBlocksTask = null;

            _neoSystem?.Dispose();
            _neoSystem = null;

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _namedPipesToken ??= CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var taskList = GC.AllocateUninitializedArray<Task>(_nodeSettings.Pipe.Instances);
            for (var i = 0; i < taskList.Length; i++)
            {
                var pipLogger = _loggerFactory.CreateLogger<NodeCommandPipeServer>();
                taskList[i] = Task.Run(async () =>
                {
                    var pipeServer = new NodeCommandPipeServer(_nodeSettings.Pipe.Instances, pipLogger);
                    await pipeServer.ListenAsync(_namedPipesToken.Token).ConfigureAwait(false); // Never exit
                    pipeServer.Dispose(); // This is for disposing if service is ever removed.
                }, stoppingToken);
            }
            await Task.WhenAll(taskList);
        }

        private async Task CreateNeoSystemAsync(CancellationToken cancellationToken)
        {
            string? storagePath = null;
            if (string.IsNullOrEmpty(_nodeSettings.Storage.Path) == false)
                storagePath = string.Format(_nodeSettings.Storage.Path, _nodeProtocolSettings.Network);

            _neoSystem ??= new(_nodeProtocolSettings, _nodeSettings.Storage.Engine, storagePath);
            _logger.LogInformation("Neo system initialized.");

            _localNode ??= await _neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance(), cancellationToken);
            _logger.LogInformation("Neo system LocalNode started.");
        }

        private async Task StartNodeAsync(CancellationToken cancellationToken)
        {
            if (_neoSystem is null)
                await CreateNeoSystemAsync(cancellationToken);

            _neoSystem!.StartNode(new()
            {
                Tcp = new(IPAddress.Parse(_nodeSettings.P2P.Listen!), _nodeSettings.P2P.Port),
                MinDesiredConnections = _nodeSettings.P2P.MinDesiredConnections,
                MaxConnections = _nodeSettings.P2P.MaxConnections,
                MaxConnectionsPerAddress = _nodeSettings.P2P.MaxConnectionsPerAddress,
            });
            _logger.LogInformation("Neo system node started.");
        }
    }
}
