// Copyright (C) 2015-2024 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Neo.CLI.Hosting.Services;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.CLI.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo N3 Command-Line Tool")
        {

        }

        public new sealed class Handler
            (NeoSystemHostedService neoSystemService) : ICommandHandler
        {
            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stoppingToken = context.GetCancellationToken();
                var host = context.GetHost();

                await neoSystemService.StartAsync(stoppingToken);

                await neoSystemService.ShowStateAsync(stoppingToken);

                await host.WaitForShutdownAsync(stoppingToken);
                return 0;
            }
        }
    }
}
