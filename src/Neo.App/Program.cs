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

using Microsoft.Extensions.Hosting;
using Neo.App.Commands;
using Neo.App.Extensions;
using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Threading.Tasks;

namespace Neo.App
{
    internal class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            var rootCommand = new ProgramRootCommand();
            var parser = new CommandLineBuilder(rootCommand)
                .UseHost(DefaultCommandLineHostFactory, builder =>
                {
                    // Commands here
                    builder.UseCommandHandler<ProgramRootCommand, ProgramRootCommand.Handler>();

                    // Running as Services
                    builder.UseSystemd();
                    builder.UseWindowsService();
                })
                .UseDefaults()
                .UseExceptionHandler(DefaultExceptionFilterHandler)
                .UseAnsiTerminalWhenAvailable()
                .Build();

            return await parser.InvokeAsync(args);
        }

        private static void DefaultExceptionFilterHandler(Exception exception, InvocationContext context)
        {
            if (exception is OperationCanceledException)
                return;

            context.Console.WriteLine(string.Empty);

            if (context.Console.IsErrorRedirected)
                context.Console.Error.Write((exception.InnerException?.Message ?? exception.Message) + Environment.NewLine);
            else
                context.Console.ErrorMessage(exception);

            context.ExitCode = exception.HResult;
        }

        private static IHostBuilder DefaultCommandLineHostFactory(string[] args) =>
            new HostBuilder()
            .UseNeoConfiguration();
    }
}
