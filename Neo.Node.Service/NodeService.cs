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
using System;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Node.Service
{
    internal sealed partial class NodeService : BackgroundService
    {
        public static JsonSerializerOptions JsonOptions => new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            WriteIndented = false,
        };

        private readonly ILogger<NodeService> _logger;
        private readonly NeoSystem? _neoSystem;
        private readonly ProtocolSettings _nodeProtocolSettings;

        private NamedPipeServerStream? _neoPipeServer;

        public NodeService(
            IConfiguration config,
            ILogger<NodeService> logger)
        {
            _logger = logger;
            _nodeProtocolSettings = ProtocolSettings.Load(config.GetSection("ProtocolConfiguration"));
            NodeSettings.Load(config.GetSection("ApplicationConfiguration"));
            PipeCommand.RegisterMethods(this);
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
            try
            {
                _neoPipeServer = new("neoclipipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

                while (stoppingToken.IsCancellationRequested == false)
                {
                    await _neoPipeServer.WaitForConnectionAsync(stoppingToken);
                    var command = await JsonSerializer.DeserializeAsync<PipeCommand>(_neoPipeServer, JsonOptions, stoppingToken);

                    if (command == null || command.Command == CommandType.None)
                    {
                        _neoPipeServer.Disconnect();
                        continue;
                    }

                    await command.ExecuteAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("{Exception}", ex!.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
