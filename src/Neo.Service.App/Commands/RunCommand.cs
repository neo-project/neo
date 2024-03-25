// Copyright (C) 2015-2024 The Neo Project.
//
// RunCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Neo.Service.App.Extensions;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Service.App.Commands
{
    internal sealed class RunCommand : Command
    {
        public RunCommand() : base("run", "Run as systemd or windows service")
        {
        }

        public new sealed class Handler : ICommandHandler
        {
            private readonly NeoSystemService _neoSystemService;

            public Handler(
                NeoSystemService neoSystemService)
            {
                _neoSystemService = neoSystemService;
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                if (SystemdHelpers.IsSystemdService() ||
                    WindowsServiceHelpers.IsWindowsService())
                {
                    var host = context.GetHost();
                    var stoppingToken = context.GetCancellationToken();

                    if (_neoSystemService.IsRunning)
                        _neoSystemService.StartNode();

                    await host.WaitForShutdownAsync(stoppingToken);

                    return 0;
                }
                else
                {
                    context.Console.ResetTerminalForegroundColor();
                    context.Console.SetTerminalForegroundRed();

                    context.Console.WriteLine($"Error: Process must be hosted as service.");

                    context.Console.ResetTerminalForegroundColor();

                    return 1;
                }
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
