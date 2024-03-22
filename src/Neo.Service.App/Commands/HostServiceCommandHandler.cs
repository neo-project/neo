// Copyright (C) 2015-2024 The Neo Project.
//
// HostServiceCommandHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Service.App.Commands
{
    public class HostServiceCommandHandler : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var host = context.GetHost();
            await host.WaitForShutdownAsync(context.GetCancellationToken());

            return 0;
        }

        public int Invoke(InvocationContext context)
        {
            var host = context.GetHost();
            host.WaitForShutdown();

            return 0;
        }
    }
}
