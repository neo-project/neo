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

using System.Threading;
using System.Threading.Tasks;

namespace Neo.Node.Service
{
    internal partial class NodeService
    {
        [PipeMethod(CommandType.Exit)]
        private async Task ShutdownAsync(string[] args, CancellationToken cancellationToken) =>
            await StopAsync(cancellationToken);
    }
}
