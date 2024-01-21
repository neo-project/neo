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

using Neo.Service.Pipes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal partial class NodeService
    {
        [PipeMethod(CommandType.Shutdown)]
        private Task<object> ShutdownAsync(
            IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(true);
        }

        [PipeMethod(CommandType.StartNeoSystem)]
        private async Task<object> StartNodeAsync(
            IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken)
        {
            // No import processing
            if (_importBlocksToken is null)
            {
                await StartNeoSystemAsync(cancellationToken);
                return true;
            }

            // We are processing import of blocks
            if (_importBlocksToken.IsCancellationRequested == false)
                _importBlocksToken.Cancel();

            await StartNeoSystemAsync(cancellationToken);
            return true;
        }

        [PipeMethod(CommandType.StopNeoSystem)]
        private async Task<object> StopNodeAsync(
            IReadOnlyDictionary<string, string> args, CancellationToken cancellationToken)
        {
            await StopNeoSystemAsync();
            return true;
        }
    }
}
