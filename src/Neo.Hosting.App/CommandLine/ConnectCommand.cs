// Copyright (C) 2015-2024 The Neo Project.
//
// ConnectCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.CommandLine.Prompt;
using Neo.Hosting.App.Extensions;
using Neo.Hosting.App.Factories;
using Neo.Hosting.App.Helpers;
using Neo.Hosting.App.NamedPipes;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine
{
    internal sealed class ConnectCommand : Command
    {
        public ConnectCommand() : base("connect", "Connect to local Neo service")
        {
            var pipeNameArgument = new Argument<string>("PIPE_NAME", "Name of the named pipe to connect to");

            AddArgument(pipeNameArgument);
        }

        public new class Handler : ICommandHandler
        {
            private static readonly string s_computerName = Environment.MachineName;
            private static readonly string s_userName = Environment.UserName;

            private NamedPipeEndPoint? _pipeEndPoint;

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stopping = context.GetCancellationToken();

                if (EnvironmentUtility.TryGetServicePipeName(out var pipeName))
                {
                    _pipeEndPoint = new(pipeName);
                    var pipeStream = NamedPipeServerFactory.CreateClientStream(_pipeEndPoint);

                    context.Console.SetTerminalForegroundColor(ConsoleColor.DarkMagenta);
                    context.Console.WriteLine($"Connecting to {_pipeEndPoint}...");
                    context.Console.ResetTerminalForegroundColor();

                    try
                    {
                        await pipeStream.ConnectAsync(stopping).DefaultTimeout();
                        await RunConsolePrompt(context, stopping);
                    }
                    catch (TimeoutException)
                    {
                        context.Console.WriteLine(string.Empty);
                        context.Console.ErrorMessage($"Failed to connect! Make sure service is running.");
                    }

                    return 0;
                }

                return -1;
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            private static void PrintPrompt(IConsole console)
            {
                console.SetTerminalForegroundColor(ConsoleColor.DarkBlue);
                console.Write($"{s_userName}@{s_computerName}");
                console.SetTerminalForegroundColor(ConsoleColor.White);
                console.Write(":~$ ");
                console.SetTerminalForegroundColor(ConsoleColor.DarkCyan);
                console.ResetTerminalForegroundColor();
            }

            public static async Task<int> RunConsolePrompt(InvocationContext context, CancellationToken cancellationToken)
            {
                context.Console.Clear();

                var rootCommand = new DefaultRemoteCommand();
                var parser = new CommandLineBuilder(rootCommand)
                    .UseParseErrorReporting()
                    .Build();

                while (cancellationToken.IsCancellationRequested == false)
                {
                    PrintPrompt(context.Console);

                    var line = context.Console.ReadLine()?.Trim() ?? string.Empty;

                    if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                        continue;

                    var exitCode = await parser.InvokeAsync(line, context.Console);

                    if (exitCode.Is(ExitCode.Exit))
                        break;
                }

                return 0;
            }
        }
    }
}
