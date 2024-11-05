// Copyright (C) 2015-2024 The Neo Project.
//
// ConsoleMessageProtocol.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.Extensions.Logging;
using Neo.Network.P2P;
using Neo.Service.CommandLine;
using Neo.Service.Commands.Prompt;
using Neo.Service.Hosting;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Service.Pipes.Messaging
{
    internal partial class ConsoleMessageProtocol(
        NamedPipeConnection connection,
        NeoSystem neoSystem,
        ILogger logger) : IThreadPoolWorkItem, IAsyncDisposable
    {
        private readonly NamedPipeConnection _connection = connection;
        private readonly NeoSystem _neoSystem = neoSystem;
        private readonly LocalNode _localNode = neoSystem.LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;

        private readonly StreamWriter _output = new StreamWriter(connection.Transport.Output.AsStream()) { AutoFlush = true };
        private readonly StreamReader _input = new StreamReader(connection.Transport.Input.AsStream());

        private readonly ILogger _logger = logger;

        public ValueTask DisposeAsync()
        {
            _logger.LogInformation("Connection shutting down.");

            return _connection.DisposeAsync();
        }

        public void Execute()
        {
            _logger.LogInformation("Connection has started.");

            _ = ProcessReceive();
        }

        private async Task ProcessReceive()
        {
            try
            {
                while (_connection.IsConnected)
                {
                    _output.Write($"{Ansi.Color.Foreground.Green}");
                    _output.Write($"{NeoDefaults.ConsolePromptName} ");
                    _output.Write($"{Ansi.Color.Foreground.White}");

                    var commands = _input.ReadLine() ?? string.Empty;

                    var rootCommand = new PromptRootCommand();
                    var parser = new CommandLineBuilder(rootCommand)
                        .UseParseErrorReporting()
                        .Build();

                    _logger.LogInformation("Received Command: {Command}", commands);
                    await parser.InvokeAsync(commands, new NamedPipeConsole(_input, _output));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                await DisposeAsync();
            }
        }
    }
}
