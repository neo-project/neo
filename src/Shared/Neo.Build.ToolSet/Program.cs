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

using Neo.Build.Core.Exceptions;
using Neo.Build.ToolSet.Commands;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Threading.Tasks;

namespace Neo.Build.ToolSet
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new ProgramRootCommand();
            var parser = new CommandLineBuilder(rootCommand)
                .UseHost(builder =>
                {
                    builder.UseCommandHandler<ProgramRootCommand, ProgramRootCommand.Handler>();
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

            if (exception is NeoBuildException nbe)
            {
                context.Console.Error.Write(nbe.Message + Environment.NewLine);
                context.ExitCode = nbe.ExitCode;
                return;
            }

            context.Console.Error.Write($"{exception}");
            context.ExitCode = exception.HResult;
        }
    }

}
