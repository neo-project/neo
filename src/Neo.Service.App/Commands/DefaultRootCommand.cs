// Copyright (C) 2015-2024 The Neo Project.
//
// DefaultRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Service.App.Commands
{
    internal sealed class DefaultRootCommand : RootCommand
    {
        public DefaultRootCommand() : base("NEO Blockchain CommandLine Tool")
        {
            var archiveCommand = new ArchiveCommand();
            AddCommand(archiveCommand);

            AddOption(new Option<bool>("--as-service", "Run as systemd or windows service"));
        }

        public new sealed class Handler : ICommandHandler
        {
            public bool AsService { get; set; }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var host = context.GetHost();
                var stoppingToken = context.GetCancellationToken();

                if (AsService)
                    await host.WaitForShutdownAsync(stoppingToken);

                return 0;
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
