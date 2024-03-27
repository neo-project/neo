// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Service.Pipes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.IO.Pipes
{
    internal sealed class NamedPipeService : IDisposable
    {
        public static Version Version => NodeUtilities.GetApplicationVersion();
        public static string PipeName => $"neo.node\\{Version.ToString(3)}\\CommandShell";

        public int Instances => _pipeServers.Count;

        private readonly ILogger<PipeServer> _pipeServerLogger;
        private readonly ILogger<NamedPipeService> _logger;
        private readonly List<PipeServer> _pipeServers;
        private readonly ProtocolSettings _protocolSettings;

        private PeriodicTimer? _periodicTimer;

        public NamedPipeService(
            ProtocolSettings protocolSettings,
            ILoggerFactory loggerFactory)
        {
            _protocolSettings = protocolSettings;
            _pipeServers = new();
            _pipeServerLogger = loggerFactory.CreateLogger<PipeServer>();
            _logger = loggerFactory.CreateLogger<NamedPipeService>();
        }

        public void Dispose()
        {
            ShutdownServers();
        }

        public async Task StartAsync(int maxAllowConnections, CancellationToken cancellationToken = default)
        {
            for (var i = 1; i < maxAllowConnections; i++)
                await CreateNewServer(cancellationToken);
            _logger.LogInformation("Created {Connections} instances.", maxAllowConnections);

            _periodicTimer = new(TimeSpan.FromSeconds(1));
            await WaitAsync(cancellationToken);
        }

        private Task CreateNewServer(CancellationToken cancellationToken = default)
        {
            var server = new PipeServer(
                NodeUtilities.GetApplicationVersionNumber(),
                _protocolSettings.Network,
                _pipeServerLogger);
            _pipeServers.Add(server);
            _ = server.StartAndListenAsync(cancellationToken);
            _logger.LogDebug("Created new instance.");
            return Task.CompletedTask;
        }

        private void ShutdownServers()
        {
            _periodicTimer?.Dispose();

            foreach (var server in _pipeServers)
                server.Dispose();

            _pipeServers.Clear();
            _logger.LogInformation("Shutdown complete.");
        }

        private async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            if (_periodicTimer is null) return;

            while (await _periodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                for (var i = 0; i < _pipeServers.Count; i++)
                {
                    if (_pipeServers[i].IsStreamOpen) continue;

                    _logger.LogWarning("Restarting instance {Instance}.", i);

                    _pipeServers[i].Dispose();
                    _pipeServers[i] = new PipeServer(
                        NodeUtilities.GetApplicationVersionNumber(),
                        _protocolSettings.Network,
                        _pipeServerLogger);
                    _ = _pipeServers[i].StartAndListenAsync(cancellationToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
