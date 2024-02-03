// Copyright (C) 2015-2024 The Neo Project.
//
// NodeService.NeoSystem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Logging;
using Neo.Network.P2P;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal partial class NodeService
    {
        internal async Task StartNeoSystemAsync(CancellationToken cancellationToken = default)
        {
            if (_importBlocksTokenSource is not null &&
                _importBlocksTokenSource.IsCancellationRequested)
                throw new ApplicationException("Import process is running.");

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

        internal Task StopNeoSystemAsync()
        {
            _neoSystem?.Dispose();
            _neoSystem = null;
            _logger.LogInformation("Stopped system node.");

            return Task.CompletedTask;
        }

        private async Task CreateNeoSystemAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;

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
    }
}
