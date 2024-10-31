// Copyright (C) 2015-2024 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Hosting;
using Neo.CLI.Commands;
using Neo.CLI.Extensions;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Neo.CLI
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new ProgramRootCommand();
            var parser = new CommandLineBuilder(rootCommand)
                .UseHost(builder =>
                {
                    builder.UseNeoAppConfiguration();
                    builder.UseNeoHostConfiguration();

                    // Command handlers below <Here>
                    builder.UseCommandHandler<ProgramRootCommand, ProgramRootCommand.Handler>();

                    builder.UseSystemd();
                    builder.UseWindowsService();
                })
                .UseDefaults()
                .CancelOnProcessTermination()
                .Build();

            return await parser.InvokeAsync(args);
        }
    }
}
