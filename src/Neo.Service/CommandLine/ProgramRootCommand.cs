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

using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Neo.Service.Configuration;
using Neo.Service.Extensions;
using Neo.Service.Pipes;
using Neo.Service.Pipes.Messaging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.CommandLine
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo N3 Command-Line Tool")
        {
            var serviceOption = new Option<bool>(["--run-as-service", "-rS"])
            {
                IsHidden = true,
            };

            AddOption(serviceOption);
        }

        public new class Handler(
            NamedPipeListener listener,
            NeoSystem neoSystem,
            NeoOptions options,
            ILogger<SimpleMessageProtocol> logger) : ICommandHandler
        {
            public bool AsService { get; set; }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stoppingToken = context.GetCancellationToken();

                if (SystemdHelpers.IsSystemdService() ||
                    WindowsServiceHelpers.IsWindowsService() ||
                    AsService)
                {
                    // This wait for NamedPipe connections
                    return await WaitForConnections(context.Console, stoppingToken);
                }

                // TODO: add client

                return 0;
            }

            private async Task<int> WaitForConnections(IConsole console, CancellationToken cancellationToken)
            {
                listener.Start();
                logger.LogInformation("Started.");

                while (cancellationToken.IsCancellationRequested == false)
                {
                    try
                    {
                        var conn = await listener.AcceptAsync(cancellationToken);

                        if (conn is null)
                            break;

                        var protocolThread = new SimpleMessageProtocol(conn, neoSystem, options, logger);
                        ThreadPool.UnsafeQueueUserWorkItem(protocolThread, preferLocal: false);
                    }
                    catch (Exception ex)
                    {
                        console.ErrorMessage(ex.Message);
                        return ex.HResult;
                    }
                }

                return 0;
            }
        }
    }
}
