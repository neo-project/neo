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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neo.Service.IO;
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
            base.Dispose();
            _neoSystem?.Dispose();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _neoSystem ??= new(_nodeProtocolSettings, _nodeSettings.Storage.Engine, _nodeSettings.Storage.Path);
            _logger.LogInformation("Neo system initialized.");

            _neoSystem.StartNode(new()
            {
                Tcp = new(IPAddress.Parse(_nodeSettings.P2P.Listen!), _nodeSettings.P2P.Port),
                MinDesiredConnections = _nodeSettings.P2P.MinDesiredConnections,
                MaxConnections = _nodeSettings.P2P.MaxConnections,
                MaxConnectionsPerAddress = _nodeSettings.P2P.MaxConnectionsPerAddress,
            }); ;
            _logger.LogInformation("Neo system node started.");

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _neoSystem?.Dispose();
            _neoSystem = null;
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var linkedSourceToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            var taskList = GC.AllocateUninitializedArray<Task>(4);
            for (var i = 0; i < taskList.Length; i++)
            {
                var pipLogger = _loggerFactory.CreateLogger<NodeCommandPipeServer>();
                var pipeServer = new NodeCommandPipeServer(pipLogger);
                taskList[i] = Task.Run(async () => await pipeServer.StartAsync(linkedSourceToken.Token).ConfigureAwait(false), stoppingToken);
            }
            await Task.WhenAll(taskList);
        }
    }
}
