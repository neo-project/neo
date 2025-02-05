// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Commands;
using Neo.Build.Commands.Wallet;
using Neo.Build.Exceptions.Filters;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Threading.Tasks;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var rootCommand = new ProgramRootCommand();
        var parser = new CommandLineBuilder(rootCommand)
            .UseHost(builder =>
            {
                builder.UseCommandHandler<ProgramRootCommand, ProgramRootCommand.Handler>();
                builder.UseCommandHandler<WalletCommand, WalletCommand.Handler>();
                builder.UseCommandHandler<CreateWalletCommand, CreateWalletCommand.Handler>();
            })
            .UseDefaults()
            .UseExceptionHandler(DefaultExceptionFilter.Handler)
            .UseAnsiTerminalWhenAvailable()
            .Build();

        return await parser.InvokeAsync(args);
    }
}
