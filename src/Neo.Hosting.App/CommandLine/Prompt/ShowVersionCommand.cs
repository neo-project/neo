// Copyright (C) 2015-2024 The Neo Project.
//
// ShowVersionCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Host.Service;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine.Prompt
{
    internal sealed class ShowVersionCommand : Command
    {
        public ShowVersionCommand(
            ILoggerFactory loggerFactory,
            NamedPipeClientService clientService) : base("version", "Show version information")
        {
            this.SetHandler(context => new Handler(loggerFactory, clientService).InvokeAsync(context));
        }

        public new sealed class Handler(
            ILoggerFactory loggerFactory,
            NamedPipeClientService clientService) : ICommandHandler
        {
            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stopping = context.GetCancellationToken();
                var logger = loggerFactory.CreateLogger("Console");

                var version = await clientService.GetVersionAsync(stopping);

                if (version is null)
                {
                    logger.LogError("Failed to get version information");
                    return 0;
                }

                logger.WriteLine(nameof(Version).PadCenter(21, '-'));
                logger.WriteLine("   Process Id: {0}", version.ProcessId);
                logger.WriteLine(" Process Path: {0}", version.ProcessPath);
                logger.WriteLine("      Version: {0}", version.VersionNumber);
                logger.WriteLine(" Machine Name: {0}", version.MachineName);
                logger.WriteLine("     Platform: {0}", version.Platform);
                logger.WriteLine("    User Name: {0}", version.UserName);
                logger.WriteLine(" Current Time: {0}", version.TimeStamp);
                logger.WriteLine();


                return 0;
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
