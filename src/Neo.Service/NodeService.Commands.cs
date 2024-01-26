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

using Neo.IO;
using Neo.Service.Pipes;
using Neo.Service.Pipes.Payloads;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service
{
    internal partial class NodeService
    {
        [PipeMethod(PipeMessageCommand.Test, false)]
        private ISerializable? ProtocolPing(ISerializable? message) => null;

        [PipeMethod(PipeMessageCommand.Start, true)]
        private async Task<ISerializable> StartNodeAsync(ISerializable? message)
        {
            // No import processing
            if (_importBlocksToken is null)
            {
                await StartNeoSystemAsync(CancellationToken.None);
                return BooleanPayload.True;
            }
            return BooleanPayload.False;
        }

        [PipeMethod(PipeMessageCommand.Stop, true)]
        private async Task<ISerializable> StopNodeAsync(ISerializable? message)
        {
            await StopNeoSystemAsync();
            return BooleanPayload.True;
        }
    }
}
