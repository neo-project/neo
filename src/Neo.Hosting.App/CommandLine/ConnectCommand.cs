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

using Microsoft.Extensions.Options;
using Neo.Hosting.App.CommandLine.Prompt;
using Neo.Hosting.App.Configuration;
using Neo.Hosting.App.Extensions;
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
    using Microsoft.Extensions.Logging;
    using Neo.Hosting.App.Host.Service;

    internal sealed class ConnectCommand : Command
    {
        public ConnectCommand() : base("connect", "Connect to local Neo service")
        {
            var pipeNameOption = new Option<string>(new[] { "--pipe-name", "-pN" }, "Named pipe to connect too");

            AddOption(pipeNameOption);
        }

        public new sealed class Handler(
            NamedPipeClientService clientService,
            ILoggerFactory loggerFactory,
            IOptions<NeoOptions> options) : ICommandHandler
        {
            private static readonly string s_computerName = Environment.MachineName;
            private static readonly string s_userName = Environment.UserName;

            private readonly NeoOptions _options = options.Value;

            private NamedPipeEndPoint? _pipeEndPoint;

            public string? PipeName { get; set; }

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stopping = context.GetCancellationToken();
                var console = loggerFactory.CreateLogger("Console");
                var pipeName = PipeName ?? _options.NamedPipe.Name;

                if (string.IsNullOrWhiteSpace(pipeName))
                    console.WriteLine("Pipe name is required.");
                else
                {
                    _pipeEndPoint = new(pipeName);
                    console.WriteLine($"Connecting to {_pipeEndPoint}...");

                    var exitCode = 0;

                    try
                    {
                        await clientService.ConnectAsync(_pipeEndPoint, stopping).DefaultTimeout();
                        exitCode = await RunConsolePrompt(context, stopping);
                    }
                    catch (TimeoutException)
                    {
                        console.WriteLine();
                        console.LogError("Failed to connect! Make sure service is running.");
                        exitCode = -1;
                    }

                    return exitCode;
                }

                return -1;
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            private static void PrintPrompt(IConsole console)
            {
                console.SetTerminalForegroundColor(ConsoleColor.DarkGreen);
                console.Write($"{s_userName}@{s_computerName}");
                console.SetTerminalForegroundColor(ConsoleColor.DarkBlue);
                console.Write(":~$ ");
                console.SetTerminalForegroundColor(ConsoleColor.White);
                console.ResetColor();
            }

            public async Task<int> RunConsolePrompt(
                InvocationContext context,
                CancellationToken cancellationToken)
            {
                context.Console.Clear();

                var rootCommand = new DefaultRemoteCommand(loggerFactory, clientService);
                var parser = new CommandLineBuilder(rootCommand)
                    .UseParseErrorReporting()
                    .Build();

                var exitCode = 0;

                while (cancellationToken.IsCancellationRequested == false)
                {
                    PrintPrompt(context.Console);

                    var line = context.Console.ReadLine()?.Trim() ?? string.Empty;

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
