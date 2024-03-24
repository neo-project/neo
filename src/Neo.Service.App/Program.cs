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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neo.Extensions;
using Neo.Service.App.Commands;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.Service.App
{
    public sealed partial class Program
    {
        internal static int ApplicationVersionNumber { get; } = AssemblyUtilities.GetVersionNumber();
        internal static Version ApplicationVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0");

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new DefaultRootCommand();
            var parser = new CommandLineBuilder(rootCommand)
                .UseHost(_ => new HostBuilder(), builder =>
                {
                    builder.ConfigureDefaults(args);
                    builder.UseSystemd();
                    builder.UseWindowsService();
                    builder.ConfigureHostConfiguration(config =>
                    {
                        config.SetBasePath(AppContext.BaseDirectory);
                    })
                    .ConfigureAppConfiguration(config =>
                    {
                        config.SetBasePath(AppContext.BaseDirectory);
                    });
                    builder.ConfigureServices((_, services) =>
                    {
                        services.AddSingleton<NeoSystemService>();
                        services.AddHostedService(sp => sp.GetRequiredService<NeoSystemService>());
                    });
                    builder.UseCommandHandler<DefaultRootCommand, ICommandHandler>();
                    builder.UseCommandHandler<ExportCommand, ExportCommand.Handler>();
                    builder.UseCommandHandler<ExportCommand.BlocksCommand, ExportCommand.BlocksCommand.Handler>();
                    builder.UseCommandHandler<RunCommand, RunCommand.Handler>();
                })
                .UseDefaults()
                .UseExceptionHandler(NullExceptionFilter.Handler)
                .Build();

            return await parser.InvokeAsync(args);
        }
    }
}
