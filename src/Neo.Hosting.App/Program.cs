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
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
using Neo.Extensions;
using Neo.Hosting.App.CommandLine;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Handlers;
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
        internal static bool IsRunningAsService =>
            SystemdHelpers.IsSystemdService() == false && WindowsServiceHelpers.IsWindowsService() == false && Environment.UserInteractive == false;

        static Program()
        {
            ApplicationVersionNumber = AssemblyUtility.GetVersionNumber();
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0");
        }

        static async Task<int> Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            var rootCommand = new DefaultRootCommand();
            var parser = new CommandLineBuilder(rootCommand)
                .UseInternalHost(DefaultNeoHostBuilderFactory, builder =>
                {
                    builder.UseNeoServiceConfiguration();
                    builder.UseCommandHandler<DefaultRootCommand, EmptyHandler>();
                    builder.UseCommandHandler<ExportCommand, EmptyHandler>();
                    builder.UseCommandHandler<RunCommand, RunCommand.Handler>();
                    builder.UseCommandHandler<ConnectCommand, ConnectCommand.Handler>();
                    builder.UseCommandHandler<ExportCommand.BlocksExportCommand, ExportCommand.BlocksExportCommand.Handler>();
                    builder.UseSystemd();
                    builder.UseWindowsService();
                })
                .UseDefaults()
                .UseExceptionHandler(NullExceptionFilter.Handler)
                .Build();

            return await parser.InvokeAsync(args);
        }
    }
}
