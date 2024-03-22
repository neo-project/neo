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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neo.Extensions;
using Neo.Service.App.Commands;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;

namespace Neo.Service.App
{
    public sealed partial class Program
    {
        internal readonly static int ApplicationVersionNumber = AssemblyUtilities.GetVersionNumber();
        internal readonly static Version ApplicationVersion;

        static Program() =>
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0");

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand { Handler = new HostServiceCommandHandler() };
            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler(ExceptionFilter.Handler)
                .UseHost(CreateHostBuilder)
                .Build();

            return await parser.InvokeAsync(args);
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddHostedService<NeoSystemService>())
                .UseSystemd()
                .UseWindowsService();
    }
}
