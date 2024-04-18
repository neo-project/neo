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
using Neo.Hosting.App.CommandLine;
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Handlers;
using Neo.Hosting.App.Hosting;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.Hosting.App
{
    public sealed partial class Program
    {
        internal static int ApplicationVersionNumber { get; }
        internal static Version ApplicationVersion { get; }

        static Program()
        {
            ApplicationVersionNumber = AssemblyUtilities.GetVersionNumber();
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0");
        }

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new DefaultRootCommand();
            var parser = new CommandLineBuilder(rootCommand)
                .UseHost(DefaultNeoHostBuilderFactory, builder =>
                {
                    builder.ConfigureServices((builder, services) =>
                    {
                        services.AddSingleton<NeoSystemHostedService>();
                        //services.AddHostedService(sp => sp.GetRequiredService<NeoSystemService>());
                        //services.Configure<InvocationLifetimeOptions>(config => config.SuppressStatusMessages = true);
                        services.Configure<SystemOptions>(builder.Configuration.GetRequiredSection("SystemOptions"));
                        services.AddSingleton(ProtocolSettings.Load(builder.Configuration.GetRequiredSection("ProtocolConfiguration")));
                    });
                    builder.UseCommandHandler<DefaultRootCommand, EmptyHandler>();
                    builder.UseCommandHandler<ExportCommand, EmptyHandler>();
                    builder.UseCommandHandler<WalletCommand, EmptyHandler>();
                    builder.UseCommandHandler<RunCommand, RunCommand.Handler>();
                    builder.UseCommandHandler<ExportCommand.BlocksExportCommand, ExportCommand.BlocksExportCommand.Handler>();
                    builder.UseCommandHandler<WalletCommand.OpenWalletCommand, WalletCommand.OpenWalletCommand.Handler>();
                    builder.UseSystemd();
                    builder.UseWindowsService();
                    builder.UseNamedPipes();
                })
                .UseDefaults()
                .UseExceptionHandler(NullExceptionFilter.Handler)
                .Build();

            return await parser.InvokeAsync(args);
        }
    }
}
