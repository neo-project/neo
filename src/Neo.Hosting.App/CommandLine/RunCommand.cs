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
        private static readonly string s_pipeName = $"neo.service\\{Program.ApplicationVersion:3}";
        public RunCommand() : base("run")
        {
            IsHidden = true;
        }

        public new sealed class Handler : ICommandHandler
        {
            private readonly NeoSystemHostedService _neoSystemHostedService;
            private readonly PromptSystemHostedService _promptSystemHostedService;

            public Handler(
                NeoSystemHostedService neoSystemHostedService,
                PromptSystemHostedService promptSystemHostedService)
            {
                _neoSystemHostedService = neoSystemHostedService;
                _promptSystemHostedService = promptSystemHostedService;
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var host = context.GetHost();
                var stoppingToken = context.GetCancellationToken();

                await _promptSystemHostedService.StartAsync(stoppingToken);
                await _neoSystemHostedService.StartAsync(stoppingToken);

                if (_neoSystemHostedService.IsInitialized)
                    _neoSystemHostedService.StartNode();

                await Task.Delay(-1, stoppingToken);
                await _promptSystemHostedService.StopAsync(stoppingToken);

                return 0;
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
