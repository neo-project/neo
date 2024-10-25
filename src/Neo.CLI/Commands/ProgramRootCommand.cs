// Copyright (C) 2015-2024 The Neo Project.
//
// ProgramRootCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI.Commands.Prompt;
using Neo.CLI.Extensions;
using Neo.CLI.Hosting;
using Neo.CLI.Hosting.Services;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.CLI.Commands
{
    internal class ProgramRootCommand : RootCommand
    {
        public ProgramRootCommand() : base("Neo N3 Command-Line Tool")
        {

        }

        public new sealed class Handler(
            NeoSystemHostedService neoSystemService) : ICommandHandler
        {
            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stoppingToken = context.GetCancellationToken();
                var host = context.GetHost();

                await neoSystemService.StartAsync(stoppingToken);

                return await RunConsolePrompt(context, stoppingToken);
            }

            private static void PrintPrompt(IConsole console)
            {
                console.SetTerminalForegroundColor(ConsoleColor.Green);
                console.Write($"{NeoDefaults.ConsolePromptName} ");
                console.SetTerminalForegroundColor(ConsoleColor.White);
                console.ResetColor();
            }

            private async Task<int> RunConsolePrompt(
                InvocationContext context,
                CancellationToken cancellationToken)
            {
                context.Console.Clear();

                var rootCommand = new ReplRootCommand(neoSystemService, cancellationToken, context.Console);
                var parser = new CommandLineBuilder(rootCommand)
                    .UseParseErrorReporting()
                    .Build();

                var exitCode = 0;

                while (cancellationToken.IsCancellationRequested == false)
                {
                    PrintPrompt(context.Console);

                    var line = context.Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                        continue;

                    exitCode = await parser.InvokeAsync(line, context.Console);

                    if (exitCode < 0)
                        break;
                }

                return exitCode;
            }
        }
    }
}
