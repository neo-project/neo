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
using Neo.Service.IO;
using System;
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

        public NodeService(
            IConfiguration config,
            ILoggerFactory loggerFactory)
        {
            if (config is null) throw new ArgumentNullException(nameof(config));

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<NodeService>();
            _nodeProtocolSettings = ProtocolSettings.Load(config.GetSection("ProtocolConfiguration"));
            _nodeSettings = NodeSettings.Load(config.GetSection("ApplicationConfiguration"));
            PipeCommand.RegisterMethods(this);
        }

        public override void Dispose()
        {
            _importBlocksTask?.Dispose();
            _neoSystem?.Dispose();
            base.Dispose();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            string? storagePath = null;
            if (string.IsNullOrEmpty(_nodeSettings.Storage.Path) == false)
                storagePath = string.Format(_nodeSettings.Storage.Path, _nodeProtocolSettings.Network);

            _neoSystem ??= new(_nodeProtocolSettings, _nodeSettings.Storage.Engine, storagePath);
            _logger.LogInformation("Neo system initialized.");

            _localNode ??= await _neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance(), cancellationToken);
            _logger.LogInformation("Neo system LocalNode started.");

            _importBlocksTask ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = Task.Run(() => StartImport(false, _importBlocksTask.Token).ConfigureAwait(false), cancellationToken);

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
            var linkedSourceToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var taskList = GC.AllocateUninitializedArray<Task>(_nodeSettings.Pipe.Instances);
            for (var i = 0; i < taskList.Length; i++)
            {
                var pipLogger = _loggerFactory.CreateLogger<NodeCommandPipeServer>();
                taskList[i] = Task.Run(async () =>
                {
                    var pipeServer = new NodeCommandPipeServer(_nodeSettings.Pipe.Instances, pipLogger);
                    await pipeServer.ListenAsync(linkedSourceToken.Token).ConfigureAwait(false);
                    pipeServer.Dispose();
                }, stoppingToken);
            }
            await Task.WhenAll(taskList);
        }
    }
}
