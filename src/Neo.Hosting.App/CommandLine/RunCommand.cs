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
using Neo.Hosting.App.Hosting;
using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine
{
    internal sealed class RunCommand : Command
    {
        public RunCommand() : base("run")
        {
            IsHidden = true;
        }

        public new sealed class Handler : ICommandHandler
        {
            private readonly NeoSystemService _neoSystemService;

            public Handler(NeoSystemService neoSystemService)
            {
                _neoSystemService = neoSystemService;
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var host = context.GetHost();
                var stoppingToken = context.GetCancellationToken();

                if (_neoSystemService.IsRunning)
                    _neoSystemService.StartNode();

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
