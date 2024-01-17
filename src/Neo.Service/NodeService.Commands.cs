// Copyright (C) 2015-2024 The Neo Project.
//
// NodeService.Commands.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Service.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal partial class NodeService
    {
        [PipeMethod(CommandType.Exit)]
        private async Task<object> ShutdownAsync(string[] args, CancellationToken cancellationToken)
        {
            await StopAsync(CancellationToken.None);
            return true;
        }

        [PipeMethod(CommandType.Start)]
        private async Task<object> StartNodeAsync(string[] args, CancellationToken cancellationToken)
        {
            // No import processing
            if (_importBlocksTask is null)
            {
                await StartNodeAsync(cancellationToken);
                return true;
            }

            // We are processing import of blocks
            if (_importBlocksTask.IsCancellationRequested == false)
                _importBlocksTask.Cancel();

            await StartNodeAsync(cancellationToken);
            return true;
        }

        [PipeMethod(CommandType.Stop)]
        private Task<object> StopNodeAsync(string[] args, CancellationToken cancellationToken)
        {
            if (_neoSystem is null)
                return Task.FromResult<object>(false);

            _neoSystem.Dispose();
            _neoSystem = null;

            _logger.LogInformation("Neo system node stopped.");

            return Task.FromResult<object>(true);
        }
    }
}
