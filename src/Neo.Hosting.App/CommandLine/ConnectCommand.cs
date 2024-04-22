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
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Hosting.App.CommandLine
{
    internal class ConnectCommand : Command
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

            public async Task<int> InvokeAsync(InvocationContext context)
            {
                var stopping = context.GetCancellationToken();

                return await RunConsolePrompt(context, stopping);
            }

            public int Invoke(InvocationContext context)
            {
                throw new NotImplementedException();
            }

            public static async Task<int> RunConsolePrompt(InvocationContext context, CancellationToken cancellationToken)
            {
                context.Console.Clear();

                var rootCommand = new DefaultRemoteCommand();
                var parser = new CommandLineBuilder(rootCommand)
                    .UseParseErrorReporting()
                    .UseExceptionHandler()
                    .Build();

                while (cancellationToken.IsCancellationRequested == false)
                {
                    context.Console.SetTerminalForegroundColor(ConsoleColor.DarkBlue);
                    context.Console.Write($"{s_userName}@{s_computerName}");
                    context.Console.SetTerminalForegroundColor(ConsoleColor.White);
                    context.Console.Write(":~$ ");
                    context.Console.SetTerminalForegroundColor(ConsoleColor.DarkCyan);
                    context.Console.ResetTerminalForegroundColor();

                    var line = context.Console.ReadLine()?.Trim() ?? string.Empty;

                    if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase) || line.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        break;

                    _ = await parser.InvokeAsync(line, context.Console);
                }

                return 0;
            }
        }
    }
}
