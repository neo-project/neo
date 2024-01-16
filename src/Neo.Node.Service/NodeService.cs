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
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Node.Service
{
    internal sealed partial class NodeService : BackgroundService
    {
        private readonly ILogger<NodeService> _logger;
        private readonly NeoSystem? _neoSystem;
        private readonly ProtocolSettings _nodeProtocolSettings;
        private readonly NodeSettings _nodeSettings;

        public NodeService(
            IConfiguration config,
            ILogger<NodeService> logger)
        {
            _logger = logger;
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

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

        }
    }
}
